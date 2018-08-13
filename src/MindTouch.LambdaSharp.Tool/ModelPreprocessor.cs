/*
 * MindTouch λ#
 * Copyright (C) 2018 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit mindtouch.com;
 * please review the licensing section.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MindTouch.LambdaSharp.Tool.Internal;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MindTouch.LambdaSharp.Tool {

    public class ModelPreprocessor : AModelProcessor {

        //--- Constants ---
        private const string VARIABLE_PATTERN = @"\{\{[^\{\}]*\}\}";

        //--- Constructors ---
        public ModelPreprocessor(Settings settings) : base(settings) { }

        //--- Methods ---
        public IParser Preprocess(string source) {
            var inputStream = YamlParser.Parse(source);
            var outputStream = new YamlStream {
                Start = inputStream.Start,
                Documents = inputStream.Documents
                    .Select(inputDocument => Preprocess(inputDocument))
                    .ToList(),
                End = inputStream.End
            };
            var parsingEvents = new List<ParsingEvent>();
            outputStream.AppendTo(parsingEvents);
            return new YamlParsingEventsParser(parsingEvents);
        }

        private YamlDocument Preprocess(YamlDocument inputDocument) {

            // replace choice branches with their respective choices
            var outputDocument = new YamlDocument {
                Start = inputDocument.Start,
                Values = ResolveChoices(inputDocument.Values),
                End = inputDocument.End
            };

            // read variables, if any
            var variables = new Dictionary<string, string>();
            if(outputDocument.Values.FirstOrDefault() is YamlMap rootMap) {

                // find optional `Variables` section
                var variablesEntry = rootMap.Entries.FirstOrDefault(entry => entry.Key.Scalar.Value == "Variables");
                if(variablesEntry.Value != null) {

                    // remove `Variables` from root map
                    rootMap.Entries = rootMap.Entries.Where(entry => entry.Key.Scalar.Value != "Variables").ToList();

                    // parse `Variables` into a dictionary
                    AtLocation("Variables", () => {
                        if(variablesEntry.Value is YamlMap variablesMap) {
                            variables = variablesMap.Entries.Select(entry => new KeyValuePair<string,string>(
                                entry.Key.Scalar.Value,
                                AtLocation(entry.Key.Scalar.Value, () => {
                                    if(entry.Value is YamlScalar scalar) {
                                        return scalar.Scalar.Value;
                                    }
                                    AddError("must be a string value");
                                    return null;
                                }, null)
                            )).Where(entry => entry.Value != null)
                            .ToDictionary(entry => entry.Key, entry => entry.Value);
                        } else {
                            AddError("'Variables' section expected be a map");
                        }
                    });
                }

                // find `Name` attribute
                var nameEntry = rootMap.Entries.FirstOrDefault(entry => entry.Key.Scalar.Value == "Name");
                AtLocation("Name", () => {
                    if(nameEntry.Value is YamlScalar nameScaler) {
                        variables["Module"] = nameScaler.Scalar.Value;
                    } else {
                        AddError("`Name` attribute expected to be a string");
                    }
                });

                // find optional `Version` attribute
                var versionEntry = rootMap.Entries.FirstOrDefault(entry => entry.Key.Scalar.Value == "Version");
                if(versionEntry.Value != null) {
                    AtLocation("Version", () => {
                        if(versionEntry.Value is YamlScalar versionScalar) {
                            variables["Version"] = versionScalar.Scalar.Value;
                        } else {
                            AddError("`Version` attribute expected to be a string");
                        }
                    });
                }
            }

            // add built-in variables
            variables["Tier"] = Settings.Tier;
            variables["GitSha"] = Settings.GitSha;
            variables["AwsRegion"] = Settings.AwsRegion;
            variables["AwsAccountId"] = Settings.AwsAccountId;
            
            // isolate bound variables (i.e. variables that contain other variables)
            var boundVariables = variables
                .Where(kv => Regex.IsMatch(kv.Value, VARIABLE_PATTERN))
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            // isolate free variables (i.e. variables that are free of dependencies)
            var freeVariables = variables
                .Where(kv => !Regex.IsMatch(kv.Value, VARIABLE_PATTERN))
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            // attempt to convert all bound variables to free variables
            AtLocation("Variables", () => {
                bool progress;
                do {
                    progress = false;
                    foreach(var variable in boundVariables.ToList()) {
                        AtLocation(variable.Key, () => {
                            var clean = true;

                            // attempt to replace all variable references using only free variables
                            var value = Substitute(freeVariables, variable.Value, _ => clean = false);
                            if(clean) {

                                // capture that progress towards resolving all bound variables has been made;
                                // if ever a round through produces no progress, we need to stop; otherwise
                                // we will loop forever
                                progress = true;

                                // promote bound variable to free variable
                                freeVariables[variable.Key] = value;
                                boundVariables.Remove(variable.Key);
                            }
                        });
                    }
                } while(progress);

                // report any remaining bound variables
                foreach(var variable in boundVariables) {
                    AtLocation(variable.Key, () => {
                        Substitute(freeVariables, variable.Value, missingName => {
                            if(boundVariables.ContainsKey(missingName)) {
                                AddError($"circular dependency on '{missingName}'");
                            } else {
                                AddError($"unknown variable reference '{missingName}'");
                            }
                        });
                    });
                }
            });

            // substitute all variables
            return new YamlDocument {
                Start = outputDocument.Start,
                Values = SubstituteVariables(freeVariables, outputDocument.Values),
                End = outputDocument.End
            };
        }

        private List<AYamlValue> ResolveChoices(List<AYamlValue> inputValues) {
            var outputValues = new List<AYamlValue>();
            var counter = 0;
            foreach(var inputValue in inputValues) {
                AtLocation($"[{counter++}]", () => {
                    var outputValue = ResolveChoices(inputValue);
                    if(outputValue != null) {
                        outputValues.Add(outputValue);
                    }
                });
            }
            return outputValues;
        }

        private AYamlValue ResolveChoices(AYamlValue inputValue) {
            switch(inputValue) {
            case YamlMap inputMap: {
                    var outputMap = new YamlMap {
                        Start = inputMap.Start,
                        Entries = new List<KeyValuePair<YamlScalar, AYamlValue>>(),
                        End = inputMap.End
                    };
                    Tuple<string, AYamlValue> choice = null;
                    var tierKey = ":" + Settings.Tier;
                    foreach(var inputEntry in inputMap.Entries) {

                        // entries that start with `:` are considered a conditional based on the current deployment tier value
                        if(inputEntry.Key.Scalar.Value.StartsWith(":")) {

                            // check if the key matches the deployment tier value or the key is `:Default` and no choice has been made yet
                            if(
                                (inputEntry.Key.Scalar.Value == tierKey)
                                || (
                                    (inputEntry.Key.Scalar.Value == ":Default") 
                                    && (choice == null)
                                )
                            ) {
                                choice = Tuple.Create(
                                    inputEntry.Key.Scalar.Value,
                                    AtLocation(inputEntry.Key.Scalar.Value, () => ResolveChoices(inputEntry.Value), inputEntry.Value)
                                );
                            }
                        } else {

                            // add the entry to the output map
                            outputMap.Entries.Add(new KeyValuePair<YamlScalar, AYamlValue>(
                                inputEntry.Key, 
                                AtLocation(inputEntry.Key.Scalar.Value, () => ResolveChoices(inputEntry.Value), inputEntry.Value)
                            ));
                        }
                    }

                    // check if a choice was found
                    if(choice != null) {

                        // check if the input map had no other keys; in the case, just return the choice value
                        if(!outputMap.Entries.Any()) {
                            return choice.Item2;
                        }

                        // otherwise, embed the choice into output map
                        AtLocation(choice.Item1, () => {
                            if(choice.Item2 is YamlMap choiceMap) {
                                foreach(var choiceEntry in choiceMap.Entries) {
                                    outputMap.Entries.Add(choiceEntry);
                                }
                            } else {
                                AddError("choice value is not a map");
                            }
                        });
                    }
                    return outputMap;
                }
            case YamlScalar inputScalar:
                return inputScalar;
            case YamlSequence inputSequence:
                return new YamlSequence {
                    Start = inputSequence.Start,
                    Values = ResolveChoices(inputSequence.Values),
                    End = inputSequence.End
                };
            default:
                AddError($"unrecognized YAML value ({inputValue?.GetType().Name ?? "<null>"})");
                return inputValue;
            }
        }

        private List<AYamlValue> SubstituteVariables(Dictionary<string, string> variables, List<AYamlValue> inputValues) {
            var counter = 0;
            return inputValues.Select(value => AtLocation(
                $"[{counter++}]",
                () => SubstituteVariables(variables, value),
                value
            )).ToList();
        }

        private AYamlValue SubstituteVariables(Dictionary<string, string> variables, AYamlValue inputValue) {
            switch(inputValue) {
            case YamlMap inputMap:
                return new YamlMap {
                    Start = inputMap.Start,
                    Entries = inputMap.Entries
                        .Select(inputEntry => new KeyValuePair<YamlScalar, AYamlValue>(
                            inputEntry.Key,
                            AtLocation(
                                inputEntry.Key.Scalar.Value,
                                () => SubstituteVariables(variables, inputEntry.Value),
                                inputEntry.Value
                            )
                        )).ToList(),
                    End = inputMap.End
                };
            case YamlScalar inputScalar:
                return new YamlScalar {
                    Scalar = new Scalar(
                        inputScalar.Scalar.Anchor,
                        inputScalar.Scalar.Tag,
                        Substitute(variables, inputScalar.Scalar.Value, missingName => AddError($"unknown variable reference '{missingName}'")),
                        inputScalar.Scalar.Style,
                        inputScalar.Scalar.IsPlainImplicit,
                        inputScalar.Scalar.IsQuotedImplicit,
                        inputScalar.Scalar.Start,
                        inputScalar.Scalar.End
                    )
                };
            case YamlSequence inputSequence:
                return new YamlSequence {
                    Start = inputSequence.Start,
                    Values = SubstituteVariables(variables, inputSequence.Values),
                    End = inputSequence.End
                };
            default:
                AddError($"unrecognized YAML value ({inputValue?.GetType().Name ?? "<null>"})");
                return inputValue;
            }
        }

        private string Substitute(Dictionary<string, string> variables, string text, Action<string> missing) {
            return Regex.Replace(text, VARIABLE_PATTERN, match => {
                var name = match.ToString();
                name = name.Substring(2, name.Length - 4).Trim();
                if(!variables.TryGetValue(name, out string value)) {
                    missing(name);
                }
                return value ?? ("{{" + name + "}}");
            });
        }
    }
}
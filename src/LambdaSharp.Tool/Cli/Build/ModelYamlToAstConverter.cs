/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2019
 * lambdasharp.net
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
using LambdaSharp.Tool.Internal;
using LambdaSharp.Tool.Model.AST;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace LambdaSharp.Tool.Cli.Build {

    public class ModelYamlToAstConverter : AModelProcessor {

        //--- Fields ---
        private string _selector;

        //--- Constructors ---
        public ModelYamlToAstConverter(Settings settings, string sourceFilename) : base(settings, sourceFilename) { }

        //--- Methods ---
        public ModuleNode Convert(string source, string selector) {

            // parse text into a pre-processed YAML token stream
            IParser yamlParser;
            try {
                _selector = ":" + (selector ?? "Default");
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
                yamlParser = new YamlParsingEventsParser(parsingEvents);
            } catch(Exception e) {
                LogError(e);
                return null;
            }

            // parse YAML token stream into module AST
            try {
                return new DeserializerBuilder()
                    .WithNamingConvention(new PascalCaseNamingConvention())
                    .WithNodeDeserializer(new CloudFormationFunctionNodeDeserializer())
                    .WithCloudFormationFunctions()
                    .Build()
                    .Deserialize<ModuleNode>(yamlParser);
            } catch(YamlDotNet.Core.YamlException e) {
                LogError($"parsing error near {e.Message}", e);
            } catch(Exception e) {
                LogError($"parse error: {e.Message}", e);
            }
            return null;
        }

        public ModuleNode Parse(string source) {

            // parse YAML file into module AST
            try {
                return new DeserializerBuilder()
                    .WithNamingConvention(new PascalCaseNamingConvention())
                    .WithNodeDeserializer(new CloudFormationFunctionNodeDeserializer())
                    .WithCloudFormationFunctions()
                    .Build()
                    .Deserialize<ModuleNode>(source);
            } catch(YamlDotNet.Core.YamlException e) {
                LogError($"parsing error near {e.Message}", e);
            } catch(Exception e) {
                LogError($"parse error: {e.Message}", e);
            }
            return null;
        }

        private YamlDocument Preprocess(YamlDocument inputDocument) {

            // replace choice branches with their respective choices
            return new YamlDocument {
                Start = inputDocument.Start,
                Values = Preprocess(inputDocument.Values),
                End = inputDocument.End
            };
        }

        private List<AYamlValue> Preprocess(List<AYamlValue> inputValues) {
            if(_selector == null) {
                return inputValues;
            }
            var outputValues = new List<AYamlValue>();
            var counter = 0;
            foreach(var inputValue in inputValues) {
                AtLocation($"{counter++}", () => {
                    var outputValue = Preprocess(inputValue);
                    if(outputValue != null) {
                        outputValues.Add(outputValue);
                    }
                });
            }
            return outputValues;
        }

        private AYamlValue Preprocess(AYamlValue inputValue) {
            switch(inputValue) {
            case YamlMap inputMap: {
                    var outputMap = new YamlMap {
                        Start = inputMap.Start,
                        Entries = new List<KeyValuePair<YamlScalar, AYamlValue>>(),
                        End = inputMap.End
                    };
                    Tuple<string, AYamlValue> choice = null;
                    foreach(var inputEntry in inputMap.Entries) {

                        // entries that start with ':' are considered a conditional based on the current selector
                        if(inputEntry.Key.Scalar.Value.StartsWith(":")) {

                            // check if the key matches the selector or the key is ':Default' and no choice has been made yet
                            if(
                                (inputEntry.Key.Scalar.Value == _selector)
                                || (
                                    (inputEntry.Key.Scalar.Value == ":Default")
                                    && (choice == null)
                                )
                            ) {
                                choice = Tuple.Create(
                                    inputEntry.Key.Scalar.Value,
                                    AtLocation(inputEntry.Key.Scalar.Value, () => Preprocess(inputEntry.Value))
                                );
                            }
                        } else {

                            // add the entry to the output map
                            outputMap.Entries.Add(new KeyValuePair<YamlScalar, AYamlValue>(
                                inputEntry.Key,
                                AtLocation(inputEntry.Key.Scalar.Value, () => Preprocess(inputEntry.Value))
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
                                LogError("choice value is not a map");
                            }
                        });
                    }
                    return outputMap;
                }
            case YamlScalar inputScalar:
                if(inputScalar.Scalar.Tag == "!Include") {
                    return Include(inputScalar.Scalar.Value);
                }
                return inputScalar;
            case YamlSequence inputSequence:
                return new YamlSequence {
                    Start = inputSequence.Start,
                    Values = Preprocess(inputSequence.Values),
                    End = inputSequence.End
                };
            default:
                LogError($"unrecognized YAML value ({inputValue?.GetType().Name ?? "<null>"})");
                return inputValue;
            }
        }

        private AYamlValue Include(string filePath)  {
            var sourceFile = Path.Combine(Path.GetDirectoryName(SourceFilename), filePath);

            // read include contents
            string contents = null;
            try {
                contents = File.ReadAllText(sourceFile);
            } catch(FileNotFoundException) {
                LogError($"could not find '{sourceFile}'");
            } catch(IOException) {
                LogError($"invalid !Include value '{sourceFile}'");
            } catch(ArgumentException) {
                LogError($"invalid !Include value '{sourceFile}'");
            }
            AYamlValue result = new YamlScalar(new Scalar("<BAD>"));
            if(contents != null) {

                // check if YAML conversion is required
                if(Path.GetExtension(sourceFile).ToLowerInvariant() != ".yml") {
                    return new YamlScalar(new Scalar(contents));
                }
                InSourceFile(sourceFile, () => {
                    try {
                        var includeStream = YamlParser.Parse(contents);
                        result = Preprocess(includeStream.Documents.First()).Values.First();
                    } catch(YamlDotNet.Core.YamlException e) {
                        LogError($"parsing error near {e.Message}", e);
                    } catch(Exception e) {
                        LogError($"parse error: {e.Message}", e);
                    }
                });
            }
            return result;
        }
   }
}
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
using System.Linq;
using System.Text.RegularExpressions;

namespace LambdaSharp.Tool {

    public static class ModelFunctions {

        //--- Constants ---
        private const string SUBVARIABLE_PATTERN = @"\$\{(?!\!)[^\}]+\}";

        //--- Class Methods ---
        public static object FnGetAtt(string reference, string attributeName)
            => new Dictionary<string, object> {
                ["Fn::GetAtt"] = new List<object> {
                    reference ?? throw new ArgumentNullException(nameof(reference)),
                    attributeName ?? throw new ArgumentNullException(nameof(attributeName))
                }
            };

        public static object FnIf(string conditionName, object valueIfTrue, object valueIfFalse)
            => new Dictionary<string, object> {
                ["Fn::If"] = new List<object> {
                    conditionName ?? throw new ArgumentNullException(nameof(conditionName)),
                    valueIfTrue ?? throw new ArgumentNullException(nameof(valueIfTrue)),
                    valueIfFalse ?? throw new ArgumentNullException(nameof(valueIfFalse))
                }
            };

        public static object FnImportValue(object sharedValueToImport)
            => new Dictionary<string, object> {
                ["Fn::ImportValue"] = sharedValueToImport ?? throw new ArgumentNullException(nameof(sharedValueToImport))
            };

        public static object FnJoin(string separator, IEnumerable<object> parameters) {

            // attempt to concatenate as many values as possible
            var processed = new List<object>();
            foreach(var parameter in parameters) {
                if(processed.Any() && (parameter is string currentText)) {
                    if(processed.Last() is string lastText) {
                        processed[processed.Count - 1] = lastText + separator + currentText;
                    } else {
                        processed.Add(parameter);
                    }
                } else {
                    processed.Add(parameter);
                }
            }
            var count = processed.Count();
            if(count == 0) {
                return "";
            }
            if(count == 1) {
                return processed.First();
            }
            return new Dictionary<string, object> {
                ["Fn::Join"] = new List<object> {
                    separator ?? throw new ArgumentNullException(nameof(separator)),
                    processed
                }
            };
        }

        public static object FnJoin(string separator, object parameters) {
            return new Dictionary<string, object> {
                ["Fn::Join"] = new List<object> {
                    separator ?? throw new ArgumentNullException(nameof(separator)),
                    parameters ?? throw new ArgumentNullException(nameof(parameters))
                }
            };
        }

        public static object FnRef(string reference)
            => new Dictionary<string, object> {
                ["Ref"] = reference ?? throw new ArgumentNullException(nameof(reference))
            };

        public static object FnSelect(string index, object listOfObjects)
            => new Dictionary<string, object> {
                ["Fn::Select"] = new List<object> {
                    index ?? throw new ArgumentNullException(nameof(index)),
                    listOfObjects ?? throw new ArgumentNullException(nameof(listOfObjects))
                }
            };

        public static object FnSub(string input)
            => new Dictionary<string, object> {
                ["Fn::Sub"] = input ?? throw new ArgumentNullException(nameof(input))
            };

        public static object FnSub(string input, IDictionary<string, object> variables) {

            // check if any variables have static values or !Ref expressions
            var staticVariables = variables.Select(kv => {
                string value = null;
                if(kv.Value is string text) {
                    value = text;
                } else if(TryGetFnRef(kv.Value, out var refKey)) {
                    value = $"${{{refKey}}}";
                } else if(TryGetFnGetAtt(kv.Value, out var getAttKey, out var getAttAttribute)) {
                    value = $"${{{getAttKey}.{getAttAttribute}}}";
                } else if(TryGetFnSub(kv.Value, out var subPattern, out var subArguments) && !subArguments.Any()) {
                    value = subPattern;
                }
                return new {
                    Key = kv.Key,
                    Value = value
                };
            }).Where(kv => kv.Value != null).ToDictionary(kv => kv.Key, kv => kv.Value);
            if(staticVariables.Any()) {

                // substitute static variables
                string original;
                do {
                    original = input;
                    foreach(var staticVariable in staticVariables) {
                        input = input.Replace($"${{{staticVariable.Key}}}", (string)staticVariable.Value);
                    }
                } while(input != original);
            }
            var remainingVariables = variables.Where(variable => !staticVariables.ContainsKey(variable.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);

            // check which form of Fn::Sub to generate
            if(remainingVariables.Any()) {

                // return Fn:Sub with parameters
                return new Dictionary<string, object> {
                    ["Fn::Sub"] = new List<object> {
                        input,
                        remainingVariables
                    }
                };
            } else if(Regex.IsMatch(input, SUBVARIABLE_PATTERN)) {

                // return Fn:Sub with inline parameters
                return new Dictionary<string, object> {
                    ["Fn::Sub"] = input
                };
            } else {

                // return input string without any parameters
                return input;
            }
        }

        public static object FnSplit(string delimiter, object sourceString)
            => new Dictionary<string, object> {
                ["Fn::Split"] = new List<object> {
                    delimiter ?? throw new ArgumentNullException(nameof(delimiter)),
                    sourceString ?? throw new ArgumentNullException(nameof(sourceString))
                }
            };

        public static object FnEquals(object left, object right)
            => new Dictionary<string, object> {
                ["Fn::Equals"] = new List<object> {
                    left ?? throw new ArgumentNullException(nameof(left)),
                    right ?? throw new ArgumentNullException(nameof(right))
                }
            };

        public static object FnAnd(params object[] values)
            => new Dictionary<string, object> {
                ["Fn::And"] = values.ToList()
            };

        public static object FnOr(params object[] values)
            => new Dictionary<string, object> {
                ["Fn::Or"] = values.ToList()
            };

        public static object FnNot(object value)
            => new Dictionary<string, object> {
                ["Fn::Not"] = new List<object> {
                    value ?? throw new ArgumentNullException(nameof(value))
                }
            };

        public static object FnCondition(string condition)
            => new Dictionary<string, object> {
                ["Condition"] = condition ?? throw new ArgumentNullException(nameof(condition))
            };

        public static object FnFindInMap(string mapName, object topLevelKey, object secondLevelKey)
            => new Dictionary<string, object> {
                ["Fn::FindInMap"] = new List<object> {
                    mapName ?? throw new ArgumentNullException(nameof(mapName)),
                    topLevelKey ?? throw new ArgumentNullException(nameof(topLevelKey)),
                    secondLevelKey ?? throw new ArgumentNullException(nameof(secondLevelKey))
                }
            };

        public static object FnTransform(string macroName, IDictionary<string, object> parameters)
            => new Dictionary<string, object> {
                ["Fn::Transform"] = new Dictionary<string, object> {
                    ["Name"] = macroName,
                    ["Parameters"] = parameters
                }
            };

        public static string ReplaceSubPattern(string subPattern, Func<string, string, string> replace)
            => Regex.Replace(subPattern, SUBVARIABLE_PATTERN, match => {
                var matchText = match.ToString();
                var name = matchText.Substring(2, matchText.Length - 3).Trim().Split('.', 2);
                var suffix = (name.Length == 2) ? ("." + name[1]) : null;
                var key = name[0];
                return replace(key, suffix) ?? matchText;
            });

        public static bool TryGetFnIf(object value, out string condition, out object ifTrue, out object ifFalse) {
            if(
                (value is IDictionary<string, object> map)
                && (map.Count == 1)
                && map.TryGetValue("Fn::If", out var argsObject)
                && (argsObject is IList<object> argsList)
                && (argsList.Count == 3)
                && (argsList[0] is string conditionText)
            ) {
                condition = conditionText;
                ifTrue = argsList[1];
                ifFalse = argsList[2];
                return true;
            }
            condition = null;
            ifTrue = null;
            ifFalse = null;
            return false;
        }

        public static bool TryGetFnRef(object value, out string key)  {
            if(
                (value is IDictionary<string, object> map)
                && (map.Count == 1)
                && map.TryGetValue("Ref", out var refObject)
                && (refObject is string refKey)
            ) {
                key = refKey;
                return true;
            }
            key = null;
            return false;
        }

        public static bool TryGetFnGetAtt(object value, out string key, out string attribute)  {
            if(
                (value is IDictionary<string, object> map)
                && (map.Count == 1)
                && map.TryGetValue("Fn::GetAtt", out var getAttObject)
                && (getAttObject is IList<object> getAttArgs)
                && (getAttArgs.Count == 2)
                && getAttArgs[0] is string getAttKey
                && getAttArgs[1] is string getAttAttribute
            ) {
                key = getAttKey;
                attribute = getAttAttribute;
                return true;
            }
            key = null;
            attribute = null;
            return false;
        }

        public static bool TryGetFnSub(object value, out string pattern, out IDictionary<string, object> arguments) {
            if(
                (value is IDictionary<string, object> map)
                && (map.Count == 1)
                && map.TryGetValue("Fn::Sub", out var subObject)
            ) {

                // determine which form of !Sub is being used
                if(subObject is string) {
                    pattern = (string)subObject;
                    arguments = new Dictionary<string, object>();
                    return true;
                }
                if(
                    (subObject is IList<object> subList)
                    && (subList.Count == 2)
                    && (subList[0] is string)
                    && (subList[1] is IDictionary<string, object>)
                ) {
                    pattern = (string)subList[0];
                    arguments = (IDictionary<string, object>)subList[1];
                    return true;
                }
            }
            pattern = null;
            arguments = null;
            return false;
        }

        public static bool TryGetFnCondition(object value, out string condition)  {
            if(
                (value is IDictionary<string, object> map)
                && (map.Count == 1)
                && map.TryGetValue("Condition", out var refObject)
                && (refObject is string refKey)
            ) {
                condition = refKey;
                return true;
            }
            condition = null;
            return false;
        }

        public static bool TryGetFnFindInMap(object value, out string mapName, out object topLevelKey, out object secondLevelKey) {
            if(
                (value is IDictionary<string, object> map)
                && (map.Count == 1)
                && map.TryGetValue("Fn::FindInMap", out var argsObject)
                && (argsObject is IList<object> argsList)
                && (argsList.Count == 3)
                && (argsList[0] is string mapNameText)
            ) {
                mapName = mapNameText;
                topLevelKey = argsList[1];
                secondLevelKey = argsList[2];
                return true;
            }
            mapName = null;
            topLevelKey = null;
            secondLevelKey = null;
            return false;
        }
    }
}
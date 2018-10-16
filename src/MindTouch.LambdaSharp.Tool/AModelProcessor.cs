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
using System.Linq;

namespace MindTouch.LambdaSharp.Tool {

    public abstract class AModelProcessor {

        //--- Constants ---
        protected const string CLOUDFORMATION_ID_PATTERN = "[a-zA-Z][a-zA-Z0-9]*";

        //--- Class Methods ---
        protected static object FnGetAtt(string logicalNameOfResource, string attributeName)
            => new Dictionary<string, object> {
                ["Fn::GetAtt"] = new List<object> {
                    logicalNameOfResource,
                    attributeName
                }
            };

        protected static object FnIf(string conditionName, object valueIfTrue, object valueIfFalse)
            => new Dictionary<string, object> {
                ["Fn::If"] = new List<object> {
                    conditionName,
                    valueIfTrue,
                    valueIfFalse
                }
            };

        protected static object FnImportValue(object sharedValueToImport)
            => new Dictionary<string, object> {
                ["Fn::ImportValue"] = sharedValueToImport
            };

        protected static object FnJoin(string separator, IList<object> parameters) {
            if(parameters.Count == 0) {
                return "";
            }
            if(parameters.Count == 1) {
                return parameters.First();
            }
            if(parameters.All(value => value is string)) {
                return string.Join(",", parameters);
            }
            return new Dictionary<string, object> {
                ["Fn::Join"] = new List<object> {
                    separator,
                    parameters
                }
            };
        }

        protected static object FnJoin(string separator, object parameters) {
            return new Dictionary<string, object> {
                ["Fn::Join"] = new List<object> {
                    separator,
                    parameters
                }
            };
        }
        protected static object FnRef(string reference)
            => new Dictionary<string, object> {
                ["Ref"] = reference
            };

        protected static object FnSelect(string index, object listOfObjects)
            => new Dictionary<string, object> {
                ["Fn::Select"] = new List<object> {
                    index,
                    listOfObjects
                }
            };

        protected static object FnSub(string input)
            => new Dictionary<string, object> {
                ["Fn::Sub"] = input
            };

        protected static object FnSub(string input, IDictionary<string, object> variables)
            => new Dictionary<string, object> {
                ["Fn::Sub"] = new List<object> {
                    input,
                    variables
                }
            };

        protected static object FnSplit(string delimiter, object sourceString)
            => new Dictionary<string, object> {
                ["Fn::Split"] = new List<object> {
                    delimiter,
                    sourceString
                }
            };

        //--- Fields ---
        private readonly Settings _settings;
        private Stack<string> _locations = new Stack<string>();

        //--- Constructors ---
        protected AModelProcessor(Settings settings) {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        //--- Properties ---
        public Settings Settings { get => _settings; }

        //--- Methods ---
        protected void AtLocation(string location, Action action) {
            try {
                _locations.Push(location);
                action();
            } catch(Exception e) {
                AddError(e);
            } finally {
                _locations.Pop();
            }
        }

        protected T AtLocation<T>(string location, Func<T> function, T onErrorReturn) {
            try {
                _locations.Push(location);
                return function();
            } catch(Exception e) {
                AddError(e);
                return onErrorReturn;
            } finally {
                _locations.Pop();
            }
        }

        protected void Validate(bool condition, string message) {
            if(!condition) {
                AddError(message);
            }
        }

        protected void AddError(string message, Exception exception = null)
            => Settings.AddError($"{message} @ {string.Join("/", _locations.Reverse())} [{Settings.ModuleSource}]", exception);

        protected void AddError(Exception exception = null)
            => Settings.AddError(exception);
    }
}
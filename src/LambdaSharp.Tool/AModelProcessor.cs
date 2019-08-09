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
using System.Text;
using LambdaSharp.Tool.Model;
using LambdaSharp.Tool.Model.AST;

namespace LambdaSharp.Tool {

    public abstract class AModelProcessor {

        //--- Constants ---
        protected const string CLOUDFORMATION_ID_PATTERN = "[a-zA-Z][a-zA-Z0-9]*";

        //--- Class Fields ---
        private static Stack<string> _locations = new Stack<string>();

        //--- Class Properties ---
        public static string LocationPath => string.Join("/", _locations.Reverse());

        //--- Fields ---
        private readonly Settings _settings;
        private string _sourceFilename;

        //--- Constructors ---
        protected AModelProcessor(Settings settings, string sourceFilename) {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _sourceFilename = sourceFilename;
        }

        //--- Properties ---
        public Settings Settings => _settings;
        public string SourceFilename => _sourceFilename;
        public bool HasErrors => Settings.HasErrors;

        //--- Methods ---
        protected void AtLocation(string location, Action action) {
            try {
                _locations.Push(location);
                action();
            } catch(ModelLocationException) {

                // exception already has location; don't re-wrap it
                throw;
            } catch(Exception e) {
                throw new ModelLocationException(LocationPath, _sourceFilename, e);
            } finally {
                _locations.Pop();
            }
        }

        protected T AtLocation<T>(string location, Func<T> function) {
            try {
                _locations.Push(location);
                return function();
            } catch(ModelLocationException) {

                // exception already has location; don't re-wrap it
                throw;
            } catch(Exception e) {
                throw new ModelLocationException(LocationPath, _sourceFilename, e);
            } finally {
                _locations.Pop();
            }
        }

        protected void Validate(bool condition, string message) {
            if(!condition) {
                LogError(message);
            }
        }

        protected void LogWarn(string message) {
            var text = new StringBuilder();
            text.Append(message);
            if(_locations.Any()) {
                text.Append($" @ {LocationPath}");
            }
            if(_sourceFilename != null) {
                text.Append($" [{_sourceFilename}]");
            }
            Settings.LogWarn(text.ToString());
        }

        protected void LogError(string message, Exception exception = null) {
            var text = new StringBuilder();
            text.Append(message);
            if(_locations.Any()) {
                text.Append($" @ {LocationPath}");
            }
            if(_sourceFilename != null) {
                text.Append($" [{_sourceFilename}]");
            }
            Settings.LogError(text.ToString(), exception);
        }

        protected void LogError(Exception exception)
            => Settings.LogError(exception);

        protected void LogInfo(string message)
            => Settings.LogInfo(message);

        protected void LogInfoVerbose(string message)
            => Settings.LogInfoVerbose(message);

        protected List<string> ConvertToStringList(object value) {
            var result = new List<string>();
            if(value is string inlineValue) {

                // inline values can be separated by ','
                result.AddRange(inlineValue.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries));
            } else if(value is IList<string> stringList) {
                result = stringList.ToList();
            } else if(value is IList<object> objectList) {
                result = objectList.Cast<string>().ToList();
            } else if(value != null) {
                LogError("invalid value");
            }
            return result;
        }

        protected void ForEach<T>(string location, IEnumerable<T> values, Action<int, T> action) {
            if(values?.Any() != true) {
                return;
            }
            AtLocation(location, () => {
                var index = 0;
                foreach(var value in values) {
                    action(++index, value);
                }
            });
        }
   }
}
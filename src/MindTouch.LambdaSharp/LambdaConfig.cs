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
using MindTouch.LambdaSharp.ConfigSource;

namespace MindTouch.LambdaSharp {
    public sealed class LambdaConfig {

        //--- Class Methods ---
        private static string CombinePathWithKey(string path, string key) => ((path ?? "").Length > 0) ? (path + "/" + key) : key;

        //--- Fields ---
        private readonly LambdaConfig _parent;
        private readonly string _key;
        private readonly ILambdaConfigSource _source;

        //--- Constructors ---
        public LambdaConfig(ILambdaConfigSource source) {
            _source = source ?? throw new ArgumentNullException(nameof(source));
        }

        private LambdaConfig(LambdaConfig parent, string key, ILambdaConfigSource source) {
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
            _key = key ?? throw new ArgumentNullException(nameof(key));
            _source = source ?? throw new ArgumentNullException(nameof(source));
        }

        //--- Properties ---
        public LambdaConfig this[string key] {
            get {
                ILambdaConfigSource childConfig;
                try {
                    childConfig = _source.Open(key) ?? new EmptyLambdaConfigSource();
                } catch(Exception e) when (!(e is ALambdaConfigException)) {
                    throw new LambdaConfigUnexpectedException(CombinePathWithKey(Path, key), "opening child config", e);
                }
                return new LambdaConfig(this, key, childConfig);
            }
        }

        public string Path => CombinePathWithKey(_parent?.Path ?? "", _key);

        public IEnumerable<string> Keys {
            get {
                try {
                    return _source.ReadAllKeys().OrderBy(key => key.ToLowerInvariant()).ToArray();
                } catch(Exception e) when(!(e is ALambdaConfigException)) {
                    throw new LambdaConfigUnexpectedException(Path, "reading all config keys", e);
                }
            }
        }

        //--- Methods ---
        public T Read<T>(string key, Func<string, T> fallback, Func<string, T> convert, Action<T> validate) {

            // attempt to read the requested key
            string textValue;
            try {
                textValue = _source.Read(key);
            } catch(Exception e) when(!(e is ALambdaConfigException)) {
                throw new LambdaConfigUnexpectedException(CombinePathWithKey(Path, key), "reading config key", e);
            }

            // check if we were unable to find a value
            T value;
            if(textValue == null) {

                // check if we have a fallback option for getting the value
                if(fallback == null) {
                    throw new LambdaConfigMissingKeyException(CombinePathWithKey(Path, key));
                }

                // attempt to get the key value using the fallback callback
                try {
                    value = fallback(key);
                } catch(Exception e) when(!(e is ALambdaConfigException)) {
                    throw new LambdaConfigUnexpectedException(CombinePathWithKey(Path, key), "invoking fallback", e);
                }
            } else {

                // attempt to convert value to desired type
                try {
                    if(convert == null) {
                        value = (T)Convert.ChangeType(textValue, typeof(T));
                    } else {
                        value = convert(textValue);
                    }
                } catch(Exception e) when(!(e is ALambdaConfigException)) {
                    throw new LambdaConfigUnexpectedException(CombinePathWithKey(Path, key), "validating value", e);
                }
            }

            // optionally validate converted value
            if(validate == null) {
                return value;
            }
            try {
                validate(value);
            } catch(Exception e) when(!(e is ALambdaConfigException)) {
                throw new LambdaConfigBadValueException(CombinePathWithKey(Path, key), e);
            }
            return value;
        }
    }
}

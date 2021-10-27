/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2021
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

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace LambdaSharp.App.Bus.BroadcastFunction {

    internal static class JObjectEx {

        //--- Class Methods ---
        public static Dictionary<string, object> ToDictionary(this JObject json) {
            var result = json.ToObject<Dictionary<string, object>>();
            ConvertJObjectValues(result);
            ConvertJArrayValues(result);
            return result;
        }

        private static void ConvertJObjectValues(Dictionary<string, object> propertyValuePairs) {
            var objectKeys = propertyValuePairs.Where(kv => kv.Value is JObject).Select(kv => kv.Key).ToList();
            objectKeys.ForEach(propertyName => propertyValuePairs[propertyName] = ToDictionary((JObject)propertyValuePairs[propertyName]));
        }

        private static void ConvertJArrayValues(Dictionary<string, object> propertyValuePairs) {
            var arrayKeys = propertyValuePairs.Where(kv => kv.Value is JArray).Select(kv => kv.Key).ToList();
            arrayKeys.ForEach(propertyName => propertyValuePairs[propertyName] = ToArray((JArray)propertyValuePairs[propertyName]));
        }

        public static object[] ToArray(this JArray array)
            => array.ToObject<object[]>().Select(ProcessArrayEntry).ToArray();

        private static object ProcessArrayEntry(object value)
            => value switch {
                JObject _ => ToDictionary((JObject)value),
                JArray _ => ToArray((JArray)value),
                _ => value
            };
    }
}

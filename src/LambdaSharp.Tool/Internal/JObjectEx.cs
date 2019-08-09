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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace LambdaSharp.Tool.Internal {

    internal static class JObjectEx {

        //--- Class Methods ---
        public static object ConvertJTokenToNative(this object value)
            => ConvertJTokenToNative(value, type => type.FullName.StartsWith("Humidifier.", StringComparison.Ordinal));

        public static object ConvertJTokenToNative(this object value, Predicate<Type> isNativeTypeAllowed) {

            // NOTE (2019-01-25, bjorg): this method is needed because the Humidifier types use 'dynamic' as type;
            //  and JsonConvert then generates JToken values instead of primitive types as it does for object.

            switch(value) {
            case JObject jObject: {
                    var map = new Dictionary<string, object>();
                    foreach(var property in jObject.Properties()) {
                        map[property.Name] = ConvertJTokenToNative(property.Value, isNativeTypeAllowed);
                    }
                    return map;
                }
            case JArray jArray: {
                    var list = new List<object>();
                    foreach(var item in jArray) {
                        list.Add(ConvertJTokenToNative(item, isNativeTypeAllowed));
                    }
                    return list;
                }
            case JValue jValue:
                return jValue.Value;
            case JToken _:
                throw new ApplicationException($"unsupported type: {value.GetType()}");
            case IDictionary dictionary:
                foreach(string key in dictionary.Keys.OfType<string>().ToList()) {
                    dictionary[key] = ConvertJTokenToNative(dictionary[key], isNativeTypeAllowed);
                }
                return value;
            case IList list:
                for(var i = 0; i < list.Count; ++i) {
                    list[i] = ConvertJTokenToNative(list[i], isNativeTypeAllowed);
                }
                return value;
            case null:
                return value;
            default:
                if(SkipType(value.GetType())) {

                    // nothing further to remove
                    return value;
                }
                if(isNativeTypeAllowed?.Invoke(value.GetType()) ?? false) {

                    // use reflection to substitute properties
                    foreach(var property in value.GetType().GetProperties().Where(p => !SkipType(p.PropertyType))) {
                        object propertyValue;
                        try {
                            propertyValue = property.GetGetMethod()?.Invoke(value, new object[0]);
                        } catch(Exception e) {
                            throw new ApplicationException($"unable to get {value.GetType()}::{property.Name}", e);
                        }
                        if((propertyValue == null) || SkipType(propertyValue.GetType())) {

                            // nothing to do
                        } else {
                            propertyValue = ConvertJTokenToNative(propertyValue, isNativeTypeAllowed);
                            try {
                                property.GetSetMethod()?.Invoke(value, new[] { propertyValue });
                            } catch(Exception e) {
                                throw new ApplicationException($"unable to set {value.GetType()}::{property.Name}", e);
                            }
                        }
                    }
                    return value;
                }
                throw new ApplicationException($"unsupported type: {value.GetType()}");
            }

            // local function
            bool SkipType(Type type) => type.IsValueType || type == typeof(string);
        }
    }
}
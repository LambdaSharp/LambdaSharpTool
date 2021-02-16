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

using System;
using System.Linq;
using System.Net;
using Newtonsoft.Json.Linq;

namespace LambdaSharp.App.EventBus {

    public static class EventPatternMatcher {

        //--- Class Fields ---
        private static readonly JValue Null = JValue.CreateNull();

        //--- Class Methos ---
        private static bool IsTextToken(JToken token) => token.Type == JTokenType.String;
        private static bool IsBooleanToken(JToken token) => token.Type == JTokenType.Boolean;
        private static bool IsNumericToken(JToken token) => (token.Type == JTokenType.Integer) || (token.Type == JTokenType.Float);

        private static bool IsOperatorToken(JToken token)
            => (token is JValue literal)
                && literal.Value switch {
                    "<" => true,
                    "<=" => true,
                    "=" => true,
                    ">" => true,
                    ">=" => true,
                    _ => false
                };

        private static bool TryGetText(JToken token, out string text) {
            if(token.Type == JTokenType.String) {
                text = (string)((JValue)token).Value;
                return true;
            } else {
                text = null;
                return false;
            }
        }

        private static bool TryGetNumeric(JToken token, out double numeric) {
            switch(token.Type) {
            case JTokenType.Integer:
            case JTokenType.Float:
                numeric = (double)Convert.ChangeType(((JValue)token).Value, typeof(double));
                return true;
            default:
                numeric = 0.0;
                return false;
            }
        }

        private static bool TryGetBoolean(JToken token, out bool boolean) {
            if(token.Type == JTokenType.Boolean) {
                boolean = (bool)((JValue)token).Value;
                return true;
            } else {
                boolean = false;
                return false;
            }
        }

        //--- Methods ---
        public static bool IsValid(JObject pattern) {
            if(!pattern.Properties().Any()) {

                // pattern cannot be empty
                return false;
            }
            foreach(var kv in pattern) {
                switch(kv.Value) {
                case JArray allowedValues:

                    // only allow literals and content-patterns
                    if(!allowedValues.Any()) {

                        // list cannot be empty
                        return false;
                    }
                    foreach(var allowedValue in allowedValues) {
                        switch(allowedValue) {
                        case JArray _:

                            // nested array is not allowed
                            return false;
                        case JObject contentBasedPattern:
                            if(!IsContentPatternValid(contentBasedPattern)) {
                                return false;
                            }
                            break;
                        case JValue _:
                            break;
                        }
                    }
                    break;
                case JObject nestedPattern:
                    if(!IsValid(nestedPattern)) {
                        return false;
                    }
                    break;
                case JValue valuePattern:

                    // event pattern contains invalid value (can only be a nonempty array or nonempty object)

                    // NOTE (2020-10-18, bjorg): we allow 'null' because it can be an artifact from JSON serialization
                    return valuePattern.Type == JTokenType.Null;
                default:
                    throw new ArgumentException($"invalid pattern type: {kv.Value?.GetType().FullName ?? "<null>"}");
                }
            }
            return true;

            // local functions
            bool IsContentPatternValid(JObject contentPattern) {
                var contentPatternOperation = contentPattern.Properties().SingleOrDefault();
                if(contentPatternOperation == null) {

                    // content-based pattern must have exactly one property
                    return false;
                }

                // validate content-based filter
                switch(contentPatternOperation.Name) {
                case "prefix":
                    if(IsTextToken(contentPatternOperation.Value)) {

                        // { "prefix": "TEXT" }
                        return true;
                    }
                    break;
                case "anything-but":
                    if(IsTextToken(contentPatternOperation.Value) || IsNumericToken(contentPatternOperation.Value)) {

                        // { "anything-but": "TEXT" }
                        // { "anything-but": NUMERIC }
                        return true;
                    } else if(
                        (contentPatternOperation.Value is JArray anythingButValues)
                        && anythingButValues.Any()
                        && anythingButValues.All(value => IsTextToken(value) || IsNumericToken(value))
                    ) {

                        // { "anything-but": [ "TEXT"+ ] }
                        // { "anything-but": [ NUMERIC+ ] }
                        return true;
                    } else if(
                        (contentPatternOperation.Value is JObject anythingButContentPattern)
                        && IsContentPatternValid(anythingButContentPattern)
                    ) {

                        // { "anything-but": { ... } }
                        return true;
                    }
                    break;
                case "numeric":
                    if(contentPatternOperation.Value is JArray numericFilterValues) {
                        if(numericFilterValues.Count() == 2) {
                            if(
                                IsOperatorToken(numericFilterValues[0])
                                && IsNumericToken(numericFilterValues[1])
                            ) {

                                // { "numeric": [ "<", NUMERIC ] }
                                return true;
                            }
                        } else if(numericFilterValues.Count() == 4) {
                            if(
                                IsOperatorToken(numericFilterValues[0])
                                && IsNumericToken(numericFilterValues[1])
                                && IsOperatorToken(numericFilterValues[2])
                                && IsNumericToken(numericFilterValues[3])
                            ) {

                                // { "numeric": [ ">", NUMERIC, "<=", NUMERIC ] }
                                return true;
                            }
                        }
                    }
                    break;
                case "cidr":
                    if((contentPatternOperation.Value is JValue cidrFilterValue) && (cidrFilterValue.Type == JTokenType.String)) {

                        // Sample value: "10.0.0.0/24"
                        var parts = ((string)cidrFilterValue.Value).Split('/');
                        if(
                            (parts.Length == 2)
                            && int.TryParse(parts[1], out var prefixValue)
                            && (prefixValue >= 0)
                            && (prefixValue < 32)
                        ) {

                            // valid ip prefix
                            var ipBytes = parts[0].Split('.');
                            if(
                                (ipBytes.Length == 4)
                                && ipBytes.All(ipByte =>
                                    int.TryParse(ipByte, out var ipByteValue)
                                    && (ipByteValue >= 0)
                                    && (ipByteValue < 256)
                                )
                            ) {

                                // { "cidr": "10.0.0.0/24" }
                                return true;
                            }
                        }
                    }
                    break;
                case "exists":
                    if(IsBooleanToken(contentPatternOperation.Value)) {

                        // { "exists": BOOLEAN  }
                        return true;
                    }
                    break;
                }

                // unrecognized content filter
                return false;
            }
        }

        public static bool IsMatch(JObject data, JObject pattern) {
            foreach(var patternProperty in pattern) {
                switch(patternProperty.Value) {
                case JArray allowedValues:

                    // check key exists
                    switch(data[patternProperty.Key]) {
                    case JObject map:

                        // array can never match an object
                        if(!allowedValues.Any(allowedValue => IsContentMatch(map, allowedValue))) {
                            return false;
                        }
                        break;
                    case JArray array:
                        if(!array.Any(value => allowedValues.Any(allowedValue => IsContentMatch(value, allowedValue)))) {
                            return false;
                        }
                        break;
                    case JValue value:
                        if(!allowedValues.Any(allowedValue => IsContentMatch(value, allowedValue))) {
                            return false;
                        }
                        break;
                    case null:
                        if(!allowedValues.Any(allowedValue => IsContentMatch(null, allowedValue))) {
                            return false;
                        }
                        break;
                    default:
                        throw new ArgumentException($"unexpected pattern type: {data[patternProperty.Key]?.GetType().FullName ?? "<null>"}");
                    }
                    break;
                case JObject nestedPattern:

                    // check key exists and matches pattern
                    if(
                        !(data[patternProperty.Key] is JObject nestedData)
                        || !IsMatch(nestedData, nestedPattern)
                    ) {
                        return false;
                    }
                    break;
                case JValue valuePattern when valuePattern.Type == JTokenType.Null:

                    // JSON serialization artifact; nothing to do
                    break;
                default:
                    throw new ArgumentException($"unexpected pattern type: {patternProperty.Value?.GetType().FullName ?? "<null>"}");
                }
            }
            return true;

            // local functions
            bool IsContentMatch(JToken data, JToken pattern) {
                if(pattern is JValue literalPattern) {
                    if(!(data is JValue dataValue)) {
                        return false;
                    }

                    // check for an exact match
                    return (dataValue.Value == literalPattern.Value)
                        || (
                            (dataValue.Value != null)
                            && (literalPattern.Value != null)
                            && dataValue.Value.Equals(literalPattern.Value)
                        );
                } else if(pattern is JObject contentPattern) {

                    // check content-based filter operation
                    var contentPatternOperation = contentPattern.Properties().Single();
                    switch(contentPatternOperation.Name) {
                    case "prefix":
                        if(!TryGetText(data, out var dataText)) {
                            return false;
                        }

                        // { "prefix": "TEXT" }
                        return dataText.StartsWith((string)contentPatternOperation.Value, StringComparison.Ordinal);
                    case "anything-but":
                        if(contentPatternOperation.Value is JArray disallowedValues) {

                            // { "anything-but": [ DISALLOWED-VALUE ] }
                            return !disallowedValues.Any(disallowedValue => IsContentMatch(data, disallowedValue));
                        } else {

                            // { "anything-but": DISALLOWED-VALUE }
                            return !IsContentMatch(data, contentPatternOperation.Value);
                        }
                    case "numeric": {

                        // check if data is numeric
                        if(!TryGetNumeric(data, out var dataNumeric)) {
                            return false;
                        }
                        var numericFilterValues = (JArray)contentPatternOperation.Value;
                        switch(numericFilterValues.Count) {
                        case 2:

                            // { "numeric": [ "<", NUMERIC ] }
                            return CheckNumericOperation(
                                (string)((JValue)numericFilterValues[0]),
                                (double)Convert.ChangeType((JValue)numericFilterValues[1], typeof(double))
                            );
                        case 4:

                            // { "numeric": [ ">", NUMERIC, "<=", NUMERIC ] }
                            return CheckNumericOperation(
                                (string)((JValue)numericFilterValues[0]),
                                (double)Convert.ChangeType((JValue)numericFilterValues[1], typeof(double))
                            ) && CheckNumericOperation(
                                (string)((JValue)numericFilterValues[2]),
                                (double)Convert.ChangeType((JValue)numericFilterValues[3], typeof(double))
                            );
                        default:
                            throw new Exception("invalid content pattern");
                        }

                        // local functions
                        bool CheckNumericOperation(string operation, double comparand)
                            => operation switch {
                                "<" => dataNumeric < comparand,
                                "<=" => dataNumeric <= comparand,
                                "=" => dataNumeric == comparand,
                                ">=" => dataNumeric >= comparand,
                                ">" => dataNumeric > comparand,
                                _ => throw new Exception($"invalid operation: {operation ?? "<null>"}")
                            };
                    }
                    case "cidr":
                        if(!TryGetText(data, out var dataIpAddress)) {
                            return false;
                        }

                        // { "cidr": "10.0.0.0/24" }
                        return IsInCidrRange(dataIpAddress, (string)contentPatternOperation.Value);
                    case "exists":

                        // { "exists": BOOLEAN  }
                        return ((bool)((JValue)contentPatternOperation.Value).Value)
                            ? ((data as JValue) != null)
                            : ((data as JValue) == null);
                    }

                    // unrecognized content filter
                    return false;
                } else {
                    throw new ArgumentException($"unexpected pattern type: {pattern.GetType().FullName ?? "<null>"}");
                }
            }

            bool IsInCidrRange(string ipValue, string cidrRange) {
                try {
                    var ipAndPrefix = cidrRange.Split('/');
                    var ipAddress = BitConverter.ToInt32(IPAddress.Parse(ipAndPrefix[0]).GetAddressBytes(), 0);
                    var cidrAddress = BitConverter.ToInt32(IPAddress.Parse(ipValue).GetAddressBytes(), 0);
                    var cidrPrefix = IPAddress.HostToNetworkOrder(-1 << (32 - int.Parse(ipAndPrefix[1])));
                    return ((ipAddress & cidrPrefix) == (cidrAddress & cidrPrefix));
                } catch {
                    return false;
                }
            }
        }
    }
}

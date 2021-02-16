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
using System.Collections.Generic;
using System.Text.Json.Serialization;
using LambdaSharp.Build.Internal;

namespace LambdaSharp.Build.CSharp {

    public class InvocationTargetDefinition {

        //--- Properties ---
        public string? Assembly { get; set; }
        public string? Type { get; set; }
        public string? Method { get; set; }
        public string? OperationName { get; set; }
        public string? RequestContentType { get; set; }

        [JsonConverter(typeof(JsonToNativeConverter))]
        public object? RequestSchema { get; set; }
        public string? RequestSchemaName { get; set; }
        public Dictionary<string, bool>? UriParameters { get; set; }
        public string? ResponseContentType { get; set; }

        [JsonConverter(typeof(JsonToNativeConverter))]
        public object? ResponseSchema { get; set; }
        public string? ResponseSchemaName { get; set; }
        public string? Error { get; set; }
        public string? StackTrace { get; set; }

        //--- Methods ---
        public string GetRequestSchemaName() {
            switch(RequestSchema) {
            case null:
                return "@undefined";
            case string value:
                return $"@{value.ToLowerInvariant()}";
            case Dictionary<string, object?> schema:
                return (string?)schema["title"] ?? throw new NotSupportedException($"invalid ResponseSchema: {schema["title"]}");
            default:
                throw new ApplicationException($"unexpected RequestSchema type: {RequestSchema.GetType()}");
            }
        }

        public string GetResponseSchemaName() {
            switch(ResponseSchema) {
            case null:
                return "@undefined";
            case string value:
                return $"@{value.ToLowerInvariant()}";
            case Dictionary<string, object?> schema:
                return (string?)schema["title"] ?? throw new NotSupportedException($"invalid ResponseSchema: {schema["title"]}");
            default:
                throw new ApplicationException($"unexpected ResponseSchema type: {ResponseSchema.GetType()}");
            }
        }
    }
}

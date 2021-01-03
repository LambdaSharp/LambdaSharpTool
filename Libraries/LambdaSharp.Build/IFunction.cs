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

namespace LambdaSharp.Build {

    public interface IFunction {

        //--- Properties ---
        string LogicalId { get; }
        string FullName { get; }
        string Name { get; }
        string Project { get; }
        string? Handler { get; }
        bool HasAssemblyValidation { get; }
        bool HasHandlerValidation { get; }
        IEnumerable<IFunctionRestApiSource> RestApiSources { get; }
        IEnumerable<IFunctionWebSocketSource> WebSocketSources { get; }
    }

    public interface IFunctionInvocationSource {

        //--- Properties ---
        string? OperationName { get; set; }
        string? RequestContentType { get; set; }
        object? RequestSchema { get; set; }
        string? RequestSchemaName { get; set; }
        string? ResponseContentType { get; set; }
        object? ResponseSchema { get; set; }
        string? ResponseSchemaName { get; set; }
    }

    public interface IFunctionRestApiSource : IFunctionInvocationSource {

        //--- Properties ---
        string HttpMethod { get; }
        string[] Path { get; }
        Dictionary<string, bool>? QueryStringParameters { get; set; }
        string Invoke { get; }
    }

    public interface IFunctionWebSocketSource : IFunctionInvocationSource {

        //--- Properties ---
        string RouteKey { get; }
        string Invoke { get; }
    }
}
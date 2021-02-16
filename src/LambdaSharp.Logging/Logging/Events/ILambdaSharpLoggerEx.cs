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
using System.Text.Json;
using LambdaSharp.Logging.Events.Models;
using LambdaSharp.Logging.Internal;

namespace LambdaSharp.Logging.Events {

    /// <summary>
    /// <see cref="ILambdaSharpLoggerEx"/> adds logging functionality as extension methods to the <see cref="ILambdaSharpLogger"/> interface.
    /// </summary>
    /// <seealso cref="LambdaLogLevel"/>
    public static class ILambdaSharpLoggerEx {

        //--- Class Fields ---
        private static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            IgnoreNullValues = true,
            WriteIndented = false
        };

        //--- Extension Methods ---

        /// <summary>
        /// Send a CloudWatch event with optional event details and resources it applies to. This event is forwarded to the configured EventBridge. The 'detail-type' property is set to the full type name of the detail value.
        /// </summary>
        /// <param name="logger">The <see cref="ILambdaSharpLogger"/> instance to use.</param>
        /// <param name="source">Name of the event source.</param>
        /// <param name="detail">Data-structure to serialize as a JSON string. If value is already a <code>string</code>, it is sent as-is. There is no other schema imposed. The data-structure may contain fields and nested subobjects.</param>
        /// <param name="resources">Optional AWS or custom resources, identified by unique identifier (e.g. ARN), which the event primarily concerns. Any number, including zero, may be present.</param>
        public static void LogEvent<T>(this ILambdaSharpLogger logger, string source, T detail, IEnumerable<string> resources = null)
            => LogEvent<T>(logger, source, typeof(T).FullName, detail, resources);

        /// <summary>
        /// Send a CloudWatch event with optional event details and resources it applies to. This event is forwarded to the configured EventBridge.
        /// </summary>
        /// <param name="logger">The <see cref="ILambdaSharpLogger"/> instance to use.</param>
        /// <param name="source">Name of the event source.</param>
        /// <param name="detailType">Free-form string used to decide what fields to expect in the event detail.</param>
        /// <param name="detail">Data-structure to serialize as a JSON string. If value is already a <code>string</code>, it is sent as-is. There is no other schema imposed. The data-structure may contain fields and nested subobjects.</param>
        /// <param name="resources">Optional AWS or custom resources, identified by unique identifier (e.g. ARN), which the event primarily concerns. Any number, including zero, may be present.</param>
        public static void LogEvent<T>(this ILambdaSharpLogger logger, string source, string detailType, T detail, IEnumerable<string> resources = null) {

            // augment event resources with LambdaSharp specific resources
            var lambdaResources = new List<string>();
            if(resources != null) {
                lambdaResources.AddRange(resources);
            }
            if(logger.Info.ModuleId != null) {
                lambdaResources.Add($"lambdasharp:stack:{logger.Info.ModuleId}");
            }
            var moduleFullName = logger.Info.GetModuleFullName();
            if(moduleFullName != null) {
                lambdaResources.Add($"lambdasharp:module:{moduleFullName}");
            }
            if(logger.Info.DeploymentTier != null) {
                lambdaResources.Add($"lambdasharp:tier:{logger.Info.DeploymentTier}");
            }
            if(logger.Info.ModuleInfo != null) {
                lambdaResources.Add($"lambdasharp:moduleinfo:{logger.Info.ModuleInfo}");
            }
            var moduleOrigin = logger.Info.GetModuleOrigin();
            if(moduleOrigin != null) {
                lambdaResources.Add($"lambdasharp:origin:{moduleOrigin}");
            }
            if(logger.Info.AppId != null) {
                lambdaResources.Add($"lambdasharp:app:{logger.Info.AppId}");
            }

            // create event record for logging
            var eventRecord = new LambdaEventRecord {
                EventBus = "default",
                Source = source,
                DetailType = detailType,
                Detail = (detail is string detailText)
                    ? detailText
                    : JsonSerializer.Serialize(detail, JsonSerializerOptions),
                Resources = lambdaResources
            };
            eventRecord.SetTime(DateTimeOffset.UtcNow);
            logger.LogRecord(eventRecord);
        }
    }
}

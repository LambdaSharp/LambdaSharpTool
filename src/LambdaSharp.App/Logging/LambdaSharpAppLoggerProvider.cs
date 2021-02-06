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
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LambdaSharp.App.Logging {

    /// <summary>
    /// The <see cref="LambdaSharpAppLoggerProvider"/> class implements the <see cref="ILoggerProvider"/> interface.
    /// </summary>
    internal class LambdaSharpAppLoggerProvider : ILoggerProvider {

        //--- Class Methods ---
        private static bool TryGetLogLevel(IConfigurationSection section, string category, out LogLevel logLevel) {

            // find the longest key that matches the category as a prefix in the logging section
            var children = section.GetChildren();
            foreach(var child in children.OrderByDescending(child => child.Key.Length)) {
                if(category.StartsWith(child.Key, StringComparison.Ordinal) && Enum.TryParse<LogLevel>(child.Value, ignoreCase: true, out var childLogLevel)) {
                    logLevel = childLogLevel;
                    return true;
                }
            }

            // find the default category in the logging section
            if(Enum.TryParse<LogLevel>(section.GetValue<string>("Default"), ignoreCase: true, out var defaultLogLevel)) {
                logLevel = defaultLogLevel;
                return true;
            }
            logLevel = LogLevel.None;
            return false;
        }

        //--- Fields ---
        private readonly ConcurrentDictionary<string, ILogger> _loggers = new ConcurrentDictionary<string, ILogger>();
        private readonly IConfiguration _configuration;
        private readonly LambdaSharpAppClient _lambdaSharpAppApiClient;

        //--- Constructors ---

        /// <summary>
        /// Initializes a new <see cref="LambdaSharpAppLoggerProvider"/> instance using the application and LambdaSharp configuration.
        /// </summary>
        /// <param name="configuration">The <see cref="IConfiguration"/> instance for the application.</param>
        /// <param name="lambdaSharpAppApiClient">The <see cref="LambdaSharpAppClient"/> instance for the application.</param>
        public LambdaSharpAppLoggerProvider(
            IConfiguration configuration,
            LambdaSharpAppClient lambdaSharpAppApiClient
        ) {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _lambdaSharpAppApiClient = lambdaSharpAppApiClient ?? throw new ArgumentNullException(nameof(lambdaSharpAppApiClient));
        }

        //--- ILoggerProvider Members ---

        /// <summary>
        /// Creates a new <see cref="Microsoft.Extensions.Logging.ILogger"/> instance.
        /// </summary>
        /// <param name="category">The category name for messages produced by the logger.</param>
        /// <returns>The instance of <see cref="Microsoft.Extensions.Logging.ILogger"/> that was created.</returns>
        public ILogger CreateLogger(string category)
            => _loggers.GetOrAdd(category, name => {
                LogLevel logLevel;

                // search for logging level configuration in this order:
                // 1) Logging:LambdaSharp:LogLevel:{category}*
                // 2) Logging:LambdaSharp:LogLevel:Default
                // 3) Logging:LogLevel:{category}*
                // 4) Logging:LogLevel:Default
                // if no configuration is found, default to 'Information' level
                if(
                    !TryGetLogLevel(_configuration.GetSection("Logging:LambdaSharp:LogLevel"), category, out logLevel)
                    && !TryGetLogLevel(_configuration.GetSection("Logging:LogLevel"), category, out logLevel)
                ) {

                    // could not find a logging configuration, use 'Information' as default level
                    logLevel = LogLevel.Information;
                }
                return new LambdaSharpAppLogger(logLevel, name, _lambdaSharpAppApiClient);
            });

        //--- IDisposable Members ---
        void IDisposable.Dispose() => _loggers.Clear();
    }
}

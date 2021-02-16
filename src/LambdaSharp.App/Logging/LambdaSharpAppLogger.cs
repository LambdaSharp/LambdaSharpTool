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
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace LambdaSharp.App.Logging {

    /// <summary>
    /// The <see cref="LambdaSharpAppLogger"/> class implements the <see cref="ILogger"/> interface.
    /// </summary>
    internal class LambdaSharpAppLogger : ILogger {

        //--- Types ---
        private class Scope : IDisposable {

            //--- Fields ---
            private readonly LambdaSharpAppLogger _logger;

            //--- Constructors ---
            public Scope(LambdaSharpAppLogger logger) => _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            //--- IDisposable Members ---
            void IDisposable.Dispose() => _logger.CloseScope();
        }


        //--- Fields ---
        private readonly LogLevel _logLevel;
        private readonly string _category;
        private readonly LambdaSharpAppClient _lambdaSharpAppApiClient;
        private readonly Stack<object> _scopes = new Stack<object>();

        //--- Constructors ---

        /// <summary>
        /// Initializes a new <see cref="LambdaSharpAppLogger"/> instance.
        /// </summary>
        /// <param name="logLevel">Entry will be written on this level.</param>
        /// <param name="category">The category name for messages produced by the logger.</param>
        /// <param name="lambdaSharpAppApiClient">The <see cref="LambdaSharpAppClient"/> instance to use for logging.</param>
        public LambdaSharpAppLogger(LogLevel logLevel, string category, LambdaSharpAppClient lambdaSharpAppApiClient) {
            _logLevel = logLevel;
            _category = category;
            _lambdaSharpAppApiClient = lambdaSharpAppApiClient ?? throw new ArgumentNullException(nameof(lambdaSharpAppApiClient));
        }

        //--- Methods ---

        /// <summary>
        /// Begins a logical operation scope.
        /// </summary>
        /// <param name="state">The identifier for the scope.</param>
        /// <typeparam name="TState">The type of the state to begin scope for.</typeparam>
        /// <returns>An System.IDisposable that ends the logical operation scope on dispose.</returns>
        public IDisposable BeginScope<TState>(TState state) {
            _scopes.Push(state);
            return new Scope(this);
        }

        /// <summary>
        /// Checks if the given logLevel is enabled.
        /// </summary>
        /// <param name="logLevel">level to be checked.</param>
        /// <returns>true if enabled.</returns>
        public bool IsEnabled(LogLevel logLevel) => (_logLevel != LogLevel.None) && (logLevel >= _logLevel);

        /// <summary>
        /// Writes a log entry.
        /// </summary>
        /// <param name="logLevel">Entry will be written on this level.</param>
        /// <param name="eventId">Id of the event.</param>
        /// <param name="state">The entry to be written. Can be also an object.</param>
        /// <param name="exception">The exception related to this entry.</param>
        /// <param name="formatter">Function to create a System.String message of the state and exception.</param>
        /// <typeparam name="TState">The type of the object to be written.</typeparam>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) {
            if(IsEnabled(logLevel)) {

                // render original message
                var message = formatter?.Invoke(state, exception) ?? "";

                // convert category and scopes into prefixes
                var scopes = Enumerable.Empty<string>();
                if(_category != null) {
                    scopes = scopes.Append(_category);
                }
                scopes = scopes.Union(Enumerable.Reverse(_scopes).Select(scope => (scope is string scopeString)
                    ? scopeString
                    : JsonSerializer.Serialize(scope)
                ));
                scopes = scopes.Append(message);

                // emit fully composed message
                _lambdaSharpAppApiClient.Log(logLevel, echo: false, exception, string.Join(" => ", scopes));
            }
        }

        private void CloseScope() => _scopes.Pop();
    }

    /// <summary>
    /// The <see cref="LambdaSharpAppLogger{T}"/> class uses the name of the type parameter to initialize the category name for <see cref="LambdaSharpAppLogger"/> base class.
    /// </summary>
    internal class LambdaSharpAppLogger<T> : LambdaSharpAppLogger {

        //--- Constructors ---

        /// <summary>
        /// Initializes a new <see cref="LambdaSharpAppLogger{T}"/> instance with <code>category</code> set to <code>typeof(T).FullName</code>.
        /// </summary>
        /// <param name="logLevel">Entry will be written on this level.</param>
        /// <param name="lambdaSharpAppApiClient">The <see cref="LambdaSharpAppClient"/> instance to use for logging.</param>
        public LambdaSharpAppLogger(LogLevel logLevel, LambdaSharpAppClient lambdaSharpAppApiClient) : base(logLevel, typeof(T).FullName, lambdaSharpAppApiClient) { }
    }
}

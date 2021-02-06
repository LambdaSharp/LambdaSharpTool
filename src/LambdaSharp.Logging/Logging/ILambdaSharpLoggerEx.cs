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

namespace LambdaSharp.Logging {

    /// <summary>
    /// <see cref="ILambdaSharpLoggerEx"/> adds logging functionality as extension methods to the <see cref="ILambdaSharpLogger"/> interface.
    /// </summary>
    /// <seealso cref="LambdaLogLevel"/>
    public static class ILambdaSharpLoggerEx {

        //--- Extension Methods ---

        /// <summary>
        /// Log a debugging message. This message will only appear in the log and will not be forwarded to an error aggregator.
        /// </summary>
        /// <param name="logger">The <see cref="ILambdaSharpLogger"/> instance to use.</param>
        /// <param name="format">The message format string. If not arguments are supplied, the message format string will be printed as a plain string.</param>
        /// <param name="arguments">Optional arguments for the message string.</param>
        /// <seealso cref="LambdaLogLevel"/>
        public static void LogDebug(this ILambdaSharpLogger logger, string format, params object[] arguments){
            if(logger.DebugLoggingEnabled) {
                logger.Log(LambdaLogLevel.DEBUG, exception: null, format: format, arguments: arguments);
            }
        }

        /// <summary>
        /// Log an informational message. This message will only appear in the log and will not be forwarded to an error aggregator.
        /// </summary>
        /// <param name="logger">The <see cref="ILambdaSharpLogger"/> instance to use.</param>
        /// <param name="format">The message format string. If not arguments are supplied, the message format string will be printed as a plain string.</param>
        /// <param name="arguments">Optional arguments for the message string.</param>
        /// <seealso cref="LambdaLogLevel"/>
        public static void LogInfo(this ILambdaSharpLogger logger, string format, params object[] arguments)
            => logger.Log(LambdaLogLevel.INFO, exception: null, format: format, arguments: arguments);

        /// <summary>
        /// Log a warning message. This message will be reported if an error aggregator is configured for the <c>LambdaSharp.Core</c> module.
        /// </summary>
        /// <param name="logger">The <see cref="ILambdaSharpLogger"/> instance to use.</param>
        /// <param name="format">The message format string. If not arguments are supplied, the message format string will be printed as a plain string.</param>
        /// <param name="arguments">Optional arguments for the message string.</param>
        /// <seealso cref="LambdaLogLevel"/>
        public static void LogWarn(this ILambdaSharpLogger logger, string format, params object[] arguments)
            => logger.Log(LambdaLogLevel.WARNING, exception: null, format: format, arguments: arguments);

        /// <summary>
        /// Log an exception as an error. This message will be reported if an error aggregator is configured for the <c>LambdaSharp.Core</c> module.
        /// </summary>
        /// <param name="logger">The <see cref="ILambdaSharpLogger"/> instance to use.</param>
        /// <param name="exception">The exception to log. The exception is logged with its message, stacktrace, and any nested exceptions.</param>
        /// <seealso cref="LambdaLogLevel"/>
        public static void LogError(this ILambdaSharpLogger logger, Exception exception)
            => logger.Log(LambdaLogLevel.ERROR, exception, exception.Message, Array.Empty<object>());

        /// <summary>
        /// Log an exception with a custom message as an error. This message will be reported if an error aggregator is configured for the <c>LambdaSharp.Core</c> module.
        /// </summary>
        /// <param name="logger">The <see cref="ILambdaSharpLogger"/> instance to use.</param>
        /// <param name="exception">The exception to log. The exception is logged with its message, stacktrace, and any nested exceptions.</param>
        /// <param name="format">Optional message to use instead of <c>Exception.Message</c>. This parameter can be <c>null</c>.</param>
        /// <param name="arguments">Optional arguments for the <c>format</c> parameter.</param>
        /// <seealso cref="LambdaLogLevel"/>
        public static void LogError(this ILambdaSharpLogger logger, Exception exception, string format, params object[] arguments)
            => logger.Log(LambdaLogLevel.ERROR, exception, format, arguments);

        /// <summary>
        /// Log an exception as an information message. This message will only appear in the log and will not be forwarded to an error aggregator.
        /// </summary>
        /// <remarks>
        /// Only use this method when the exception has no operational impact.
        /// Otherwise, either use <see cref="LogError(ILambdaSharpLogger,Exception)"/> or <see cref="LogErrorAsWarning(ILambdaSharpLogger,Exception)"/>.
        /// </remarks>
        /// <param name="logger">The <see cref="ILambdaSharpLogger"/> instance to use.</param>
        /// <param name="exception">The exception to log. The exception is logged with its message, stacktrace, and any nested exceptions.</param>
        /// <seealso cref="LambdaLogLevel"/>
        public static void LogErrorAsInfo(this ILambdaSharpLogger logger, Exception exception)
            => logger.Log(LambdaLogLevel.INFO, exception, exception.Message, Array.Empty<object>());

        /// <summary>
        /// Log an exception with a custom message as an information message. This message will only appear in the log and will not be forwarded to an error aggregator.
        /// </summary>
        /// <remarks>
        /// Only use this method when the exception has no operational impact.
        /// Otherwise, either use <see cref="LogError(ILambdaSharpLogger,Exception)"/> or <see cref="LogErrorAsWarning(ILambdaSharpLogger,Exception)"/>.
        /// </remarks>
        /// <param name="logger">The <see cref="ILambdaSharpLogger"/> instance to use.</param>
        /// <param name="exception">The exception to log. The exception is logged with its message, stacktrace, and any nested exceptions.</param>
        /// <param name="format">Optional message to use instead of <c>Exception.Message</c>. This parameter can be <c>null</c>.</param>
        /// <param name="arguments">Optional arguments for the <c>format</c> parameter.</param>
        /// <seealso cref="LambdaLogLevel"/>
        public static void LogErrorAsInfo(this ILambdaSharpLogger logger, Exception exception, string format, params object[] arguments)
            => logger.Log(LambdaLogLevel.INFO, exception, format, arguments);

        /// <summary>
        /// Log an exception as a warning. This message will be reported if an error aggregator is configured for the <c>LambdaSharp.Core</c> module.
        /// </summary>
        /// <remarks>
        /// Only use this method when the exception has no operational impact.
        /// Otherwise, either use <see cref="LogError(ILambdaSharpLogger,Exception)"/>.
        /// </remarks>
        /// <param name="logger">The <see cref="ILambdaSharpLogger"/> instance to use.</param>
        /// <param name="exception">The exception to log. The exception is logged with its message, stacktrace, and any nested exceptions.</param>
        /// <seealso cref="LambdaLogLevel"/>
        public static void LogErrorAsWarning(this ILambdaSharpLogger logger, Exception exception)
            => logger.Log(LambdaLogLevel.WARNING, exception, exception.Message, Array.Empty<object>());

        /// <summary>
        /// Log an exception with a custom message as a warning. This message will be reported if an error aggregator is configured for the <c>LambdaSharp.Core</c> module.
        /// </summary>
        /// <remarks>
        /// Only use this method when the exception has no operational impact.
        /// Otherwise, either use <see cref="LogError(ILambdaSharpLogger,Exception)"/>.
        /// </remarks>
        /// <param name="logger">The <see cref="ILambdaSharpLogger"/> instance to use.</param>
        /// <param name="exception">The exception to log. The exception is logged with its message, stacktrace, and any nested exceptions.</param>
        /// <param name="format">Optional message to use instead of <c>Exception.Message</c>. This parameter can be <c>null</c>.</param>
        /// <param name="arguments">Optional arguments for the <c>format</c> parameter.</param>
        /// <seealso cref="LambdaLogLevel"/>
        public static void LogErrorAsWarning(this ILambdaSharpLogger logger, Exception exception, string format, params object[] arguments)
            => logger.Log(LambdaLogLevel.WARNING, exception, format, arguments);

        /// <summary>
        /// Log a fatal exception with a custom message. This message will be reported if an error aggregator is configured for the <c>LambdaSharp.Core</c> module.
        /// </summary>
        /// <param name="logger">The <see cref="ILambdaSharpLogger"/> instance to use.</param>
        /// <param name="exception">The exception to log. The exception is logged with its message, stacktrace, and any nested exceptions.</param>
        /// <param name="format">Optional message to use instead of <c>Exception.Message</c>. This parameter can be <c>null</c>.</param>
        /// <param name="arguments">Optional arguments for the <c>format</c> parameter.</param>
        /// <seealso cref="LambdaLogLevel"/>
        public static void LogFatal(this ILambdaSharpLogger logger, Exception exception, string format, params object[] arguments)
            => logger.Log(LambdaLogLevel.FATAL, exception, format, arguments);
    }
}

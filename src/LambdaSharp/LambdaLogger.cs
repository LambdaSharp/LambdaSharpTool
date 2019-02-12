/*
 * MindTouch λ#
 * Copyright (C) 2018-2019 MindTouch, Inc.
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

namespace LambdaSharp {

    public enum LambdaLogLevel {
        INFO,
        WARNING,
        ERROR,
        FATAL
    }

    public interface ILambdaLogger {

        //--- Methods ---
        void Log(LambdaLogLevel level, Exception exception, string format, params object[] args);
    }

    public static class ILambdaLoggerEx {

        //--- Extension Methods ---
        public static void LogInfo(this ILambdaLogger logger, string format, params object[] args)
            => logger.Log(LambdaLogLevel.INFO, exception: null, format: format, args: args);

        public static void LogWarn(this ILambdaLogger logger, string format, params object[] args)
            => logger.Log(LambdaLogLevel.WARNING, exception: null, format: format, args: args);

        public static void LogError(this ILambdaLogger logger, Exception exception)
            => logger.Log(LambdaLogLevel.ERROR, exception, exception.Message, new object[0]);

        public static void LogError(this ILambdaLogger logger, Exception exception, string format, params object[] args)
            => logger.Log(LambdaLogLevel.ERROR, exception, format, args);

        public static void LogErrorAsInfo(this ILambdaLogger logger, Exception exception)
            => logger.Log(LambdaLogLevel.INFO, exception, exception.Message, new object[0]);

        public static void LogErrorAsInfo(this ILambdaLogger logger, Exception exception, string format, params object[] args)
            => logger.Log(LambdaLogLevel.INFO, exception, format, args);

        public static void LogErrorAsWarning(this ILambdaLogger logger, Exception exception)
            => logger.Log(LambdaLogLevel.WARNING, exception, exception.Message, new object[0]);

        public static void LogErrorAsWarning(this ILambdaLogger logger, Exception exception, string format, params object[] args)
            => logger.Log(LambdaLogLevel.WARNING, exception, format, args);

        public static void LogFatal(this ILambdaLogger logger, Exception exception, string format, params object[] args)
            => logger.Log(LambdaLogLevel.FATAL, exception, format, args);
    }
}

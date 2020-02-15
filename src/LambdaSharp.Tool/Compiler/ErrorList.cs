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

#nullable disable

using System;
using System.Collections.Generic;

namespace LambdaSharp.Tool.Compiler {

    public interface ILogger {

        //--- Methods ---
        void Log(IBuildReportEntry entry, SourceLocation sourceLocation, bool exact);
    }

    public static class ILoggerEx {

        //--- Methods ---
        public static void Log(this ILogger logger, IBuildReportEntry entry) => logger.Log(entry, sourceLocation: null, exact: true);
        public static void Log(this ILogger logger, IBuildReportEntry entry, SourceLocation sourceLocation) => logger.Log(entry, sourceLocation, exact: true);
        public static void LogInfoVerbose(this ILogger logger, string message, SourceLocation sourceLocation, bool exact) => logger.Log(new Verbose(message), sourceLocation, exact);
        public static void LogInfoVerbose(this ILogger logger, string message) => logger.LogInfoVerbose(message, sourceLocation: null, exact: true);
        public static void LogInfoPerformance(this ILogger logger, string message, TimeSpan duration, bool? cached, SourceLocation sourceLocation, bool exact) => logger.Log(new Timing(message, duration, cached), sourceLocation, exact);
        public static void LogInfoPerformance(this ILogger logger, string message, TimeSpan duration, bool? cached) => logger.LogInfoPerformance(message, duration, cached, sourceLocation: null, exact: true);
    }

    public enum BuildReportEntrySeverity {
        Timing,
        Verbose,
        Info,
        Warning,
        Error,
        Fatal
    }

    public interface IBuildReportEntry {

        //--- Properties ---
        int Code { get; }
        string Message { get; }
        BuildReportEntrySeverity Severity { get; }
    }

    public class BuildReportLogger : ILogger {

        //--- Fields ---
        private readonly List<string> _messages = new List<string>();

        //--- Properties ---
        public IEnumerable<string> Messages => _messages;

        //--- Methods ---
        public void Log(IBuildReportEntry entry, SourceLocation sourceLocation, bool exact) {

            // TODO: message should not be captured as strings, which makes further formatting impossible (such as colorization)
            var label = entry.Severity.ToString().ToUpperInvariant();
            if(sourceLocation == null) {
                _messages.Add($"{label}{entry.Code}: {entry.Message}");
            } else if(exact) {
                _messages.Add($"{label}{entry.Code}: {entry.Message} @ {sourceLocation.FilePath ?? "n/a"}({sourceLocation.LineNumberStart},{sourceLocation.ColumnNumberStart})");
            } else {
                _messages.Add($"{label}{entry.Code}: {entry.Message} @ (near) {sourceLocation.FilePath ?? "n/a"}({sourceLocation.LineNumberStart},{sourceLocation.ColumnNumberStart})");
            }
        }
    }
}

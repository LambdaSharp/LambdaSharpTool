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
using LambdaSharp.CloudFormation.Builder;

namespace LambdaSharp.CloudFormation.Reporting {

    public interface IReportEntry {

        //--- Properties ---
        int Code { get; }
        string Message { get; }
        string Severity { get; }
        SourceLocation Location { get; }
    }

    public sealed class ReportEntry : IReportEntry {

        //--- Class Methods ---
        public static IReportEntry Debug(string message, SourceLocation location) => new ReportEntry("DEBUG", code: 0, message, location);
        public static IReportEntry Warning(string message, SourceLocation location) => new ReportEntry("WARN", code: 0, message, location);
        public static IReportEntry Error(string message, SourceLocation location) => new ReportEntry("ERROR", code: 0, message, location);
        public static IReportEntry Fatal(string message, SourceLocation location) => new ReportEntry("FATAL", code: 0, message, location);

        //--- Constructors ---
        private ReportEntry(string severity, int code, string message, SourceLocation location) {
            Code = code;
            Severity = severity ?? throw new ArgumentNullException(nameof(severity));
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Location = location ?? throw new ArgumentNullException(nameof(location));
        }

        //--- Properties ---
        public int Code { get; }
        public string Message { get; }
        public string Severity { get; }
        public SourceLocation Location { get; }

        //--- Methods ---
        public override bool Equals(object? obj) => (obj is ReportEntry other) && Equals(other);

        public override int GetHashCode()
            => HashCode.Combine(Code.GetHashCode(), Message.GetHashCode(), Severity.GetHashCode(), Location.GetHashCode());

        public bool Equals(ReportEntry other)
            => (other != null)
                && (Code == other.Code)
                && (Message == other.Message)
                && (Severity == other.Severity)
                && (Location == other.Location);
    }
}

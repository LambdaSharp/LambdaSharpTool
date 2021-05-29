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

    public sealed class InfoEntry : IReportEntry {

        //--- Constructors ---
        public InfoEntry(string message) => Message = message ?? throw new ArgumentNullException(nameof(message));

        //--- Properties ---
        public int Code => 0;
        public string Message { get; }
        public string Severity => "INFO";
        public SourceLocation Location => SourceLocation.Empty;

        //--- Methods ---
        public override bool Equals(object? obj)
            => (obj is InfoEntry other) && Equals(other);

        public override int GetHashCode()
            => HashCode.Combine(Code.GetHashCode(), Message.GetHashCode(), Severity.GetHashCode(), Location.GetHashCode());

        public bool Equals(VerboseEntry other)
            => (other != null)
                && (Code == other.Code)
                && (Message == other.Message)
                && (Severity == other.Severity)
                && (Location == other.Location);
    }
}

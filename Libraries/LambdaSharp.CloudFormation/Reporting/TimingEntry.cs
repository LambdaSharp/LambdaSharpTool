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

    public sealed class TimingEntry : IReportEntry {

        //--- Constructors ---
        public TimingEntry(string description, TimeSpan duration, bool? cached) {
            Description = description ?? throw new ArgumentNullException(nameof(description));
            Duration = duration;
            Cached = cached;
            Message = $"{Description} [duration={Duration.TotalSeconds:N2}s{(Cached.HasValue ? $", cached={Cached.Value.ToString().ToLowerInvariant()}" : "")}]";
        }

        //--- Properties ---
        public int Code => 0;
        public string Message { get;}
        public string Severity => "TIMING";
        public SourceLocation Location => SourceLocation.Empty;
        public string Description { get; }
        public TimeSpan Duration { get; }
        public bool? Cached { get; }

        //--- Methods ---
        public override bool Equals(object? obj)
            => (obj is TimingEntry other) && Equals(other);

        public override int GetHashCode()
            => HashCode.Combine(Code.GetHashCode(), Message.GetHashCode(), Severity.GetHashCode(), Location.GetHashCode());

        public bool Equals(TimingEntry other)
            => (other != null)
                && (Code == other.Code)
                && (Message == other.Message)
                && (Severity == other.Severity)
                && (Location == other.Location);
    }
}

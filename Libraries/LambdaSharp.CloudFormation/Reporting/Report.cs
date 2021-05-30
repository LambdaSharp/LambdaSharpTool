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
using System.Linq;

namespace LambdaSharp.CloudFormation.Reporting {

    public interface IReport {

        //--- Properties ---
        bool HasErrors => Entries.Any(entry => entry.IsError);
        IEnumerable<IReportEntry> Entries { get; }

        //--- Methods ---
        void Add(IReportEntry entry);
    }

    public class Report : IReport {

        //--- Fields ---
        private readonly List<IReportEntry> _entries = new List<IReportEntry>();

        //--- Public ---
        public IEnumerable<IReportEntry> Entries => _entries;

        //--- Methods ---
        public void Add(IReportEntry entry) {
            if(entry is null) {
                throw new System.ArgumentNullException(nameof(entry));
            }
            if(!_entries.Contains(entry)) {
                _entries.Add(entry);
            }
        }
    }
}

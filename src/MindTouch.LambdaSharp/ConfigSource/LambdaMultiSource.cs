/*
 * MindTouch λ#
 * Copyright (C) 2018 MindTouch, Inc.
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
using System.Collections.Generic;
using System.Linq;

namespace MindTouch.LambdaSharp.ConfigSource {
    public sealed class LambdaMultiSource : ILambdaConfigSource {

        //--- Fields ---
        private readonly IEnumerable<ILambdaConfigSource> _sources;

        //--- Constructors ---
        public LambdaMultiSource(IEnumerable<ILambdaConfigSource> sources) {
            _sources = sources ?? throw new ArgumentNullException(nameof(sources));
        }

        //--- Methods ---
        public ILambdaConfigSource Open(string key) {
            var sources = _sources.Select(x => x.Open(key)).ToArray();
            return new LambdaMultiSource(sources);
        }

        public string Read(string key) => _sources.Select(source => source.Read(key)).FirstOrDefault(result => result != null);

        public IEnumerable<string> ReadAllKeys() => _sources
            .Reverse()
            .Select(source => source.ReadAllKeys())
            .SelectMany(keys => keys)
            .Distinct(StringComparer.InvariantCultureIgnoreCase)
            .ToArray();
    }
}
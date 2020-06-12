/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2020
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
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LambdaSharp.CloudFormation.Converter {

    public class ProcessedCloudFormationSpecification {

        //--- Constructors ---
        public ProcessedCloudFormationSpecification(string region, JObject document, IEnumerable<string> warnings) {
            Region = region ?? throw new ArgumentNullException(nameof(region));
            Document = document ?? throw new ArgumentNullException(nameof(document));
            Warnings = warnings ?? throw new ArgumentNullException(nameof(warnings));
        }

        //--- Properties ---
        public string Region { get; }
        public JObject Document { get; }
        public IEnumerable<string> Warnings { get; }

        //--- Methods ---
        public async Task SaveAsync(string folder, bool compressed) {

            // write JSON document
            var text = Document.ToString(compressed ? Formatting.None : Formatting.Indented);
            await WriteAsync(Path.Combine(folder, $"{Region}.json"), text);

            // write JSON patch log
            await WriteAsync(Path.Combine(folder, $"{Region}.json.log"), string.Join("\n", Warnings));

            // local functions
            async Task WriteAsync(string filename, string contents) {
                if(compressed) {
                    using var file = File.OpenWrite(filename + ".br");
                    using var compression = new BrotliStream(file, CompressionLevel.Optimal);
                    var buffer = Encoding.UTF8.GetBytes(contents);
                    await compression.WriteAsync(buffer, 0, buffer.Length);
                } else {
                    await File.WriteAllTextAsync(filename, contents);
                }
            }
        }
    }
}
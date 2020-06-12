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

using System;
using System.IO;
using System.Text;

namespace Tests.LambdaSharp.Compiler {

    public static class ResourceReader {

        //--- Class Methods ---
        public static string ReadText(string filename) {
            using var resource = OpenStream(filename);
            using var reader = new StreamReader(resource, Encoding.UTF8);
            return reader.ReadToEnd().Replace("\r", "");
        }

        public static Stream OpenStream(string filename) {
            var assembly = typeof(ResourceReader).Assembly;
            var resourceName = $"{assembly.GetName().Name}.Resources.{filename.Replace(" ", "_").Replace("\\", ".").Replace("/", ".")}";
            return assembly.GetManifestResourceStream(resourceName) ?? throw new Exception($"unable to locate embedded resource: '{resourceName}' in assembly '{assembly.GetName().Name}'");
        }
    }
}
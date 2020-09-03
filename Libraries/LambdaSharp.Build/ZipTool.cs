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

using LambdaSharp.Build.Internal;

namespace LambdaSharp.Build {

    public class ZipTool : ABuildEventsSource {

        //--- Constructors ---
        public ZipTool(BuildEventsConfig? buildEventsConfig = null) : base(buildEventsConfig) { }

        //--- Methods ---
        public bool Zip(string zipArchivePath, string zipFolder, bool showOutput) {
            var zipTool = ProcessLauncher.ZipExe;
            if(string.IsNullOrEmpty(zipTool)) {
                LogError("failed to find the \"zip\" utility program in path. This program is required to maintain Linux file permissions in the zip archive.");
                return false;
            }
            return new ProcessLauncher(BuildEventsConfig).Execute(
                zipTool,
                new[] { "-r", zipArchivePath, "." },
                zipFolder,
                showOutput
            );
        }
    }
}
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

using System.Diagnostics;
using System.IO;

namespace LambdaSharp.Build.CSharp {

    public class DotNetTool : ABuildEventsSource {

        //--- Constructors ---
        public DotNetTool(BuildEventsConfig? buildEventsConfig = null) : base(buildEventsConfig) { }

        //--- Methods ---
        public string? GetDotNetVersion() {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try {
                var dotNetExe = ProcessLauncher.DotNetExe;
                if(string.IsNullOrEmpty(dotNetExe)) {
                    return null;
                }

                // read the dotnet version
                var process = new Process {
                    StartInfo = new ProcessStartInfo(dotNetExe, "--version") {
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        WorkingDirectory = Directory.GetCurrentDirectory()
                    }
                };
                try {
                    process.Start();
                    var dotnetVersion = process.StandardOutput.ReadToEnd().Trim();
                    process.WaitForExit();
                    if(process.ExitCode != 0) {
                        return null;
                    }
                    return dotnetVersion;
                } catch {
                    return null;
                }
            } finally {
                LogInfoPerformance($"GetDotNetVersion()", stopwatch.Elapsed);
            }
        }
    }
}
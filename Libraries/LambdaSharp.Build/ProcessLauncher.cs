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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;

namespace LambdaSharp.Build {

    public class ProcessLauncher : ABuildEventsSource {

        //--- Class Properties ---
        public static string? DotNetExe => McMaster.Extensions.CommandLineUtils.DotNetExe.FullPathOrDefault();
        public static string? ZipExe => FindExecutableInPath("zip");
        public static string? UnzipExe => FindExecutableInPath("unzip");
        public static string? Lash => FindExecutableInPath("lash");

        //--- Class Methods ---
        private static string? FindExecutableInPath(string command) {
            if(File.Exists(command)) {
                return Path.GetFullPath(command);
            }
            var envPath = Environment.GetEnvironmentVariable("PATH");
            if(envPath != null) {
                foreach(var path in envPath.Split(Path.PathSeparator)) {
                    try {
                        var fullPath = Path.Combine(RemoveQuotes(path), command);
                        if(File.Exists(fullPath)) {
                            return fullPath;
                        }
                    } catch {

                        // catch exceptions and continue if there are invalid characters in the user's path.
                    }
                }
            }
            switch(command) {
            case "zip":
                if(File.Exists("/usr/bin/zip")) {
                    return "/usr/bin/zip";
                }
                break;
            case "unzip":
                if(File.Exists("/usr/bin/unzip")) {
                    return "/usr/bin/unzip";
                }
                break;
            case "lash":

                // dotnet tools are installed under the user's home directory
                if(Environment.OSVersion.Platform == PlatformID.Unix) {
                    var homeDirectory = Environment.GetEnvironmentVariable("HOME");
                    if(homeDirectory != null) {
                        var lashPath = Path.Combine(homeDirectory, ".dotnet", "tools", "lash");
                        if(File.Exists(lashPath)) {
                            return lashPath;
                        }
                    }
                } else {
                    var homeDirectory = Environment.GetEnvironmentVariable("USERPROFILE");
                    if(homeDirectory != null) {
                        var lashPath = Path.Combine(homeDirectory, ".dotnet", "tools", "lash.exe");
                        if(File.Exists(lashPath)) {
                            return lashPath;
                        }
                    }
                }
                break;
            }
            return null;

            // local functions
            string RemoveQuotes(string text) {
                if(text.StartsWith("\"", StringComparison.Ordinal)) {
                    text = text.Substring(1);
                }
                if(text.EndsWith("\"", StringComparison.Ordinal)) {
                    text = text.Substring(0, text.Length - 1);
                }
                return text;
            }
        }

        //--- Constructors ---
        public ProcessLauncher(BuildEventsConfig? buildEventsConfig = null) : base(buildEventsConfig) { }

        //--- Methods ---
        public bool Execute(
            string application,
            IEnumerable<string> arguments,
            string? workingFolder,
            bool showOutput,
            Func<string, string?>? processOutputLine = null
        ) => Execute(application, ArgumentEscaper.EscapeAndConcatenate(arguments), workingFolder, showOutput, processOutputLine);

        public bool Execute(
            string application,
            string arguments,
            string? workingFolder,
            bool showOutput,
            Func<string, string?>? processOutputLine = null
        ) {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try {
                using(var process = new Process()) {
                    process.StartInfo = new ProcessStartInfo {
                        FileName = application,
                        Arguments = arguments,
                        WorkingDirectory = workingFolder,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                    process.Start();
                    process.EnableRaisingEvents = true;
                    var output = Task.Run(() => process.StandardOutput.ReadToEndAsync());
                    var error = Task.Run(() => process.StandardError.ReadToEndAsync());;
                    process.WaitForExit();
                    var success = (process.ExitCode == 0);
                    if(showOutput || !success) {
                        PrintLines(output.GetAwaiter().GetResult());
                        PrintLines(error.GetAwaiter().GetResult());
                    }
                    return success;
                }
            } finally {
                LogInfoPerformance($"Execute() '{Path.Combine(workingFolder ?? Directory.GetCurrentDirectory(), application)} {arguments}'", stopwatch.Elapsed);
            }

            // local functions
            void PrintLines(string buffer) {
                var currentPosition = 0;
                int lineBreakPosition;
                while(currentPosition < buffer.Length) {
                    lineBreakPosition = buffer.IndexOf('\n', currentPosition);
                    if(lineBreakPosition < 0) {
                        lineBreakPosition = buffer.Length - 1;
                    }
                    var line = buffer.Substring(currentPosition, lineBreakPosition - currentPosition + 1);
                    if(processOutputLine != null) {
                        var newLine = processOutputLine(line);
                        if(newLine != null) {
                            Console.Write(newLine);
                        }
                    } else {
                        Console.Write(line);
                    }
                    currentPosition = lineBreakPosition + 1;
                }
            }
        }

        public string? ExecuteWithOutputCapture(string application, IEnumerable<string> arguments, string? workingFolder) {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try {
                using(var process = new Process()) {
                    process.StartInfo = new ProcessStartInfo {
                        FileName = application,
                        Arguments = ArgumentEscaper.EscapeAndConcatenate(arguments),
                        WorkingDirectory = workingFolder,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                    process.Start();
                    process.EnableRaisingEvents = true;
                    Task<string>? output = null;
                    Task<string>? error = null;
                    output = Task.Run(() => process.StandardOutput.ReadToEndAsync());
                    error = Task.Run(() => process.StandardError.ReadToEndAsync());
                    process.WaitForExit();
                    return (process.ExitCode == 0)
                        ? output.GetAwaiter().GetResult()
                        : null;
                }
            } finally {
                LogInfoPerformance($"ExecuteWithOutputCapture() '{Path.Combine(workingFolder ?? Directory.GetCurrentDirectory(), application)} {string.Join(" ", arguments)}'", stopwatch.Elapsed);
            }
        }
    }
}
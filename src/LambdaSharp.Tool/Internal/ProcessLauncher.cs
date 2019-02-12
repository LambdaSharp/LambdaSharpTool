/*
 * MindTouch λ#
 * Copyright (C) 2018-2019 MindTouch, Inc.
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
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;

namespace LambdaSharp.Tool.Internal {

    internal class ProcessLauncher {

        //--- Class Properties ---
        public static string DotNetExe { get => McMaster.Extensions.CommandLineUtils.DotNetExe.FullPathOrDefault(); }
        public static string ZipExe { get => FindExecutableInPath("zip"); }
        public static string UnzipExe { get => FindExecutableInPath("unzip"); }

        //--- Class methods ---
        public static bool Execute(string application, IEnumerable<string> arguments, string workingFolder, bool showOutput) {
            using(var process = new Process()) {
                process.StartInfo = new ProcessStartInfo {
                    FileName = application,
                    Arguments = ArgumentEscaper.EscapeAndConcatenate(arguments),
                    WorkingDirectory = workingFolder,
                    UseShellExecute = false,
                    RedirectStandardOutput = !showOutput,
                    RedirectStandardError = !showOutput
                };
                process.Start();
                process.EnableRaisingEvents = true;
                Task<string> output = null;
                Task<string> error = null;
                if(!showOutput) {

                    // make sure to drain the redirected output
                    output = Task.Run(() => process.StandardOutput.ReadToEndAsync());
                    error = Task.Run(() => process.StandardError.ReadToEndAsync());
                }
                process.WaitForExit();
                var success = (process.ExitCode == 0);
                if(!success && !showOutput) {
                    Console.WriteLine(output.Result);
                    Console.WriteLine(error.Result);
                }
                return success;
            }
        }

        public static string FindExecutableInPath(string command) {
            if(File.Exists(command)) {
                return Path.GetFullPath(command);
            }
            var envPath = Environment.GetEnvironmentVariable("PATH");
            foreach(var path in envPath.Split(Path.PathSeparator)) {
                try {
                    var fullPath = Path.Combine(RemoveQuotes(path), command);
                    if(File.Exists(fullPath)) {
                        return fullPath;
                    }
                } catch(Exception) {

                    // catch exceptions and continue if there are invalid characters in the user's path.
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
            }
            return null;

            // local functions
            string RemoveQuotes(string text) {
                if(text.StartsWith("\"")) {
                    text = text.Substring(1);
                }
                if(text.EndsWith("\"")) {
                    text = text.Substring(0, text.Length - 1);
                }
                return text;
            }
        }
    }
}
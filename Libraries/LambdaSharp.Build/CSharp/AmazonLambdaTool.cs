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
using System.Text.RegularExpressions;

namespace LambdaSharp.Build.CSharp {

    public class AmazonLambdaTool : ABuildEventsSource {

        //--- Constants ---
        private const string MIN_AWS_LAMBDA_TOOLS_VERSION = "4.1.0";

        //--- Class Fields ---
        private static bool _dotnetLambdaToolVersionChecked;
        private static bool _dotnetLambdaToolVersionIsValid;

        //--- Constructors ---
        public AmazonLambdaTool(BuildEventsConfig? buildEventsConfig = null) : base(buildEventsConfig) { }

        //--- Class Methods ---
        public bool CheckIsInstalled() {

            // only run check once
            if(_dotnetLambdaToolVersionChecked) {
                return _dotnetLambdaToolVersionIsValid;
            }
            _dotnetLambdaToolVersionChecked = true;

            // check if dotnet executable can be found
            var dotNetExe = ProcessLauncher.DotNetExe;
            if(string.IsNullOrEmpty(dotNetExe)) {
                LogError("failed to find the \"dotnet\" executable in path.");
                return false;
            }

            // check if AWS Lambda Tools extension is installed
            var result = new ProcessLauncher(BuildEventsConfig).ExecuteWithOutputCapture(
                dotNetExe,
                new[] { "lambda", "tool", "help" },
                workingFolder: null
            );
            if(result == null) {

                // attempt to install the AWS Lambda Tools extension
                if(!new ProcessLauncher(BuildEventsConfig).Execute(
                    dotNetExe,
                    new[] { "tool", "install", "--global", "Amazon.Lambda.Tools" },
                    workingFolder: null,
                    showOutput: false
                )) {
                    LogError("'dotnet tool install --global Amazon.Lambda.Tools' command failed");
                    return false;
                }

                // latest version is now installed, we're good to proceed
                _dotnetLambdaToolVersionIsValid = true;
                return true;
            }

            // check version of installed AWS Lambda Tools extension
            var match = Regex.Match(result, @"\((?<Version>.*)\)");
            if(!match.Success || !Version.TryParse(match.Groups["Version"].Value, out var version)) {
                LogWarn($"proceeding with compilation using unknown version of 'Amazon.Lambda.Tools'; please ensure version {MIN_AWS_LAMBDA_TOOLS_VERSION} or later is installed");
            } else if(version < Version.Parse(MIN_AWS_LAMBDA_TOOLS_VERSION)) {

                // attempt to install the AWS Lambda Tools extension
                if(!new ProcessLauncher(BuildEventsConfig).Execute(
                    dotNetExe,
                    new[] { "tool", "update", "--global", "Amazon.Lambda.Tools" },
                    workingFolder: null,
                    showOutput: false
                )) {
                    LogError("'dotnet tool update -g Amazon.Lambda.Tools' command failed");
                    return false;
                }
            }
            _dotnetLambdaToolVersionIsValid = true;
            return true;
        }

        public string? GetAmazonLambdaToolVersion() {

            // check if dotnet executable can be found
            var dotNetExe = ProcessLauncher.DotNetExe;
            if(string.IsNullOrEmpty(dotNetExe)) {
                return null;
            }

            // check if Amazon Lambda Tools extension is installed
            var result = new ProcessLauncher(BuildEventsConfig).ExecuteWithOutputCapture(
                dotNetExe,
                new[] { "lambda", "help" },
                workingFolder: null
            );
            if(result == null) {
                return null;
            }

            // parse version from Amazon Lambda Tools
            var match = Regex.Match(result, @"\((?<Version>.*)\)");
            if(!match.Success) {
                return null;
            }
            return match.Groups["Version"].Value;
        }
    }
}
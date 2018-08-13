/*
 * MindTouch λ#
 * Copyright (C) 2006-2018 MindTouch, Inc.
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
using System.Linq;
using Amazon.CloudFormation.Model;
using Amazon.S3.Transfer;
using McMaster.Extensions.CommandLineUtils;
using MindTouch.LambdaSharp.Tool.Cli;

namespace MindTouch.LambdaSharp.Tool {

    public enum VerboseLevel {
        Quiet,
        Normal,
        Detailed,
        Exceptions
    }

    public enum DryRunLevel {
        Everything,
        CloudFormation
    }
    
    public class Program : CliBase {

        //--- Class Methods ---
        public static int Main(string[] args) {
            var app = new CommandLineApplication(throwOnUnexpectedArg: false) {
                Name = "MindTouch.LambdaSharp.Tool",
                FullName = $"MindTouch LambdaSharp Tool (v{Version})",
                Description = "Project Home: https://github.com/LambdaSharp/LambdaSharpTool"
            };
            app.HelpOption();

            // register commands
            new CliInfoCommand().Register(app);
            new CliDeployCommand().Register(app);
            new CliNewCommand().Register(app);
            new CliListCommand().Register(app);

            // new command
            app.OnExecute(() => {
                Console.WriteLine(app.GetHelpText());
            });

            // execute command line options and report any errors
            try {
                app.Execute(args);
            } catch(Exception e) {
                AddError(e);
            }
            if(ErrorCount > 0) {
                Console.WriteLine();
                Console.WriteLine($"FAILED: {ErrorCount:N0} errors encountered");
                ShowErrors();
                return -1;
            }
            return 0;
        }

    }
}

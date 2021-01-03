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
using System.Diagnostics;
using McMaster.Extensions.CommandLineUtils;
using LambdaSharp.Tool.Cli;

namespace LambdaSharp.Tool {

    public enum VerboseLevel {
        Quiet,
        Normal,
        Detailed,
        Exceptions,
        Performance
    }

    public enum DryRunLevel {

        // compile module, build artifacts, publish module
        Everything,

        // compile module
        CloudFormation
    }

    public enum XRayTracingLevel {
        Disabled,
        RootModule,
        AllModules
    }

    public class Program : CliBase {

        //--- Class Fields ---
        public static bool Quiet;
        public static bool ShowHelp;
        public static TimeSpan BeepThreshold = TimeSpan.FromSeconds(10);
        private readonly static Stopwatch _beepStopwatch = Stopwatch.StartNew();

        //--- Class Methods ---
        public static int Main(string[] args) {
            var app = new CommandLineApplication(throwOnUnexpectedArg: false) {
                Name = Settings.Lash,
                FullName = $"LambdaSharp CLI (v{Version})",
                Description = "Project Home: https://github.com/LambdaSharp/LambdaSharpTool"
            };
            app.HelpOption();

            // register commands
            new CliInitCommand().Register(app);
            new CliTierCommand().Register(app);
            new CliInfoCommand().Register(app);
            new CliListCommand().Register(app);
            new CliNewCommand().Register(app);
            new CliBuildPublishDeployCommand().Register(app);
            new CliEncryptCommand().Register(app);
            new CliUtilCommand().Register(app);
            new CliNukeCommand().Register(app);

            // no command
            app.OnExecute(() => {
                ShowHelp = true;
                Console.WriteLine(app.GetHelpText());
            });

            // execute command line options and report any errors
            var stopwatch = Stopwatch.StartNew();
            ResetBeepTimer();
            using(Settings.AnsiTerminal = new AnsiTerminal()) {
                try {
                    int exitCode = 0;
                    try {
                        exitCode = app.Execute(args);
                    } catch(CommandParsingException e) {
                        LogError(e.Message);
                    } catch(Exception e) {
                        LogError(e);
                    }
                    if(Settings.HasErrors) {
                        if(!Quiet) {
                            Console.WriteLine();
                            Console.WriteLine($"FAILED: {Settings.ErrorCount:N0} errors encountered");
                        }
                        Settings.ShowErrors();
                        return -1;
                    }
                    if(Settings.HasWarnings) {
                        if(!Quiet) {
                            Console.WriteLine();
                        }
                        Settings.ShowErrors();
                    }
                    return exitCode;
                } finally {
                    if(!ShowHelp && !Quiet) {
                        Console.WriteLine();
                        Console.WriteLine($"Done (finished: {DateTime.Now}; duration: {stopwatch.Elapsed:c})");

                        // check execution was long enough to emit a sound
                        if(_beepStopwatch.Elapsed >= BeepThreshold) {
                            Console.Beep();
                        }
                    }
                }
            }
        }

        public static void ResetBeepTimer() => _beepStopwatch.Reset();
    }
}

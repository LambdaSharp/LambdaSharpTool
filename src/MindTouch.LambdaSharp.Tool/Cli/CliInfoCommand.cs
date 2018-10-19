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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.SimpleSystemsManagement;
using McMaster.Extensions.CommandLineUtils;
using MindTouch.LambdaSharp.Tool.Internal;

namespace MindTouch.LambdaSharp.Tool.Cli {

    public class CliInfoCommand : ACliCommand {

        //--- Methods ---
        public void Register(CommandLineApplication app) {
            app.Command("info", cmd => {
                cmd.HelpOption();
                cmd.Description = "Show LambdaSharp settings";

                // command options
                var initSettingsCallback = CreateSettingsInitializer(cmd);
                cmd.OnExecute(async () => {
                    Console.WriteLine($"{app.FullName} - {cmd.Description}");
                    var settings = await initSettingsCallback();
                    if(settings == null) {
                        return;
                    }
                    await Info(
                        settings,
                        Environment.GetEnvironmentVariable("LAMBDASHARP_TIER"),
                        GetGitShaValue(Directory.GetCurrentDirectory())
                    );
                });
            });
        }

        public async Task Info(Settings settings, string tier, string gitsha) {
            await PopulateToolSettingsAsync(settings);
            await PopulateEnvironmentSettingsAsync(settings, tier);

            // show LambdaSharp settings
            Console.WriteLine($"LambdaSharp Tool");
            Console.WriteLine($"    Profile: {settings.ToolProfile ?? "<NOT SET>"}");
            Console.WriteLine($"    Version: {settings.ToolVersion}");
            Console.WriteLine($"    Module Deployment S3 Bucket: {settings.DeploymentBucketName ?? "<NOT SET>"}");
            Console.WriteLine($"    Module Deployment S3 Path: {settings.DeploymentBucketPath ?? "<NOT SET>"}");
            Console.WriteLine($"    Module Deployment Notifications Topic: {settings.DeploymentNotificationsTopicArn ?? "<NOT SET>"}");
            Console.WriteLine($"LambdaSharp Environment");
            Console.WriteLine($"    Deployment Tier: {tier ?? "<NOT SET>"}");
            Console.WriteLine($"    Version: {settings.EnvironmentVersion?.ToString() ?? "<NOT SET>"}");
            Console.WriteLine($"Git SHA: {gitsha ?? "<NOT SET>"}");
            Console.WriteLine($"AWS");
            Console.WriteLine($"    Region: {settings.AwsRegion ?? "<NOT SET>"}");
            Console.WriteLine($"    Account Id: {((settings.AwsAccountId != null) ? "<SET>" : "<NOT SET>")}");
            Console.WriteLine($"Tools");
            Console.WriteLine($"    .Net Core CLI Version: {GetDotNetVersion() ?? "<NOT FOUND>"}");
            Console.WriteLine($"    Git CLI Version: {GetGitVersion() ?? "<NOT FOUND>"}");
        }

        private string GetDotNetVersion() {
            var dotNetExe = ProcessLauncher.DotNetExe;
            if(string.IsNullOrEmpty(dotNetExe)) {
                return null;
            }

            // read the dotnet version
            var process = new Process {
                StartInfo = new ProcessStartInfo(dotNetExe, "--version") {
                    RedirectStandardOutput = true,
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
        }

        private string GetGitVersion() {

            // constants
            const string GIT_VERSION_PREFIX = "git version ";

            // read the gitSha using `git` directly
            var process = new Process {
                StartInfo = new ProcessStartInfo("git", "--version") {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    WorkingDirectory = Directory.GetCurrentDirectory()
                }
            };
            try {
                process.Start();
                var gitVersion = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();
                if(process.ExitCode != 0) {
                    return null;
                }
                if(gitVersion.StartsWith(GIT_VERSION_PREFIX, StringComparison.Ordinal)) {
                    gitVersion = gitVersion.Substring(GIT_VERSION_PREFIX.Length);
                }
                return gitVersion;
            } catch {
                Console.WriteLine("WARNING: git is not installed; skipping git-sha fingerprint file");
                return null;
            }
        }
    }
}

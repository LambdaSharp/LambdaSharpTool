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
using System.IO;
using McMaster.Extensions.CommandLineUtils;

namespace LambdaSharp.Build {

    public class GitTool : ABuildEventsSource {

        //--- Constructors ---
        public GitTool(BuildEventsConfig? buildEventsConfig = null) : base(buildEventsConfig) { }

        //--- Methods ---
        public string? GetGitShaValue(string workingDirectory, bool showWarningOnFailure = true) {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try {

                // read the gitSha using 'git' directly
                var process = new Process {
                    StartInfo = new ProcessStartInfo("git", ArgumentEscaper.EscapeAndConcatenate(new[] { "rev-parse", "HEAD" })) {
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        WorkingDirectory = workingDirectory
                    }
                };

                // attempt to get git-sha value
                string? gitSha = null;
                try {
                    process.Start();
                    gitSha = process.StandardOutput.ReadToEnd().Trim();
                    process.WaitForExit();
                    if(process.ExitCode != 0) {
                        gitSha = null;
                        if(showWarningOnFailure) {
                            LogWarn($"unable to get git-sha 'git rev-parse HEAD' failed with exit code = {process.ExitCode}");
                        }
                    }
                } catch {
                    if(showWarningOnFailure) {
                        LogWarn("git is not installed; skipping git-sha detection");
                    }
                }
                if(gitSha == null) {
                    return null;
                }

                // check if folder contains uncommitted/untracked changes
                process = new Process {
                    StartInfo = new ProcessStartInfo("git", ArgumentEscaper.EscapeAndConcatenate(new[] { "status", "--porcelain" })) {
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        WorkingDirectory = workingDirectory
                    }
                };

                // attempt to get git status
                try {
                    process.Start();
                    var dirty = process.StandardOutput.ReadToEnd().Trim();
                    process.WaitForExit();
                    if(process.ExitCode != 0) {
                        if(showWarningOnFailure) {
                            LogWarn($"unable to get git status 'git status --porcelain' failed with exit code = {process.ExitCode}");
                        }
                    }

                    // check if any changes were detected
                    if(!string.IsNullOrEmpty(dirty)) {
                        gitSha = "DIRTY-" + gitSha;
                    }
                } catch {
                    if(showWarningOnFailure) {
                        LogWarn("git is not installed; skipping git status detection");
                    }
                }
                return gitSha;
            } finally {
                LogInfoPerformance($"GetGitShaValue() for '{workingDirectory}'", stopwatch.Elapsed);
            }
        }

        public string? GetGitBranch(string workingDirectory, bool showWarningOnFailure = true) {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try {

                // read the gitSha using 'git' directly
                var process = new Process {
                    StartInfo = new ProcessStartInfo("git", ArgumentEscaper.EscapeAndConcatenate(new[] { "rev-parse", "--abbrev-ref", "HEAD" })) {
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        WorkingDirectory = workingDirectory
                    }
                };
                string? gitBranch = null;
                try {
                    process.Start();
                    gitBranch = process.StandardOutput.ReadToEnd().Trim();
                    process.WaitForExit();
                    if(process.ExitCode != 0) {
                        if(showWarningOnFailure) {
                            LogWarn($"unable to get git branch 'git rev-parse --abbrev-ref HEAD' failed with exit code = {process.ExitCode}");
                        }
                        gitBranch = null;
                    }
                } catch {
                    if(showWarningOnFailure) {
                        LogWarn("git is not installed; skipping git branch detection");
                    }
                }
                return gitBranch;
            } finally {
                LogInfoPerformance($"GetGitBranch() for '{workingDirectory}'", stopwatch.Elapsed);
            }
        }

        public string? GetGitVersion() {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try {

                // constants
                const string GIT_VERSION_PREFIX = "git version ";

                // read the gitSha using 'git' directly
                var process = new Process {
                    StartInfo = new ProcessStartInfo("git", "--version") {
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
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
                    return null;
                }
            } finally {
                LogInfoPerformance($"GetGitVersion()", stopwatch.Elapsed);
            }
        }
    }
}
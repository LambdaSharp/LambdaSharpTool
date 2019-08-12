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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Amazon.APIGateway;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.IdentityManagement;
using Amazon.KeyManagementService;
using Amazon.Lambda;
using Amazon.S3;
using Amazon.SimpleSystemsManagement;

namespace LambdaSharp.Tool {

    public class LambdaSharpDeploymentTierSetupException : Exception {

        //--- Fields ---
        public readonly string TierName;

        //--- Constructors ---
        public LambdaSharpDeploymentTierSetupException(string tierName) : base() {
            TierName = tierName ?? throw new ArgumentNullException(nameof(tierName));
        }
    }

    public class LambdaSharpToolOutOfDateException : Exception {

        //--- Fields ---
        public readonly VersionInfo Version;

        //--- Constructors ---
        public LambdaSharpToolOutOfDateException(VersionInfo version) : base() {
            Version = version ?? throw new ArgumentNullException(nameof(version));
        }
    }

    public class LambdaSharpDeploymentTierOutOfDateException : Exception {

        //--- Fields ---
        public readonly string TierName;

        //--- Constructors ---
        public LambdaSharpDeploymentTierOutOfDateException(string tierName) : base() {
            TierName = tierName ?? throw new ArgumentNullException(nameof(tierName));
        }
    }

    public enum CoreServices {
        Undefined,
        Disabled,
        Bootstrap,
        Enabled
    }

    public class Settings {

        //--- Constants ---
        public const string Lash = "lash";

        //--- Class Fields ---
        public static VerboseLevel VerboseLevel = Tool.VerboseLevel.Exceptions;
        public static bool UseAnsiConsole = true;
        private static IList<(bool Error, string Message, Exception Exception)> _errors = new List<(bool Error, string Message, Exception Exception)>();
        private static string PromptColor = AnsiTerminal.Cyan;
        private static string LabelColor = AnsiTerminal.BrightCyan;

        //--- Class Properties ---
        public static int ErrorCount => _errors.Count(entry => entry.Error);
        public static bool HasErrors => _errors.Any(entry => entry.Error);
        public static int WarningCount => _errors.Count(entry => !entry.Error);
        public static bool HasWarnings => _errors.Any(entry => !entry.Error);

        //--- Class Methods ---
        public static void ShowErrors() {
            var suppressedStacktrace = false;
            foreach(var error in _errors) {
                var builder = new StringBuilder();
                if(UseAnsiConsole) {
                    builder.Append(error.Error ? AnsiTerminal.Red : AnsiTerminal.Yellow);
                }
                if(error.Error) {
                    builder.Append("ERROR: " + error.Message);
                } else {
                    builder.Append("WARNING: " + error.Message);
                }
                if((error.Exception != null) && (VerboseLevel >= VerboseLevel.Exceptions)) {
                    builder.AppendLine();
                    if(error.Exception is AggregateException aggregateException) {
                        foreach(var innerException in aggregateException.Flatten().InnerExceptions) {
                            builder.Append(innerException.ToString());
                        }
                    } else {
                        builder.Append(error.Exception.ToString());
                    }
                } else {
                    suppressedStacktrace = suppressedStacktrace || (error.Exception != null);
                }
                if(UseAnsiConsole) {
                    builder.Append(AnsiTerminal.Reset);
                }
                Console.WriteLine(builder.ToString());
            }

            // check if we omitted exception stacktraces
            if(suppressedStacktrace) {
                Console.WriteLine();
                Console.WriteLine("NOTE: one ore more errors have stacktraces; use --verbose:exceptions to show them");
            }

            // check if the errors are due to missing configuration or initialization steps
            var toolException = _errors.Select(error => error.Exception).OfType<LambdaSharpToolOutOfDateException>().FirstOrDefault();
            if(toolException != null) {
                Console.WriteLine();
                WriteAnsiLine($"IMPORTANT: run 'dotnet tool update LambdaSharp.Tool --global --version {toolException.Version}' to update the '{Lash}' command", AnsiTerminal.BrightWhite);
                return;
            }
            var setupException = _errors.Select(error => error.Exception).OfType<LambdaSharpDeploymentTierSetupException>().FirstOrDefault();
            if(setupException != null) {
                Console.WriteLine();
                WriteAnsiLine($"IMPORTANT: run '{Lash} init' to create a new LambdaSharp deployment tier '{setupException.TierName}'", AnsiTerminal.BrightWhite);
                return;
            }
            var tierException = _errors.Select(error => error.Exception).OfType<LambdaSharpDeploymentTierOutOfDateException>().FirstOrDefault();
            if(tierException != null) {
                Console.WriteLine();
                WriteAnsiLine($"IMPORTANT: run '{Lash} init' to upgrade the LambdaSharp deployment tier '{tierException.TierName}'", AnsiTerminal.BrightWhite);
            }
        }

        public static void LogWarn(string message)
            => _errors.Add((Error: false, Message: message, Exception: null));

        public static void LogError(string message, Exception exception = null)
            => _errors.Add((Error: true, Message: message, Exception: exception));

        public static void LogError(Exception exception)
            => LogError($"internal error: {exception.Message}", exception);

        public static void LogInfo(string message) {
            if(VerboseLevel > Tool.VerboseLevel.Quiet) {
                Console.WriteLine(message);
            }
        }

        public static void LogInfoVerbose(string message) {
            if(VerboseLevel >= Tool.VerboseLevel.Detailed) {
                Console.WriteLine(message);
            }
        }

        private static void WriteAnsiLine(string text, string ansiColor) {
            if(UseAnsiConsole) {
                Console.WriteLine($"{ansiColor}{text}{AnsiTerminal.Reset}");
            } else {
                Console.WriteLine(text);
            }
        }

        private static void WriteAnsi(string text, string ansiColor) {
            if(UseAnsiConsole) {
                Console.Write($"{ansiColor}{text}{AnsiTerminal.Reset}");
            } else {
                Console.Write(text);
            }
        }

        //--- Properties ---
        public VersionInfo ToolVersion { get; set; }
        public string Tier { get; set; }
        public string TierName => string.IsNullOrEmpty(Tier) ? "<DEFAULT>" : Tier;
        public string TierPrefix => string.IsNullOrEmpty(Tier) ? "" : (Tier + "-");
        public CoreServices CoreServices { get; set; }
        public VersionInfo TierVersion { get; set; }
        public string AwsRegion { get; set; }
        public string AwsAccountId { get; set; }
        public string AwsUserArn { get; set; }
        public string DeploymentBucketName { get; set; }
        public IAmazonSimpleSystemsManagement SsmClient { get; set; }
        public IAmazonCloudFormation CfnClient { get; set; }
        public IAmazonKeyManagementService KmsClient { get; set; }
        public IAmazonS3 S3Client { get; set; }
        public IAmazonAPIGateway ApiGatewayClient { get; set; }
        public IAmazonIdentityManagementService IamClient { get; set; }
        public IAmazonLambda LambdaClient { get; set; }
        public string WorkingDirectory { get; set; }
        public string OutputDirectory { get; set; }
        public bool NoDependencyValidation { get; set; }
        public bool PromptsAsErrors { get; set; }
        public DateTime UtcNow { get; set; }

        //--- Constructors ---
        public Settings() {
            var now = DateTime.UtcNow;
            now = new DateTime(now.Ticks - (now.Ticks % TimeSpan.TicksPerSecond), now.Kind);
            UtcNow = now;
        }

        //--- Methods ---
        public List<Tag> GetCloudFormationStackTags(string moduleName, string stackName)
            => new List<Tag> {
                new Tag {
                    Key = "LambdaSharp:Tier",
                    Value = string.IsNullOrEmpty(Tier) ? "-" : Tier
                },
                new Tag {
                    Key = "LambdaSharp:Module",
                    Value = moduleName
                },
                new Tag {
                    Key = "LambdaSharp:RootStack",
                    Value = stackName
                },
                new Tag {
                    Key = "LambdaSharp:DeployedBy",
                    Value = AwsUserArn.Split(':').Last()
                }
            };

        public string GetStackName(string moduleName, string instanceName = null)
            => $"{TierPrefix}{instanceName ?? moduleName.Replace(".", "-")}";

        public string PromptString(string message, string defaultValue = null)
            => PromptString(message, defaultValue, pattern: null, constraintDescription: null);

        public string PromptString(string message, string defaultValue, string pattern, string constraintDescription) {
            if(PromptsAsErrors) {
                LogError($"prompt was attempted for \"{message}\"");
                return defaultValue;
            }
            var prompt = $"|=> {message}: ";
            if(!string.IsNullOrEmpty(defaultValue)) {
                prompt += $"[{defaultValue}] ";
            }
        again:
            WriteAnsi(prompt, PromptColor);
            SetCursorVisible(true);
            var result = Console.ReadLine();
            SetCursorVisible(false);
            if((pattern != null) && !Regex.IsMatch(result, pattern)) {
                WriteAnsiLine(constraintDescription ?? $"Value must match regular expression pattern: {pattern}", PromptColor);
                goto again;
            }
            return string.IsNullOrEmpty(result)
                ? defaultValue
                : result;

            // local functions
            void SetCursorVisible(bool visible) {
                try {
                    Console.CursorVisible = visible;
                } catch { }
            }
        }

        public void PromptLabel(string message) => WriteAnsiLine($"*** {message} ***", LabelColor);

        public string PromptChoice(string message, IList<string> choices) {
            if(PromptsAsErrors) {
                LogError($"prompt was attempted for \"{message}\"");
                return choices.FirstOrDefault();
            }
            WriteAnsiLine($"{message}:", PromptColor);
            var choiceCount = choices.Count;
            for(var i = 0; i < choiceCount; ++i) {
                WriteAnsiLine($"{i + 1}. {choices[i]}", PromptColor);
            }
            while(true) {
                var enteredValue = PromptString($"Enter a choice", pattern: null, constraintDescription: null, defaultValue: null);
                if(int.TryParse(enteredValue, out var choice) && (choice >= 1) && (choice <= choiceCount)) {
                    return choices[choice - 1];
                }
            }
        }
    }
}
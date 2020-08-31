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


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LambdaSharp.Tool {

    public class LambdaSharpException : Exception { }

    public class LambdaSharpDeploymentTierSetupException : LambdaSharpException {

        //--- Fields ---
        public readonly string TierName;

        //--- Constructors ---
        public LambdaSharpDeploymentTierSetupException(string tierName) : base() {
            TierName = tierName ?? throw new ArgumentNullException(nameof(tierName));
        }
    }

    public class LambdaSharpToolOutOfDateException : LambdaSharpException {

        //--- Fields ---
        public readonly VersionInfo Version;

        //--- Constructors ---
        public LambdaSharpToolOutOfDateException(VersionInfo version) : base() {
            Version = version ?? throw new ArgumentNullException(nameof(version));
        }
    }

    public class LambdaSharpDeploymentTierOutOfDateException : LambdaSharpException {

        //--- Fields ---
        public readonly string TierName;

        //--- Constructors ---
        public LambdaSharpDeploymentTierOutOfDateException(string tierName) : base() {
            TierName = tierName ?? throw new ArgumentNullException(nameof(tierName));
        }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum CoreServices {
        Undefined,
        Disabled,
        Bootstrap,
        Enabled
    }

    public class BuildPolicy {

        //--- Properties ---
        public BuildModulesPolicy Modules { get; set; }
    }

    public class BuildModulesPolicy {

        //--- Properties ---
        public List<string> Allow { get; set; }
    }

    public class Settings {

        //--- Constants ---
        public const string Lash = "lash";
        private static string SYSTEM_OS_INFORMATION = "/etc/system-release";

        //--- Class Fields ---
        public static VerboseLevel VerboseLevel = Tool.VerboseLevel.Exceptions;
        public static AnsiTerminal AnsiTerminal;
        public static bool AllowCaching = false;
        public static TimeSpan MaxCacheAge = TimeSpan.FromDays(1);
        private static IList<(bool Error, string Message, Exception Exception)> _errors = new List<(bool Error, string Message, Exception Exception)>();
        private static string PromptColor => UseAnsiConsole ? AnsiTerminal.Cyan : "";
        private static string LabelColor => UseAnsiConsole ? AnsiTerminal.BrightCyan : "";
        public static string ResetColor => UseAnsiConsole ? AnsiTerminal.Reset : "";
        public static string OutputColor => UseAnsiConsole ? AnsiTerminal.Green : "";
        public static string InfoColor => UseAnsiConsole ? AnsiTerminal.Yellow : "";
        public static string AlertColor => UseAnsiConsole ? (AnsiTerminal.Black + AnsiTerminal.BackgroundRed) : "";
        public static string WarningColor => UseAnsiConsole ? AnsiTerminal.BrightYellow : "";
        public static string ErrorColor => UseAnsiConsole ? AnsiTerminal.BrightRed : "";
        public static string HighContrastColor => UseAnsiConsole ? AnsiTerminal.BrightWhite : "";
        public static string LowContrastColor => UseAnsiConsole ? AnsiTerminal.BrightBlack : "";
        public static string DebugColor => UseAnsiConsole ? AnsiTerminal.BrightBlue : "";

        private static Lazy<bool> _isAmazonLinux2 = new Lazy<bool>(() => {

            // check if running on Linux OS
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {

                // check if OS information file contains Amazon Linux string
                try {
                    if(File.Exists(SYSTEM_OS_INFORMATION)) {
                        var osRelease = File.ReadAllText(SYSTEM_OS_INFORMATION);
                        return osRelease.StartsWith("Amazon Linux release 2", StringComparison.Ordinal);
                    }
                } catch { }
            }
            return false;
        });

        //--- Class Properties ---
        public static bool UseAnsiConsole  {
            get => AnsiTerminal.Enabled;
            set => AnsiTerminal.Enabled = value;
        }

        public static int ErrorCount => _errors.Count(entry => entry.Error);
        public static bool HasErrors => _errors.Any(entry => entry.Error);
        public static int WarningCount => _errors.Count(entry => !entry.Error);
        public static bool HasWarnings => _errors.Any(entry => !entry.Error);
        public static string ToolCacheDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LambdaSharp");
        public static string AwsProfileCacheDirectory => Path.Combine(ToolCacheDirectory, AwsProfileEnvironmentVariable);
        public static string AwsProfileEnvironmentVariable = Environment.GetEnvironmentVariable("AWS_PROFILE")
                ?? Environment.GetEnvironmentVariable("AWS_DEFAULT_PROFILE")
                ?? "default";
        public static string CloudFormationResourceSpecificationCacheFilePath = Path.Combine(ToolCacheDirectory, "CloudFormationResourceSpecification.json");

        //--- Class Methods ---
        public static void ShowErrors() {
            var suppressedStacktrace = false;
            foreach(var error in _errors) {
                var builder = new StringBuilder();
                if(UseAnsiConsole) {
                    builder.Append(error.Error ? AnsiTerminal.Red : AnsiTerminal.BrightYellow);
                }
                if(error.Error) {
                    builder.Append("ERROR: " + error.Message);
                } else {
                    builder.Append("WARNING: " + error.Message);
                }
                var hasException = (error.Exception != null) && !(error.Exception is LambdaSharpException);
                if(hasException && (VerboseLevel >= VerboseLevel.Exceptions)) {
                    builder.AppendLine();
                    if(error.Exception is AggregateException aggregateException) {
                        foreach(var innerException in aggregateException.Flatten().InnerExceptions) {
                            builder.Append(innerException.ToString());
                        }
                    } else {
                        builder.Append(error.Exception.ToString());
                    }
                } else {
                    suppressedStacktrace = suppressedStacktrace || hasException;
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
                Console.WriteLine($"{HighContrastColor}IMPORTANT: run 'dotnet tool update LambdaSharp.Tool --global --version {toolException.Version}' to update the '{Lash}' command{ResetColor}");
                return;
            }
            var setupException = _errors.Select(error => error.Exception).OfType<LambdaSharpDeploymentTierSetupException>().FirstOrDefault();
            if(setupException != null) {
                Console.WriteLine();
                Console.WriteLine($"{HighContrastColor}IMPORTANT: run '{Lash} init' to create a new LambdaSharp deployment tier '{setupException.TierName}'{ResetColor}");
                return;
            }
            var tierException = _errors.Select(error => error.Exception).OfType<LambdaSharpDeploymentTierOutOfDateException>().FirstOrDefault();
            if(tierException != null) {
                Console.WriteLine();
                Console.WriteLine($"{HighContrastColor}IMPORTANT: run '{Lash} init' to upgrade the LambdaSharp deployment tier '{tierException.TierName}'{ResetColor}");
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

        public static void LogInfoPerformance(string message, TimeSpan duration, bool? cached = null) {
            if(VerboseLevel >= Tool.VerboseLevel.Performance) {
                Console.WriteLine($"{DebugColor}TIMING: {message} [duration={duration.TotalSeconds:N2}s{(cached.HasValue ? $", cached={cached.Value.ToString().ToLowerInvariant()}" : "")}]{ResetColor}");
            }
        }

        public static bool IsAmazonLinux2() => _isAmazonLinux2.Value;

        //--- Constructors ---
        public Settings() {
            var now = DateTime.UtcNow;
            now = new DateTime(now.Ticks - (now.Ticks % TimeSpan.TicksPerSecond), now.Kind);
            UtcNow = now;
        }

        //--- Properties ---
        public VersionInfo ToolVersion { get; set; }

        /// <summary>
        /// This property determines the reference version for compatibility between the tool, the tier, and the core services.
        /// The reference version is `Major.Minor`, where `Major` can be a fractional version.
        /// </summary>
        public VersionInfo CoreServicesReferenceVersion => ToolVersion.GetCoreServicesReferenceVersion();

        public string Tier { get; set; }
        public string TierName => string.IsNullOrEmpty(Tier) ? "<DEFAULT>" : Tier;
        public string TierPrefix => string.IsNullOrEmpty(Tier) ? "" : (Tier + "-");
        public CoreServices CoreServices { get; set; }
        public VersionInfo TierVersion { get; set; }
        public string AwsRegion { get; set; }
        public string AwsAccountId { get; set; }
        public string AwsUserArn { get; set; }
        public string DeploymentBucketName { get; set; }
        public string LoggingBucketName { get; set; }
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
        public BuildPolicy BuildPolicy { get; set; }

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
            Console.Write($"{PromptColor}{prompt}{ResetColor}");
            SetCursorVisible(true);
            var result = Console.ReadLine();
            SetCursorVisible(false);
            if((pattern != null) && !Regex.IsMatch(result, pattern)) {
                Console.WriteLine($"{PromptColor}{constraintDescription ?? $"Value must match regular expression pattern: {pattern}"}{ResetColor}");
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

        public void PromptLabel(string message) => Console.WriteLine($"{LabelColor}*** {message} ***{ResetColor}");

        public string PromptChoice(string message, IList<string> choices) {
            if(PromptsAsErrors) {
                LogError($"prompt was attempted for \"{message}\"");
                return choices.FirstOrDefault();
            }
            Console.WriteLine($"{PromptColor}{message}:{ResetColor}");
            var choiceCount = choices.Count;
            for(var i = 0; i < choiceCount; ++i) {
                Console.WriteLine($"{PromptColor}{i + 1}. {choices[i]}{ResetColor}");
            }
            while(true) {
                var enteredValue = PromptString($"Enter a choice", pattern: null, constraintDescription: null, defaultValue: null);
                if(int.TryParse(enteredValue, out var choice) && (choice >= 1) && (choice <= choiceCount)) {
                    return choices[choice - 1];
                }
            }
        }

        public bool PromptYesNo(string message, bool defaultAnswer) {
            if(PromptsAsErrors) {
                LogError($"prompt was attempted for \"{message}\"");
                return defaultAnswer;
            }
            return Prompt.GetYesNo($"{PromptColor}|=> {message}{ResetColor}", defaultAnswer);
        }

        public string GetOriginCacheDirectory(ModuleInfo moduleInfo) => Path.Combine(ToolCacheDirectory, ".origin", moduleInfo.Origin ?? DeploymentBucketName, moduleInfo.Namespace, moduleInfo.Name);
    }
}
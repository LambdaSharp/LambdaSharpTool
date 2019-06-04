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
using System.Linq;
using System.Text;
using Amazon.APIGateway;
using Amazon.CloudFormation;
using Amazon.IdentityManagement;
using Amazon.KeyManagementService;
using Amazon.S3;
using Amazon.SimpleSystemsManagement;
using LambdaSharp.Tool.Model;

namespace LambdaSharp.Tool {

    public class LambdaSharpDeploymentTierSetupException : Exception {

        //--- Fields ---
        public readonly string Tier;

        //--- Constructors ---
        public LambdaSharpDeploymentTierSetupException(string tier) : base() {
            Tier = tier ?? throw new ArgumentNullException(nameof(tier));
        }
    }

    public class LambdaSharpToolConfigException : Exception {

        //--- Fields ---
        public readonly string Profile;

        //--- Constructors ---
        public LambdaSharpToolConfigException(string profile) : base() {
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        }
    }

    public class Settings {

        //--- Constants ---
        public const string Lash = "lash";

        //--- Class Fields ---
        public static VerboseLevel VerboseLevel = Tool.VerboseLevel.Exceptions;
        public static bool UseAnsiConsole = true;
        private static IList<(bool Error, string Message, Exception Exception)> _errors = new List<(bool Error, string Message, Exception Exception)>();

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
                    builder.Append(error.Exception.ToString());
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
            var configException = _errors.Select(error => error.Exception).OfType<LambdaSharpToolConfigException>().FirstOrDefault();
            if(configException != null) {
                Console.WriteLine();
                Console.WriteLine($"IMPORTANT: run '{Lash} config' to configure LambdaSharp CLI for profile '{configException.Profile}'");
                return;
            }
            var setupException = _errors.Select(error => error.Exception).OfType<LambdaSharpDeploymentTierSetupException>().FirstOrDefault();
            if(setupException != null) {
                Console.WriteLine();
                Console.WriteLine($"IMPORTANT: run '{Lash} init' to create a new LambdaSharp deployment tier '{setupException.Tier}'");
                return;
            }
        }

        public static void LogWarn(string message)
            => _errors.Add((Error: false, Message: message, Exception: null));

        public static void LogError(string message, Exception exception = null)
            => _errors.Add((Error: true, Message: message, Exception: exception));

        public static void LogError(Exception exception)
            => LogError($"internal error: {exception.Message}", exception);

        //--- Properties ---
        public VersionInfo ToolVersion { get; set; }
        public string ToolProfile { get; set; }
        public bool ToolProfileExplicitlyProvided { get; set; }
        public string Tier { get; set; }
        public VersionInfo TierVersion { get; set; }
        public string TierDefaultSecretKey { get; set; }
        public string AwsRegion { get; set; }
        public string AwsAccountId { get; set; }
        public string AwsUserArn { get; set; }
        public string DeploymentBucketName { get; set; }
        public string DeploymentNotificationsTopic { get; set; }
        public string ApiGatewayAccountRole { get; set; }
        public IEnumerable<string> ModuleBucketNames { get; set; }
        public IAmazonSimpleSystemsManagement SsmClient { get; set; }
        public IAmazonCloudFormation CfnClient { get; set; }
        public IAmazonKeyManagementService KmsClient { get; set; }
        public IAmazonS3 S3Client { get; set; }
        public IAmazonAPIGateway ApiGatewayClient { get; set; }
        public IAmazonIdentityManagementService IamClient { get; set; }
        public string WorkingDirectory { get; set; }
        public string OutputDirectory { get; set; }
        public bool NoDependencyValidation { get; set; }
    }
}
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
using Amazon.CloudFormation;
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
        private static IList<(string Message, Exception Exception)> _errors = new List<(string Message, Exception Exception)>();

        //--- Class Properties ---
        public static int ErrorCount => _errors.Count;
        public static bool HasErrors => _errors.Count > 0;

        //--- Class Methods ---
        public static void ShowErrors() {
            foreach(var error in _errors) {
                if((error.Exception != null) && (VerboseLevel >= VerboseLevel.Exceptions)) {
                    Console.WriteLine("ERROR: " + error.Message + Environment.NewLine + error.Exception);
                } else {
                    Console.WriteLine("ERROR: " + error.Message);
                }
            }
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

        public static void AddWarning(string message)
            => Console.WriteLine("WARNING: " + message);

        public static void AddError(string message, Exception exception = null)
            => _errors.Add((Message: message, Exception: exception));

        public static void AddError(Exception exception)
            => AddError($"internal error: {exception.Message}", exception);

        //--- Properties ---
        public VersionInfo ToolVersion { get; set; }
        public string ToolProfile { get; set; }
        public bool ToolProfileExplicitlyProvided { get; set; }
        public VersionInfo CoreVersion { get; set; }
        public string Tier { get; set; }
        public string AwsRegion { get; set; }
        public string AwsAccountId { get; set; }
        public string DeploymentBucketName { get; set; }
        public string DeploymentNotificationsTopic { get; set; }
        public IEnumerable<string> ModuleBucketNames { get; set; }
        public IAmazonSimpleSystemsManagement SsmClient { get; set; }
        public IAmazonCloudFormation CfnClient { get; set; }
        public IAmazonKeyManagementService KmsClient { get; set; }
        public IAmazonS3 S3Client { get; set; }
        public string WorkingDirectory { get; set; }
        public string OutputDirectory { get; set; }
        public bool NoDependencyValidation { get; set; }
    }
}
/*
 * MindTouch λ#
 * Copyright (C) 2018 MindTouch, Inc.
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
using Amazon.CloudFormation;
using Amazon.KeyManagementService;
using Amazon.S3;
using Amazon.SimpleSystemsManagement;
using MindTouch.LambdaSharp.Tool.Model;

namespace MindTouch.LambdaSharp.Tool {

    public class LambdaSharpDeploymentTierSetupException : Exception {

        //--- Fields ---
        public readonly string Tier;

        //--- Constructors ---
        public LambdaSharpDeploymentTierSetupException(string tier) : base() {
            Tier = tier ?? throw new ArgumentNullException(nameof(tier));
        }
    }

    public class Settings {

        //--- Properties ---
        public Version ToolVersion { get; set; }
        public Version EnvironmentVersion { get; set; }
        public string Tier { get; set; }
        public string GitSha { get; set; }
        public string BuildConfiguration { get; set; }
        public string AwsRegion { get; set; }
        public string AwsAccountId { get; set; }
        public string DeploymentBucketName { get; set; }
        public string DeadLetterQueueUrl { get; set; }
        public string LoggingTopicArn { get; set; }
        public string NotificationTopicArn { get; set; }
        public string RollbarCustomResourceTopicArn { get; set; }
        public string S3PackageLoaderCustomResourceTopicArn { get; set; }
        public string S3SubscriberCustomResourceTopicArn { get; set; }
        public ResourceMapping ResourceMapping { get; set; }
        public IAmazonSimpleSystemsManagement SsmClient { get; set; }
        public IAmazonCloudFormation CfClient { get; set; }
        public IAmazonKeyManagementService KmsClient { get; set; }
        public IAmazonS3 S3Client { get; set; }
        public Action<string, Exception> ErrorCallback { get; set; }
        public bool HasErrors { get; set; }
        public VerboseLevel VerboseLevel { get; set; }
        public string ModuleSource { get; set; }
        public string WorkingDirectory { get; set; }
        public string OutputDirectory { get; set; }

        public string DeadLetterQueueArn {
            get {
                if(DeadLetterQueueUrl == null) {
                    return null;
                }
                var queueName = DeadLetterQueueUrl.Substring(DeadLetterQueueUrl.LastIndexOf('/') + 1);
                return $"arn:aws:sqs:{AwsRegion}:{AwsAccountId}:{queueName}";
             }
        }

        //--- Methods ---
        public void AddError(string message, Exception exception = null) {
            HasErrors = true;
            ErrorCallback(message, exception);
        }

        public void Reset() {
            EnvironmentVersion = null;
            DeploymentBucketName = null;
            DeadLetterQueueUrl = null;
            LoggingTopicArn = null;
            NotificationTopicArn = null;
            RollbarCustomResourceTopicArn = null;
            S3PackageLoaderCustomResourceTopicArn = null;
            S3SubscriberCustomResourceTopicArn = null;
        }
    }
}
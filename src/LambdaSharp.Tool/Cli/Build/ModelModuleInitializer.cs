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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using LambdaSharp.Tool.Internal;
using LambdaSharp.Tool.Model;
using LambdaSharp.Tool.Model.AST;
using Newtonsoft.Json;

namespace LambdaSharp.Tool.Cli.Build {
    using static ModelFunctions;

    public class ModelModuleInitializer : AModelProcessor {

        //--- Class Fields ---
        private static readonly string DecryptSecretFunctionCode;

        //--- Class Constructor ---
        static ModelModuleInitializer() {
            var assembly = typeof(ModelModuleInitializer).Assembly;
            using(var resource = assembly.GetManifestResourceStream($"LambdaSharp.Tool.Resources.DecryptSecretFunction.js"))
            using(var reader = new StreamReader(resource, Encoding.UTF8)) {
                DecryptSecretFunctionCode = reader.ReadToEnd().Replace("\r", "");
            }
        }

        //--- Fields ---
        private ModuleBuilder _builder;

        //--- Constructors ---
        public ModelModuleInitializer(Settings settings, string sourceFilename) : base(settings, sourceFilename) { }

        //--- Methods ---
        public void Initialize(ModuleBuilder builder) {
            _builder = builder;

            // add module variables
            var moduleItem = _builder.AddVariable(
                parent: null,
                name: "Module",
                description: "Module Variables",
                type: "String",
                scope: null,
                value: "",
                allow: null,
                encryptionContext: null
            );
            _builder.AddVariable(
                parent: moduleItem,
                name: "Id",
                description: "Module ID",
                type: "String",
                scope: null,
                value: FnRef("AWS::StackName"),
                allow: null,
                encryptionContext: null
            );
            _builder.AddVariable(
                parent: moduleItem,
                name: "Owner",
                description: "Module Owner",
                type: "String",
                scope: null,
                value: _builder.Owner,
                allow: null,
                encryptionContext: null
            );
            _builder.AddVariable(
                parent: moduleItem,
                name: "Name",
                description: "Module Name",
                type: "String",
                scope: null,
                value: _builder.Name,
                allow: null,
                encryptionContext: null
            );
            _builder.AddVariable(
                parent: moduleItem,
                name: "FullName",
                description: "Module FullName",
                type: "String",
                scope: null,
                value: _builder.FullName,
                allow: null,
                encryptionContext: null
            );
            _builder.AddVariable(
                parent: moduleItem,
                name: "Version",
                description: "Module Version",
                type: "String",
                scope: null,
                value: _builder.Version.ToString(),
                allow: null,
                encryptionContext: null
            );
            _builder.AddCondition(
                parent: moduleItem,
                name: "IsNested",
                description: "Module is nested",
                value: FnNot(FnEquals(FnRef("DeploymentRoot"), ""))
            );
            _builder.AddVariable(
                parent: moduleItem,
                name: "RootId",
                description: "Root Module ID",
                type: "String",
                scope: null,
                value: FnIf("Module::IsNested", FnRef("DeploymentRoot"), FnRef("Module::Id")),
                allow: null,
                encryptionContext: null
            );

            // add LambdaSharp Module Options
            var section = "LambdaSharp Module Options";
            _builder.AddParameter(
                name: "Secrets",
                section: section,
                label: "Comma-separated list of additional KMS secret keys",
                description: "Secret Keys (ARNs)",
                type: "String",
                scope: null,
                noEcho: null,
                defaultValue: "",
                constraintDescription: null,
                allowedPattern: null,
                allowedValues: null,
                maxLength: null,
                maxValue: null,
                minLength: null,
                minValue: null,
                allow: null,
                properties: null,
                arnAttribute: null,
                encryptionContext: null,
                pragmas: null
            );
            var secretsIsEmpty = _builder.AddCondition(
                parent: null,
                name: "SecretsIsEmpty",
                description: null,
                value: FnEquals(FnRef("Secrets"), "")
            );
            _builder.AddParameter(
                name: "XRayTracing",
                section: section,
                label: "AWS X-Ray tracing mode for module functions",
                description: "AWS X-Ray Tracing Mode",
                type: "String",
                scope: null,
                noEcho: null,
                defaultValue: "PassThrough",
                constraintDescription: null,
                allowedPattern: null,
                allowedValues: new[] { "Active", "PassThrough" },
                maxLength: null,
                maxValue: null,
                minLength: null,
                minValue: null,
                allow: null,
                properties: null,
                arnAttribute: null,
                encryptionContext: null,
                pragmas: null
            );

            // import lambdasharp dependencies (unless requested otherwise)
            if(_builder.HasLambdaSharpDependencies) {

                // add LambdaSharp Module Internal resource imports
                var lambdasharp = _builder.AddVariable(
                    parent: null,
                    name: "LambdaSharp",
                    description: "LambdaSharp Core Imports",
                    type: "String",
                    scope: null,
                    value: "",
                    allow: null,
                    encryptionContext: null
                );
                _builder.AddImport(
                    parent: lambdasharp,
                    name: "DeadLetterQueue",
                    description: null,

                    // TODO (2018-12-01, bjorg): consider using 'AWS::SQS::Queue'
                    type: "String",
                    scope: null,
                    allow: null /* new[] {
                        "sqs:SendMessage"
                    }*/,
                    module: "LambdaSharp.Core",
                    encryptionContext: null
                );
                _builder.AddImport(
                    parent: lambdasharp,
                    name: "LoggingStream",
                    description: null,

                    // NOTE (2018-12-11, bjorg): we use type 'String' to be more flexible with the type of values we're willing to take
                    type: "String",
                    scope: null,
                    allow: null,
                    module: "LambdaSharp.Core",
                    encryptionContext: null
                );
                _builder.AddImport(
                    parent: lambdasharp,
                    name: "LoggingStreamRole",
                    description: null,

                    // NOTE (2018-12-11, bjorg): we use type 'String' to be more flexible with the type of values we're willing to take
                    type: "String",
                    scope: null,
                    allow: null,
                    module: "LambdaSharp.Core",
                    encryptionContext: null
                );
                _builder.AddImport(
                    parent: lambdasharp,
                    name: "DefaultSecretKey",
                    description: null,

                    // TODO (2018-12-01, bjorg): consider using 'AWS::KMS::Key'
                    type: "String",
                    scope: null,

                    // NOTE (2018-12-11, bjorg): we grant decryption access later as part of a bulk permissioning operation
                    allow: null,
                    module: "LambdaSharp.Core",
                    encryptionContext: null
                );
            }

            // add module variables
            if(TryGetModuleVariable("DeadLetterQueue", out var deadLetterQueueVariable)) {
                _builder.AddVariable(
                    parent: moduleItem,
                    name: "DeadLetterQueue",
                    description: "Module Dead Letter Queue (ARN)",
                    type: "String",
                    scope: null,
                    value: deadLetterQueueVariable,
                    allow: null,
                    encryptionContext: null
                );
                _builder.AddGrant(
                    sid: "ModuleDeadLetterQueueLogging",
                    awsType: null,
                    reference: FnRef("Module::DeadLetterQueue"),
                    allow: new[] {
                        "sqs:SendMessage"
                    }
                );
            }
            if(TryGetModuleVariable("LoggingStream", out var loggingStreamVariable)) {
                _builder.AddVariable(
                    parent: moduleItem,
                    name: "LoggingStream",
                    description: "Module Logging Stream (ARN)",
                    type: "String",
                    scope: null,
                    value: loggingStreamVariable,
                    allow: null,
                    encryptionContext: null
                );
            }
            if(TryGetModuleVariable("LoggingStreamRole", out var loggingStreamRoleVariable)) {
                _builder.AddVariable(
                    parent: moduleItem,
                    name: "LoggingStreamRole",
                    description: "Module Logging Stream Role (ARN)",
                    type: "String",
                    scope: null,
                    value: loggingStreamRoleVariable,
                    allow: null,
                    encryptionContext: null
                );
            }
            if(TryGetModuleVariable("DefaultSecretKey", out var defaultSecretKeyVariable)) {
                _builder.AddVariable(
                    parent: moduleItem,
                    name: "DefaultSecretKey",
                    description: "Module Default Secret Key (ARN)",
                    type: "String",
                    scope: null,
                    value: defaultSecretKeyVariable,
                    allow: null,
                    encryptionContext: null
                );
                _builder.AddSecret(FnRef("Module::DefaultSecretKey"));
            }

            // add decryption permission for secrets
            var secretsReference = _builder.Secrets.Any()
                ? FnSplit(
                    ",",
                    FnIf(
                        secretsIsEmpty.FullName,
                        FnJoin(",", _builder.Secrets),
                        FnJoin(
                            ",",
                            _builder.Secrets.Append(FnRef("Secrets")).ToList()
                        )
                    )
                )
                : FnIf(
                    secretsIsEmpty.FullName,

                    // TODO (2018-11-26, bjorg): this hack does not work to bypass the error of an empty list :(
                    "arn:aws:kms:${AWS::Region}:${AWS::AccountId}:key/12345678-1234-1234-1234-123456789012",
                    FnSplit(",", FnRef("Secrets"))
                );
            _builder.AddGrant(
                sid: "SecretsDecryption",
                awsType: null,
                reference: secretsReference,
                allow: new[] {
                    "kms:Decrypt",
                    "kms:Encrypt",
                    "kms:GenerateDataKey",
                    "kms:GenerateDataKeyWithoutPlaintext"
                }
            );

            // add decryption function for secret parameters and values
            _builder.AddInlineFunction(
                parent: moduleItem,
                name: "DecryptSecretFunction",
                description: "Module secret decryption function",
                environment: null,
                sources: null,
                condition: null,
                pragmas: new[] {
                    "no-function-registration",
                    "no-dead-letter-queue",
                    "no-wildcard-scoped-variables"
                },
                timeout: "30",
                memory: "128",
                code: DecryptSecretFunctionCode
            ).DiscardIfNotReachable = true;

            // add LambdaSharp Deployment Settings
            section = "LambdaSharp Deployment Settings (DO NOT MODIFY)";
            _builder.AddParameter(
                name: "DeploymentBucketName",
                section: section,
                label: "Deployment S3 bucket name",
                description: "Deployment S3 Bucket Name",
                type: "String",
                scope: null,
                noEcho: null,
                defaultValue: null,
                constraintDescription: null,
                allowedPattern: null,
                allowedValues: null,
                maxLength: null,
                maxValue: null,
                minLength: null,
                minValue: null,
                allow: null,
                properties: null,
                arnAttribute: null,
                encryptionContext: null,
                pragmas: null
            );
            _builder.AddParameter(
                name: "DeploymentPrefix",
                section: section,
                label: "Deployment tier prefix",
                description: "Deployment Tier Prefix",
                type: "String",
                scope: null,
                noEcho: null,
                defaultValue: null,
                constraintDescription: null,
                allowedPattern: null,
                allowedValues: null,
                maxLength: null,
                maxValue: null,
                minLength: null,
                minValue: null,
                allow: null,
                properties: null,
                arnAttribute: null,
                encryptionContext: null,
                pragmas: null
            );
            _builder.AddParameter(
                name: "DeploymentPrefixLowercase",
                section: section,
                label: "Deployment tier prefix (lowercase)",
                description: "Deployment Tier Prefix (lowercase)",
                type: "String",
                scope: null,
                noEcho: null,
                defaultValue: null,
                constraintDescription: null,
                allowedPattern: null,
                allowedValues: null,
                maxLength: null,
                maxValue: null,
                minLength: null,
                minValue: null,
                allow: null,
                properties: null,
                arnAttribute: null,
                encryptionContext: null,
                pragmas: null
            );
            _builder.AddParameter(
                name: "DeploymentRoot",
                section: section,
                label: "Root stack name for nested deployments, blank otherwise",
                description: "Root Stack Name",
                type: "String",
                scope: null,
                noEcho: null,
                defaultValue: "",
                constraintDescription: null,
                allowedPattern: null,
                allowedValues: null,
                maxLength: null,
                maxValue: null,
                minLength: null,
                minValue: null,
                allow: null,
                properties: null,
                arnAttribute: null,
                encryptionContext: null,
                pragmas: null
            );
            _builder.AddParameter(
                name: "DeploymentChecksum",
                section: section,
                label: "CloudFormation template MD5 checksum",
                description: "Deployment Checksum",
                type: "String",
                scope: null,
                noEcho: null,
                defaultValue: "",
                constraintDescription: null,
                allowedPattern: null,
                allowedValues: null,
                maxLength: null,
                maxValue: null,
                minLength: null,
                minValue: null,
                allow: null,
                properties: null,
                arnAttribute: null,
                encryptionContext: null,
                pragmas: null
            );

            // create module IAM role used by all functions
            _builder.AddResource(
                parent: moduleItem,
                name: "Role",
                description: null,
                scope: null,
                resource: new Humidifier.IAM.Role {
                    AssumeRolePolicyDocument = new Humidifier.PolicyDocument {
                        Version = "2012-10-17",
                        Statement = new[] {
                            new Humidifier.Statement {
                                Sid = "ModuleLambdaPrincipal",
                                Effect = "Allow",
                                Principal = new Humidifier.Principal {
                                    Service = "lambda.amazonaws.com"
                                },
                                Action = "sts:AssumeRole"
                            }
                        }.ToList()
                    },
                    Policies = new[] {
                        new Humidifier.IAM.Policy {
                            PolicyName = FnSub("${AWS::StackName}ModulePolicy"),
                            PolicyDocument = new Humidifier.PolicyDocument {
                                Version = "2012-10-17",
                                Statement = new List<Humidifier.Statement>()
                            }
                        }
                    }.ToList()
                },
                resourceExportAttribute: null,
                dependsOn: null,
                condition: null,
                pragmas: null
            ).DiscardIfNotReachable = true;

            // permissions needed for writing to log streams (but not for creating log groups!)
            _builder.AddGrant(
                sid: "ModuleLogStreamAccess",
                awsType: null,
                reference: "arn:aws:logs:*:*:*",
                allow: new[] {
                    "logs:CreateLogStream",
                    "logs:PutLogEvents"
                }
            );

            // permissions needed for X-Ray daemon to upload tracing information
            _builder.AddGrant(
                sid: "AWSXRayWriteAccess",
                awsType: null,
                reference: "*",
                allow: new[] {
                    "xray:PutTraceSegments",
                    "xray:PutTelemetryRecords",
                    "xray:GetSamplingRules",
                    "xray:GetSamplingTargets",
                    "xray:GetSamplingStatisticSummaries"
                }
            );

            // check if lambdasharp specific resources need to be initialized
            var functions = _builder.Items.OfType<FunctionItem>().ToList();
            if(_builder.TryGetItem("Module::DeadLetterQueue", out _)) {
                foreach(var function in functions.Where(f => f.HasDeadLetterQueue)) {

                    // initialize dead-letter queue
                    function.Function.DeadLetterConfig = new Humidifier.Lambda.FunctionTypes.DeadLetterConfig {
                        TargetArn = FnRef("Module::DeadLetterQueue")
                    };
                }
            }

            // permissions needed for lambda functions to exist in a VPC
            if(functions.Any(function => function.Function.VpcConfig != null)) {
                _builder.AddGrant(
                    sid: "ModuleVpcNetworkInterfaces",
                    awsType: null,
                    reference: "*",
                    allow: new[] {
                        "ec2:DescribeNetworkInterfaces",
                        "ec2:CreateNetworkInterface",
                        "ec2:DeleteNetworkInterface"
                    }
                );
            }

            // add module registration
            if(_builder.HasModuleRegistration) {
                _builder.AddDependency("LambdaSharp.Core", Settings.ToolVersion.GetCompatibleBaseVersion(), maxVersion: null, bucketName: null);

                // create module registration
                _builder.AddResource(
                    parent: moduleItem,
                    name: "Registration",
                    description: null,
                    type: "LambdaSharp::Registration::Module",
                    scope: null,
                    allow: null,
                    properties: new Dictionary<string, object> {
                        ["Module"] = _builder.Info,
                        ["ModuleId"] = FnRef("AWS::StackName")
                    },
                    dependsOn: null,
                    arnAttribute: null,
                    condition: null,
                    pragmas: null
                );

                // handle function registrations
                var registeredFunctions = _builder.Items
                    .OfType<FunctionItem>()
                    .Where(function => function.HasFunctionRegistration)
                    .ToList();
                if(registeredFunctions.Any()) {

                    // create registration-related resources for functions
                    foreach(var function in registeredFunctions) {

                        // create function registration
                        _builder.AddResource(
                            parent: function,
                            name: "Registration",
                            description: null,
                            type: "LambdaSharp::Registration::Function",
                            scope: null,
                            allow: null,
                            properties: new Dictionary<string, object> {
                                ["ModuleId"] = FnRef("AWS::StackName"),
                                ["FunctionId"] = FnRef(function.FullName),
                                ["FunctionName"] = function.Name,
                                ["FunctionLogGroupName"] = FnSub($"/aws/lambda/${{{function.FullName}}}"),
                                ["FunctionPlatform"] = "AWS Lambda",
                                ["FunctionFramework"] = function.Function.Runtime,
                                ["FunctionLanguage"] = function.Language,
                                ["FunctionMaxMemory"] = function.Function.MemorySize,
                                ["FunctionMaxDuration"] = function.Function.Timeout
                            },
                            dependsOn: new[] { "Module::Registration" },
                            arnAttribute: null,
                            condition: function.Condition,
                            pragmas: null
                        );

                        // create function log-group subscription
                        if(
                            _builder.TryGetItem("Module::LoggingStream", out _)
                            && _builder.TryGetItem("Module::LoggingStreamRole", out _)
                        ) {
                            _builder.AddResource(
                                parent: function,
                                name: "LogGroupSubscription",
                                description: null,
                                scope: null,
                                resource: new Humidifier.Logs.SubscriptionFilter {
                                    DestinationArn = FnRef("Module::LoggingStream"),
                                    FilterPattern = "-\"*** \"",
                                    LogGroupName = FnRef($"{function.FullName}::LogGroup"),
                                    RoleArn = FnRef("Module::LoggingStreamRole")
                                },
                                resourceExportAttribute: null,
                                dependsOn: null,
                                condition: function.Condition,
                                pragmas: null
                            );
                        }
                    }
                }
            }
        }

        private bool TryGetModuleVariable(string name, out object variable) {
            if(
                _builder.TryGetLabeledPragma("Overrides", out var value)
                && (value is IDictionary dictionary)
            ) {
                var entry = dictionary[$"Module::{name}"];
                if(entry != null) {
                    variable = entry;
                    return true;
                }
            }
            if(_builder.HasLambdaSharpDependencies) {
                variable = FnRef($"LambdaSharp::{name}");
                return true;
            }
            variable = null;
            return false;
        }
    }
}
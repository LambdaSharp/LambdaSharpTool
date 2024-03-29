﻿/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2022
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

using System.Collections.Generic;
using System.Linq;
using LambdaSharp.Tool.Internal;
using LambdaSharp.Tool.Model;

namespace LambdaSharp.Tool.Cli.Build {
    using static ModelFunctions;

    public class ModelModuleInitializer : AModelProcessor {

        //--- Class Fields ---
        private static readonly string DecryptSecretFunctionCode;

        //--- Class Constructor ---
        static ModelModuleInitializer() {
            DecryptSecretFunctionCode = typeof(ModelModuleInitializer).Assembly.ReadManifestResource("LambdaSharp.Tool.Resources.DecryptSecretFunction.js");
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
                name: "Namespace",
                description: "Module Namespace",
                type: "String",
                scope: null,
                value: _builder.Namespace,
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
            _builder.AddVariable(
                parent: moduleItem,
                name: "Info",
                description: "Module Fullname, Version, and Origin",
                type: "String",
                scope: null,
                value: _builder.ModuleInfo.ToString(),
                allow: null,
                encryptionContext: null
            );

            // add overridable logging retention variable
            if(!_builder.TryGetOverride("Module::LogRetentionInDays", out var logRetentionInDays)) {
                logRetentionInDays = 30;
            }
            _builder.AddVariable(
                parent: moduleItem,
                name: "LogRetentionInDays",
                description: "Number days CloudWatch Log streams are retained for",
                type: "Number",
                scope: null,
                value: logRetentionInDays,
                allow: null,
                encryptionContext: null
            );

            // create module IAM role used by all functions
            _builder.TryGetOverride("Module::Role.PermissionsBoundary", out var rolePermissionsBoundary);
            var moduleRoleItem = _builder.AddResource(
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
                    PermissionsBoundary = rolePermissionsBoundary,
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
                pragmas: null,
                deletionPolicy: null
            );
            moduleRoleItem.DiscardIfNotReachable = true;

            // add deployment variables
            var deploymentItem = _builder.AddVariable(
                parent: null,
                name: "Deployment",
                description: "Deployment Variables",
                type: "String",
                scope: null,
                value: "",
                allow: null,
                encryptionContext: null
            );
            _builder.AddVariable(
                parent: deploymentItem,
                name: "Tier",
                description: "Deployment tier name",
                type: "String",
                scope: null,
                value: FnSelect("0", FnSplit("-", FnRef("DeploymentPrefix"))),
                allow: null,
                encryptionContext: null
            );
            _builder.AddVariable(
                parent: deploymentItem,
                name: "TierPrefix",
                description: "Deployment tier prefix",
                type: "String",
                scope: null,
                value: FnRef("DeploymentPrefix"),
                allow: null,
                encryptionContext: null
            );
            _builder.AddVariable(
                parent: deploymentItem,
                name: "TierLowercase",
                description: "Deployment tier name in lowercase characters",
                type: "String",
                scope: null,
                value: FnSelect("0", FnSplit("-", FnRef("DeploymentPrefixLowercase"))),
                allow: null,
                encryptionContext: null
            );
            _builder.AddVariable(
                parent: deploymentItem,
                name: "TierPrefixLowercase",
                description: "Deployment tier prefix in lowercase characters",
                type: "String",
                scope: null,
                value: FnRef("DeploymentPrefixLowercase"),
                allow: null,
                encryptionContext: null
            );
            _builder.AddVariable(
                parent: deploymentItem,
                name: "BucketName",
                description: "Deployment S3 Bucket Name",
                type: "String",
                scope: null,
                value: FnRef("DeploymentBucketName"),
                allow: null,
                encryptionContext: null
            );

            // add LambdaSharp Module Options
            var section = "LambdaSharp Module Options";
            var secretsParameter = _builder.AddParameter(
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
                pragmas: null,
                deletionPolicy: null
            );
            var secretsHasValueCondition = _builder.AddCondition(
                parent: secretsParameter,
                name: "HasValue",
                description: "",
                value: FnNot(FnEquals(FnRef("Secrets"), ""))
            );
            _builder.AddParameter(
                name: "XRayTracing",
                section: section,
                label: "Enable AWS X-Ray tracing mode for module resources",
                description: "AWS X-Ray Tracing",
                type: "String",
                scope: null,
                noEcho: null,
                defaultValue: XRayTracingLevel.Disabled.ToString(),
                constraintDescription: null,
                allowedPattern: null,
                allowedValues: new[] {
                    XRayTracingLevel.Disabled.ToString(),
                    XRayTracingLevel.RootModule.ToString(),
                    XRayTracingLevel.AllModules.ToString()
                },
                maxLength: null,
                maxValue: null,
                minLength: null,
                minValue: null,
                allow: null,
                properties: null,
                arnAttribute: null,
                encryptionContext: null,
                pragmas: null,
                deletionPolicy: null
            ).DiscardIfNotReachable = true;
            _builder.AddCondition(
                parent: null,
                name: "XRayIsEnabled",
                description: null,
                value: FnNot(FnEquals(FnRef("XRayTracing"), XRayTracingLevel.Disabled.ToString()))
            );
            _builder.AddCondition(
                parent: null,
                name: "XRayNestedIsEnabled",
                description: null,
                value: FnEquals(FnRef("XRayTracing"), XRayTracingLevel.AllModules.ToString())
            );

            // check if module might depend on core services
            if(_builder.HasLambdaSharpDependencies || _builder.HasModuleRegistration) {
                _builder.AddParameter(
                    name: "LambdaSharpCoreServices",
                    section: section,
                    label: "Integrate with LambdaSharp.Core services",
                    description: "Use LambdaSharp.Core Services",
                    type: "String",
                    scope: null,
                    noEcho: null,
                    defaultValue: "Disabled",
                    constraintDescription: null,
                    allowedPattern: null,
                    allowedValues: new[] {
                        "Disabled",
                        "Enabled"
                    },
                    maxLength: null,
                    maxValue: null,
                    minLength: null,
                    minValue: null,
                    allow: null,
                    properties: null,
                    arnAttribute: null,
                    encryptionContext: null,
                    pragmas: null,
                    deletionPolicy: null
                ).DiscardIfNotReachable = true;
                _builder.AddCondition(
                    parent: null,
                    name: "UseCoreServices",
                    description: null,
                    value: FnEquals(FnRef("LambdaSharpCoreServices"), "Enabled")
                );
            }

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
                    allow: null,
                    module: "LambdaSharp.Core",
                    encryptionContext: null,
                    out var _
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
                    encryptionContext: null,
                    out var _
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
                    encryptionContext: null,
                    out var _
                );
            }

            // add module variables
            if(TryGetModuleVariable("DeadLetterQueue", out var deadLetterQueueVariable, out var deadLetterQueueCondition)) {
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
                    name: "DeadLetterQueue",
                    awsType: null,
                    reference: FnRef("Module::DeadLetterQueue"),
                    allow: new[] {
                        "sqs:SendMessage"
                    },
                    condition: deadLetterQueueCondition
                );
            }
            if(TryGetModuleVariable("LoggingStream", out var loggingStreamVariable, out _)) {
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
            if(TryGetModuleVariable("LoggingStreamRole", out var loggingStreamRoleVariable, out _)) {
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

            // add KMS permissions for secrets in module
            _builder.AddRoleStatement(
                new Humidifier.Statement {
                    Sid = "DeploymentSecrets",
                    Effect = "Allow",
                    Resource = FnIf(
                        secretsHasValueCondition.FullName,
                        FnSplit(",", FnRef(secretsParameter.FullName)),
                        FnRef("AWS::NoValue")
                    ),
                    NotResource = FnIf(
                        secretsHasValueCondition.FullName,
                        FnRef("AWS::NoValue"),
                        "*"
                    ),
                    Action = new List<string> {
                        "kms:Decrypt",
                        "kms:Encrypt"
                    }
                }
            );
            if(_builder.Secrets.Any()) {
                _builder.AddGrant(
                    name: "EmbeddedSecrets",
                    awsType: null,
                    reference: _builder.Secrets.ToList(),
                    allow: new[] {
                        "kms:Decrypt",
                        "kms:Encrypt"
                    },
                    condition: null
                );
            }

            // add decryption function for secret parameters and values
            var decryptSecretFunction = _builder.AddInlineFunction(
                parent: moduleItem,
                name: "DecryptSecretFunction",
                description: "Module secret decryption function",
                environment: null,
                sources: null,
                condition: null,
                pragmas: new[] {
                    "no-registration",
                    "no-dead-letter-queue",
                    "no-wildcard-scoped-variables"
                },
                timeout: "30",
                memory: "128",
                code: DecryptSecretFunctionCode,
                dependsOn: null,
                role: FnGetAtt("Module::DecryptSecretFunction::Role", "Arn")
            );
            decryptSecretFunction.DiscardIfNotReachable = true;

            // add custom role decryption function
            var decryptSecretFunctionPolicyStatements = new List<Humidifier.Statement> {
                new Humidifier.Statement {
                    Sid = "DeploymentSecrets",
                    Effect = "Allow",
                    Resource = FnIf(
                        secretsHasValueCondition.FullName,
                        FnSplit(",", FnRef(secretsParameter.FullName)),
                        FnRef("AWS::NoValue")
                    ),
                    NotResource = FnIf(
                        secretsHasValueCondition.FullName,
                        FnRef("AWS::NoValue"),
                        "*"
                    ),
                    Action = new List<string> {
                        "kms:Decrypt",
                        "kms:Encrypt"
                    }
                }
            };
            if(_builder.Secrets.Any()) {
                decryptSecretFunctionPolicyStatements.Add(new Humidifier.Statement {
                    Sid = "EmbeddedSecrets",
                    Effect = "Allow",
                    Resource = _builder.Secrets.ToList(),
                    Action = new List<string> {
                        "kms:Decrypt",
                        "kms:Encrypt"
                    }
                });
            }
            var decryptSecretFunctionRoleItem = _builder.AddResource(
                parent: decryptSecretFunction,
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
                    PermissionsBoundary = rolePermissionsBoundary,
                    Policies = new[] {
                        new Humidifier.IAM.Policy {
                            PolicyName = FnSub("${AWS::StackName}DecryptSecretFunction"),
                            PolicyDocument = new Humidifier.PolicyDocument {
                                Version = "2012-10-17",
                                Statement = decryptSecretFunctionPolicyStatements
                            }
                        }
                    }.ToList()
                },
                resourceExportAttribute: null,
                dependsOn: null,
                condition: null,
                pragmas: null,
                deletionPolicy: null
            );
            decryptSecretFunctionRoleItem.DiscardIfNotReachable = true;

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
                pragmas: null,
                deletionPolicy: null
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
                pragmas: null,
                deletionPolicy: null
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
                pragmas: null,
                deletionPolicy: null
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
                pragmas: null,
                deletionPolicy: null
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
                pragmas: null,
                deletionPolicy: null
            );

            // permissions needed for writing to log streams (but not for creating log groups!)
            _builder.AddGrant(
                name: "LogStream",
                awsType: null,
                reference: "arn:aws:logs:*:*:*",
                allow: new[] {
                    "logs:CreateLogStream",
                    "logs:PutLogEvents"
                },
                condition: null
            );

            // permissions needed for reading state of CloudFormation stack (used by Finalizer to confirm a delete operation is happening)
            _builder.AddGrant(
                name: "CloudFormation",
                awsType: null,
                reference: FnRef("AWS::StackId"),
                allow: new[] {
                    "cloudformation:DescribeStacks"
                },
                condition: null
            );

            // permissions needed for X-Ray lambda daemon to upload tracing information
            _builder.AddGrant(
                name: "AWSXRay",
                awsType: null,
                reference: "*",
                allow: new[] {
                    "xray:PutTraceSegments",
                    "xray:PutTelemetryRecords",
                    "xray:GetSamplingRules",
                    "xray:GetSamplingTargets",
                    "xray:GetSamplingStatisticSummaries"
                },
                condition: null
            );

            // permission needed for posting events to the default event bus
            _builder.AddGrant(
                name: "EventBus",
                awsType: null,
                reference: FnSub("arn:${AWS::Partition}:events:${AWS::Region}:${AWS::AccountId}:event-bus/default"),
                allow: new[] {
                    "events:PutEvents"
                },
                condition: null
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

            // TODO (2020-06-30, bjorg): should we also check for function.Function.Properties["VpcConfig"]?

            // permissions needed for lambda functions to exist in a VPC
            if(functions.Any(function => function.Function.VpcConfig != null)) {
                _builder.AddGrant(
                    name: "VpcNetworkInterfaces",
                    awsType: null,
                    reference: "*",
                    allow: new[] {
                        "ec2:DescribeNetworkInterfaces",
                        "ec2:CreateNetworkInterface",
                        "ec2:DeleteNetworkInterface"
                    },
                    condition: null
                );
            }

            // add module registration
            if(_builder.HasModuleRegistration) {

                // create module registration
                _builder.AddResource(
                    parent: moduleItem,
                    name: "Registration",
                    description: null,
                    type: "LambdaSharp::Registration::Module",
                    scope: null,
                    allow: null,
                    properties: new Dictionary<string, object> {
                        ["ModuleInfo"] = _builder.ModuleInfo.ToString(),
                        ["ModuleId"] = FnRef("AWS::StackName")
                    },
                    dependsOn: null,
                    arnAttribute: null,
                    condition: "UseCoreServices",
                    pragmas: null,
                    deletionPolicy: null
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
                                ["ModuleId"] = FnRef("Module::Id"),
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
                            condition: (function.Condition != null)
                                ? FnAnd(FnCondition("UseCoreServices"), FnCondition(function.Condition))
                                : "UseCoreServices",
                            pragmas: null,
                            deletionPolicy: null
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
                                resource: new Humidifier.CustomResource("AWS::Logs::SubscriptionFilter") {
                                    ["DestinationArn"] = FnRef("Module::LoggingStream"),
                                    ["FilterPattern"] = "-\"*** \"",
                                    ["LogGroupName"] = FnRef($"{function.FullName}::LogGroup"),
                                    ["RoleArn"] = FnRef("Module::LoggingStreamRole")
                                },
                                resourceExportAttribute: null,
                                dependsOn: null,
                                condition: (function.Condition != null)
                                    ? FnAnd(FnCondition("UseCoreServices"), FnCondition(function.Condition))
                                    : "UseCoreServices",
                                pragmas: null,
                                deletionPolicy: null
                            );
                        }
                    }
                }

                // add app registration
                var registeredApps = _builder.Items
                    .OfType<AppItem>()
                    .Where(app => app.HasAppRegistration)
                    .ToList();
                if(registeredApps.Any()) {

                    // create registration-related resources for functions
                    foreach(var app in registeredApps) {

                        // create app log-group subscription
                        if(
                            _builder.TryGetItem("Module::LoggingStream", out _)
                            && _builder.TryGetItem("Module::LoggingStreamRole", out _)
                        ) {
                            _builder.AddResource(
                                parent: app,
                                name: "LogGroupSubscription",
                                description: null,
                                scope: null,
                                resource: new Humidifier.CustomResource("AWS::Logs::SubscriptionFilter") {
                                    ["DestinationArn"] = FnRef("Module::LoggingStream"),
                                    ["FilterPattern"] = "-\"*** \"",
                                    ["LogGroupName"] = FnRef($"{app.FullName}::LogGroup"),
                                    ["RoleArn"] = FnRef("Module::LoggingStreamRole")
                                },
                                resourceExportAttribute: null,
                                dependsOn: null,
                                condition: "UseCoreServices",
                                pragmas: null,
                                deletionPolicy: null
                            );

                            // create app registration
                            _builder.AddResource(
                                parent: app,
                                name: "Registration",
                                description: null,
                                type: "LambdaSharp::Registration::App",
                                scope: null,
                                allow: null,
                                properties: new Dictionary<string, object> {
                                    ["ModuleId"] = FnRef("Module::Id"),
                                    ["AppLogGroup"] = FnRef($"{app.FullName}::LogGroup"),
                                    ["AppId"] = FnRef(app.FullName),
                                    ["AppName"] = app.Name,
                                    ["AppPlatform"] = FnRef($"{app.FullName}::AppPlatform"),
                                    ["AppFramework"] = FnRef($"{app.FullName}::AppFramework"),
                                    ["AppLanguage"] = FnRef($"{app.FullName}::AppLanguage")
                                },
                                dependsOn: new[] { "Module::Registration" },
                                arnAttribute: null,
                                condition: "UseCoreServices",
                                pragmas: null,
                                deletionPolicy: null
                            );
                        }
                    }
                }
            }
        }

        private bool TryGetModuleVariable(string name, out object variable, out string condition) {
            if(_builder.TryGetOverride($"Module::{name}", out variable)) {
                condition = null;
                return true;
            }
            if(_builder.HasLambdaSharpDependencies) {
                condition = "UseCoreServices";
                variable = FnIf("UseCoreServices", FnRef($"LambdaSharp::{name}"), FnRef("AWS::NoValue"));
                return true;
            }
            variable = null;
            condition = null;
            return false;
        }
    }
}
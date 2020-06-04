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

#nullable disable

using System;
using System.Linq;
using LambdaSharp.Compiler;
using LambdaSharp.Compiler.Syntax;

namespace LambdaSharp.Tool.Compiler.Analyzers {

    public partial class StructureAnalyzer {

        //--- Methods ---
        public override bool VisitStart(ModuleDeclaration node) {
            _builder.ModuleDeclaration = node;

            // ensure module version is present and valid
            if(!VersionInfo.TryParse(node.Version.Value, out var version)) {
                _builder.Log(Error.VersionAttributeInvalid, node.Version);
                version = VersionInfo.Parse("0.0");
            }
            if(_builder.ModuleVersion == null) {
                _builder.ModuleVersion = version;
            }

            // ensure module has a namespace and name
            if(TryParseModuleFullName(node.ModuleName.Value, out string moduleNamespace, out var moduleName)) {
                _builder.ModuleNamespace = moduleNamespace;
                _builder.ModuleName = moduleName;
            } else {
                _builder.Log(Error.ModuleNameAttributeInvalid, node.ModuleName);
            }

            // validate secrets
            foreach(var secret in node.Secrets) {
                if(secret.Value.Equals("aws/ssm", StringComparison.OrdinalIgnoreCase)) {
                    _builder.Log(Error.CannotGrantPermissionToDecryptParameterStore, secret);
                } else if(secret.Value.StartsWith("arn:", StringComparison.Ordinal)) {
                    if(!SecretArnRegex.IsMatch(secret.Value)) {
                        _builder.Log(Error.SecretKeyMustBeValidARN, secret);
                    }
                } else if(SecretAliasRegex.IsMatch(secret.Value)) {

                    // TODO: resolve KMS key alias to ARN

                    // // assume key name is an alias and resolve it to its ARN
                    // try {
                    //     var response = Settings.KmsClient.DescribeKeyAsync(textSecret).Result;
                    //     _secrets.Add(response.KeyMetadata.Arn);
                    // } catch(Exception e) {
                    //     LogError($"failed to resolve key alias: {textSecret}", e);
                    // }
                } else {
                    _builder.Log(Error.SecreteKeyMustBeValidAlias, secret);
                }
            }

            // add built-in AWS variables
            var awsGroupDeclaration = AddDeclaration(node, new GroupDeclaration(Fn.Literal("AWS")) {
                Description = Fn.Literal("AWS Pseudo-Parameters")
            });
            AddDeclaration(awsGroupDeclaration, new PseudoParameterDeclaration(Fn.Literal("AccountId")) {
                Description = Fn.Literal("AWS account ID of the account in which the CloudFormation stack is being created")
            });
            AddDeclaration(awsGroupDeclaration, new PseudoParameterDeclaration(Fn.Literal("NotificationARNs")) {
                Description = Fn.Literal("List of notification Amazon Resource Names (ARNs) for the current CloudFormation stack")
            });
            AddDeclaration(awsGroupDeclaration, new PseudoParameterDeclaration(Fn.Literal("NoValue")) {
                Description = Fn.Literal("Removes the resource property it is assigned to")
            });
            AddDeclaration(awsGroupDeclaration, new PseudoParameterDeclaration(Fn.Literal("Partition")) {
                Description = Fn.Literal("Partition that the resource is in (e.g. aws, aws-cn, aws-us-gov, etc.)")
            });
            AddDeclaration(awsGroupDeclaration, new PseudoParameterDeclaration(Fn.Literal("Region")) {
                Description = Fn.Literal("AWS Region in which the CloudFormation stack is located")
            });
            AddDeclaration(awsGroupDeclaration, new PseudoParameterDeclaration(Fn.Literal("StackId")) {
                Description = Fn.Literal("ARN of the current CloudFormation stack")
            });
            AddDeclaration(awsGroupDeclaration, new PseudoParameterDeclaration(Fn.Literal("StackName")) {
                Description = Fn.Literal("Name of the current CloudFormation stack")
            });
            AddDeclaration(awsGroupDeclaration, new PseudoParameterDeclaration(Fn.Literal("URLSuffix")) {
                Description = Fn.Literal("Suffix for a domain (e.g. amazonaws.com, amazonaws.com.cn, etc.)")
            });

            // add implicit module variables
            var moduleGroupDeclaration = AddDeclaration(node, new GroupDeclaration(Fn.Literal("Module")) {
                Description = Fn.Literal("Module Variables")
            });
            AddDeclaration(moduleGroupDeclaration, new VariableDeclaration(Fn.Literal("Id")) {
                Description = Fn.Literal("Module ID"),
                Value = Fn.Ref("AWS::StackName")
            });
            AddDeclaration(moduleGroupDeclaration, new VariableDeclaration(Fn.Literal("Namespace")) {
                Description = Fn.Literal("Module Namespace"),
                Value = Fn.Literal(_builder.ModuleNamespace)
            });
            AddDeclaration(moduleGroupDeclaration, new VariableDeclaration(Fn.Literal("Name")) {
                Description = Fn.Literal("Module Name"),
                Value = Fn.Literal(_builder.ModuleName)
            });
            AddDeclaration(moduleGroupDeclaration, new VariableDeclaration(Fn.Literal("FullName")) {
                Description = Fn.Literal("Module Full Name"),
                Value = Fn.Literal(_builder.ModuleFullName)
            });
            AddDeclaration(moduleGroupDeclaration, new VariableDeclaration(Fn.Literal("Version")) {
                Description = Fn.Literal("Module Version"),
                Value = Fn.Literal(_builder.ModuleVersion.ToString())
            });
            AddDeclaration(moduleGroupDeclaration, new ConditionDeclaration(Fn.Literal("IsNested")) {
                Description = Fn.Literal("Module is nested"),
                Value = Fn.Not(Fn.Equals(Fn.Ref("DeploymentRoot"), Fn.Literal("")))
            });
            AddDeclaration(moduleGroupDeclaration, new VariableDeclaration(Fn.Literal("RootId")) {
                Description = Fn.Literal("Root Module ID"),
                Value = Fn.If("Module::IsNested", Fn.Ref("DeploymentRoot"), Fn.Ref("Module::Id"))
            });

            // create module IAM role used by all functions
            AddDeclaration(moduleGroupDeclaration, new ResourceDeclaration(Fn.Literal("Role")) {
                Type = Fn.Literal("AWS::IAM::Role"),
                Properties = new ObjectExpression {
                    ["AssumeRolePolicyDocument"] = new ObjectExpression {
                        ["Version"] = Fn.Literal("2012-10-17"),
                        ["Statement"] = new ListExpression {
                            new ObjectExpression {
                                ["Sid"] = Fn.Literal("ModuleLambdaPrincipal"),
                                ["Effect"] = Fn.Literal("Allow"),
                                ["Principal"] = new ObjectExpression {
                                    ["Service"] = Fn.Literal("lambda.amazonaws.com")
                                },
                                ["Action"] = Fn.Literal("sts:AssumeRole")
                            }
                        }
                    },
                    ["Policies"] = new ListExpression {
                        new ObjectExpression {
                            ["PolicyName"] = Fn.Sub("${AWS::StackName}ModulePolicy"),
                            ["PolicyDocument"] = new ObjectExpression {
                                ["Version"] = Fn.Literal("2012-10-17"),
                                ["Statement"] = new ListExpression()
                            }
                        }
                    }
                },
                DiscardIfNotReachable = true
            });

            // add overridable logging retention variable
            if(!TryGetOverride(node, "Module::LogRetentionInDays", out var logRetentionInDays)) {
                logRetentionInDays = Fn.Literal(30);
            }
            AddDeclaration(moduleGroupDeclaration, new VariableDeclaration(Fn.Literal("LogRetentionInDays")) {
                Description = Fn.Literal("Number days log entries are retained for"),
                Type = Fn.Literal("Number"),
                Value = logRetentionInDays
            });

            // add LambdaSharp Module Options
            var section = "LambdaSharp Module Options";
            AddDeclaration(node, new ParameterDeclaration(Fn.Literal("Secrets")) {
                Section = Fn.Literal(section),
                Label = Fn.Literal("Comma-separated list of additional KMS secret keys"),
                Description = Fn.Literal("Secret Keys (ARNs)"),
                Default = Fn.Literal("")
            });
            AddDeclaration(node, new ParameterDeclaration(Fn.Literal("XRayTracing")) {
                Section = Fn.Literal(section),
                Label = Fn.Literal("Enable AWS X-Ray tracing mode for module resources"),
                Description = Fn.Literal("AWS X-Ray Tracing"),
                Default = Fn.Literal(XRayTracingLevel.Disabled.ToString()),
                AllowedValues = new SyntaxNodeCollection<LiteralExpression> {
                    Fn.Literal(XRayTracingLevel.Disabled.ToString()),
                    Fn.Literal(XRayTracingLevel.RootModule.ToString()),
                    Fn.Literal(XRayTracingLevel.AllModules.ToString())
                },
                DiscardIfNotReachable = true
            });

            // TODO (2019-11-05, bjorg): consider making this a child declaration of the parameter XRayTracing::IsEnabled
            AddDeclaration(node, new ConditionDeclaration(Fn.Literal("XRayIsEnabled")) {
                Value = Fn.Not(Fn.Equals(Fn.Ref("XRayTracing"), Fn.Literal(XRayTracingLevel.Disabled.ToString())))
            });

            // TODO (2019-11-05, bjorg): consider making this a child declaration of the parameter XRayTracing::NestedIsEnabled
            AddDeclaration(node, new ConditionDeclaration(Fn.Literal("XRayNestedIsEnabled")) {
                Value = Fn.Equals(Fn.Ref("XRayTracing"), Fn.Literal(XRayTracingLevel.AllModules.ToString()))
            });

            // check if module might depdent on core services
            if(node.HasLambdaSharpDependencies || node.HasModuleRegistration) {
                AddDeclaration(node, new ParameterDeclaration(Fn.Literal("LambdaSharpCoreServices")) {
                    Section = Fn.Literal(section),
                    Label = Fn.Literal("Integrate with LambdaSharp.Core services"),
                    Description = Fn.Literal("Use LambdaSharp.Core Services"),

                    // TODO (2019-11-05, bjorg): use enum with ToString() instead of hard-coded strings
                    Default = Fn.Literal("Disabled"),
                    AllowedValues = new SyntaxNodeCollection<LiteralExpression> {
                        Fn.Literal("Disabled"),
                        Fn.Literal("Enabled")
                    },
                    DiscardIfNotReachable = true
                });
                AddDeclaration(node, new ConditionDeclaration(Fn.Literal("UseCoreServices")) {

                    // TODO (2019-11-05, bjorg): use enum with ToString() instead of hard-coded strings
                    Value = Fn.Equals(Fn.Ref("LambdaSharpCoreServices"), Fn.Literal("Enabled"))
                });
            }

            // import lambdasharp dependencies (unless requested otherwise)
            if(node.HasLambdaSharpDependencies) {

                // add LambdaSharp Module Internal resource imports
                var lambdasharpGroupDeclaration = AddDeclaration(node, new GroupDeclaration(Fn.Literal("LambdaSharp")) {
                   Description = Fn.Literal("LambdaSharp Core Imports")
                });
                AddDeclaration(lambdasharpGroupDeclaration, new ImportDeclaration(Fn.Literal("DeadLetterQueue")) {
                    Module = Fn.Literal("LambdaSharp.Core"),

                    // TODO (2018-12-01, bjorg): consider using 'AWS::SQS::Queue'
                    Type = Fn.Literal("String")
                });
                AddDeclaration(lambdasharpGroupDeclaration, new ImportDeclaration(Fn.Literal("LoggingStream")) {
                    Module = Fn.Literal("LambdaSharp.Core"),

                    // NOTE (2018-12-11, bjorg): we use type 'String' to be more flexible with the type of values we're willing to take
                    Type = Fn.Literal("String")
                });
                AddDeclaration(lambdasharpGroupDeclaration, new ImportDeclaration(Fn.Literal("LoggingStreamRole")) {
                    Module = Fn.Literal("LambdaSharp.Core"),

                    // NOTE (2018-12-11, bjorg): we use type 'String' to be more flexible with the type of values we're willing to take
                    Type = Fn.Literal("String")
                });
            }

            // add module variables
            if(TryGetVariable(node, "DeadLetterQueue", out var deadLetterQueueVariable, out var deadLetterQueueCondition)) {
                var deadLetterQueueDeclaration = AddDeclaration(moduleGroupDeclaration, new VariableDeclaration(Fn.Literal("DeadLetterQueue")) {
                    Description = Fn.Literal("Module Dead Letter Queue (ARN)"),
                    Value = deadLetterQueueVariable
                });
                AddGrant(
                    name: deadLetterQueueDeclaration.FullName,
                    awsType: null,
                    reference: Fn.Ref("Module::DeadLetterQueue"),
                    allow: new SyntaxNodeCollection<LiteralExpression> {
                        Fn.Literal("sqs:SendMessage")
                    },
                    condition: deadLetterQueueCondition
                );
            }
            if(TryGetVariable(node, "LoggingStream", out var loggingStreamVariable, out var _)) {
                AddDeclaration(moduleGroupDeclaration, new VariableDeclaration(Fn.Literal("LoggingStream")) {
                    Description = Fn.Literal("Module Logging Stream (ARN)"),
                    Value = loggingStreamVariable,

                    // TODO (2019-11-05, bjorg): can we use a more specific type than 'String' here?
                    Type = Fn.Literal("String")
                });
            }
            if(TryGetVariable(node, "LoggingStreamRole", out var loggingStreamRoleVariable, out var _)) {
                AddDeclaration(moduleGroupDeclaration, new VariableDeclaration(Fn.Literal("LoggingStreamRole")) {
                    Description = Fn.Literal("Module Logging Stream Role (ARN)"),
                    Value = loggingStreamRoleVariable,

                    // TODO (2019-11-05, bjorg): consider using 'AWS::IAM::Role'
                    Type = Fn.Literal("String")

                });
            }

            // add KMS permissions for secrets in module
            if(node.Secrets.Any()) {
                AddGrant(
                    name: "EmbeddedSecrets",
                    awsType: null,
                    reference: new ListExpression(node.Secrets),
                    allow: new SyntaxNodeCollection<LiteralExpression> {
                        Fn.Literal("kms:Decrypt"),
                        Fn.Literal("kms:Encrypt")
                    },
                    condition: null
                );
            }

            // add decryption function for secret parameters and values
            AddDeclaration(node, new FunctionDeclaration(Fn.Literal("DecryptSecretFunction")) {
                Description = Fn.Literal("Module secret decryption function"),
                Environment = new ObjectExpression {

                    // NOTE (2019-11-05, bjorg): we use the Lambda environment to introduce a conditional dependency
                    //  on the policy for KMS keys passed in through the 'Secrets' parameter; without this dependency,
                    //  the Lambda function could run before the policy is in effect, causing it to fail.
                    ["MODULE_ROLE_SECRETSPOLICY"] = Fn.If(
                        "Module::Role::SecretsPolicy::Condition",
                        Fn.Ref("Module::Role::SecretsPolicy"),
                        Fn.Ref("AWS::NoValue")
                    )
                },
                Pragmas = new ListExpression {
                    Fn.Literal("no-function-registration"),
                    Fn.Literal("no-dead-letter-queue"),
                    Fn.Literal("no-wildcard-scoped-variables")
                },
                Timeout = Fn.Literal(30),
                Memory = Fn.Literal(128),
                Runtime = Fn.Literal(Amazon.Lambda.Runtime.Nodejs12X.ToString()),
                Handler = Fn.Literal("index.handler"),
                Language = Fn.Literal("javascript"),
                Properties = new ObjectExpression {
                    ["Code"] = new ObjectExpression {
                        ["ZipFile"] = Fn.Literal(_decryptSecretFunctionCode)
                    }
                },
                DiscardIfNotReachable = true
            });

            // add LambdaSharp Deployment Settings
            section = "LambdaSharp Deployment Settings (DO NOT MODIFY)";
            AddDeclaration(node, new ParameterDeclaration(Fn.Literal("DeploymentBucketName")) {
                Section = Fn.Literal(section),
                Label = Fn.Literal("Deployment S3 bucket name"),
                Description = Fn.Literal("Deployment S3 Bucket Name")
            });
            AddDeclaration(node, new ParameterDeclaration(Fn.Literal("DeploymentPrefix")) {
                Section = Fn.Literal(section),
                Label = Fn.Literal("Deployment tier prefix"),
                Description = Fn.Literal("Deployment Tier Prefix")
            });
            AddDeclaration(node, new ParameterDeclaration(Fn.Literal("DeploymentPrefixLowercase")) {
                Section = Fn.Literal(section),
                Label = Fn.Literal("Deployment tier prefix (lowercase)"),
                Description = Fn.Literal("Deployment Tier Prefix (lowercase)")
            });
            AddDeclaration(node, new ParameterDeclaration(Fn.Literal("DeploymentRoot")) {
                Section = Fn.Literal(section),
                Label = Fn.Literal("Root stack name for nested deployments, blank otherwise"),
                Description = Fn.Literal("Root Stack Name"),
                Default = Fn.Literal("")
            });
            AddDeclaration(node, new ParameterDeclaration(Fn.Literal("DeploymentChecksum")) {
                Section = Fn.Literal(section),
                Label = Fn.Literal("CloudFormation template MD5 checksum"),
                Description = Fn.Literal("Deployment Checksum"),
                Default = Fn.Literal("")
            });

            // add conditional KMS permissions for secrets parameter
            AddGrant(
                name: "Secrets",
                awsType: null,
                reference: Fn.Split(",", Fn.Ref("Secrets")),
                allow: new SyntaxNodeCollection<LiteralExpression> {
                    Fn.Literal("kms:Decrypt"),
                    Fn.Literal("kms:Encrypt")
                },
                condition: Fn.Not(Fn.Equals(Fn.Ref("Secrets"), Fn.Literal("")))
            );

            // permissions needed for writing to log streams (but not for creating log groups!)
            AddGrant(
                name: "LogStream",
                awsType: null,
                reference: Fn.Literal("arn:aws:logs:*:*:*"),
                allow: new SyntaxNodeCollection<LiteralExpression> {
                    Fn.Literal("logs:CreateLogStream"),
                    Fn.Literal("logs:PutLogEvents")
                },
                condition: null
            );

            // permissions needed for reading state of CloudFormation stack (used by Finalizer to confirm a delete operation is happening)
            AddGrant(
                name: "CloudFormation",
                awsType: null,
                reference: Fn.Ref("AWS::StackId"),
                allow: new SyntaxNodeCollection<LiteralExpression> {
                    Fn.Literal("cloudformation:DescribeStacks")
                },
                condition: null
            );

            // permissions needed for X-Ray lambda daemon to upload tracing information
            AddGrant(
                name: "AWS-XRay",
                awsType: null,
                reference: Fn.Literal("*"),
                allow: new SyntaxNodeCollection<LiteralExpression> {
                    Fn.Literal("xray:PutTraceSegments"),
                    Fn.Literal("xray:PutTelemetryRecords"),
                    Fn.Literal("xray:GetSamplingRules"),
                    Fn.Literal("xray:GetSamplingTargets"),
                    Fn.Literal("xray:GetSamplingStatisticSummaries")
                },
                condition: null
            );

            // add module registration
            if(node.HasModuleRegistration) {

                // create module registration
                AddDeclaration(node, new ResourceDeclaration(Fn.Literal("Registration")) {
                    Type = Fn.Literal("LambdaSharp::Registration::Module"),
                    Properties = new ObjectExpression {
                        ["Module"] = Fn.Literal(_builder.ModuleInfo.ToString()),
                        ["ModuleId"] = Fn.Ref("AWS::StackName")
                    },
                    If = Fn.Condition("UseCoreServices")
                });
            }
            return true;
        }

        public override ASyntaxNode VisitEnd(ModuleDeclaration node) {

            // permissions needed for lambda functions to exist in a VPC
            if(_builder.ItemDeclarations.OfType<FunctionDeclaration>().Any()) {
                AddGrant(
                    name: "VpcNetworkInterfaces",
                    awsType: null,
                    reference: Fn.Literal("*"),
                    allow: new SyntaxNodeCollection<LiteralExpression> {
                        Fn.Literal("ec2:DescribeNetworkInterfaces"),
                        Fn.Literal("ec2:CreateNetworkInterface"),
                        Fn.Literal("ec2:DeleteNetworkInterface")
                    },
                    condition: null
                );
            }

            // TODO: this might be a good spot to compute the effective role permissions
            return node;
        }
    }
}
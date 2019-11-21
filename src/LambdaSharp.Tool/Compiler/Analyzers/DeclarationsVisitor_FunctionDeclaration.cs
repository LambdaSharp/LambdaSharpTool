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
using System.IO;
using System.Linq;
using System.Xml.Linq;
using LambdaSharp.Tool.Compiler.Parser.Syntax;

namespace LambdaSharp.Tool.Compiler.Analyzers {

    public partial class DeclarationsVisitor {

        //--- Class Fields ---

        // NOTE (2019-11-20, bjorg): list of S3 event types taken from https://docs.aws.amazon.com/AmazonS3/latest/dev/NotificationHowTo.html
        private static HashSet<string> S3EventTypes = new HashSet<string> {
            "s3:ObjectCreated:*",
            "s3:ObjectCreated:Put",
            "s3:ObjectCreated:Post",
            "s3:ObjectCreated:Copy",
            "s3:ObjectCreated:CompleteMultipartUpload",
            "s3:ObjectRemoved:*",
            "s3:ObjectRemoved:Delete",
            "s3:ObjectRemoved:DeleteMarkerCreated",
            "s3:ObjectRestore:Post",
            "s3:ObjectRestore:Completed",
            "s3:ReducedRedundancyLostObject",
            "s3:Replication:OperationFailedReplication",
            "s3:Replication:OperationMissedThreshold",
            "s3:Replication:OperationReplicatedAfterThreshold",
            "s3:Replication:OperationNotTracked"
        };

        //--- Methods ---
        public override void VisitStart(ASyntaxNode parent, FunctionDeclaration node) {

            // validate attributes
            ValidateExpressionIsLiteralOrListOfLiteral(node.Scope);
            ValidateExpressionIsNumber(node, node.Memory, Error.MemoryAttributeInvalid);
            ValidateExpressionIsNumber(node, node.Timeout, Error.TimeoutAttributeInvalid);

            // check if function uses inline code or a project
            var isInlineFunction = false;
            if(
                node.Properties.TryGetValue("Code", out var codeProperty)
                && (codeProperty is ObjectExpression codeObject)
                && (codeObject.ContainsKey("ZipFile"))
            ) {
                isInlineFunction = true;

                // lambda declaration uses inline code; validate the other required fields are set
                if(node.Runtime == null) {
                    _builder.Log(Error.RuntimeAttributeMissing, node);
                }
                if(node.Handler == null) {
                    _builder.Log(Error.HandlerAttributeMissing, node);
                }
                if(node.Language == null) {
                    _builder.Log(Error.LanguageAttributeMissing, node);
                }
            } else {
                var workingDirectory = Path.GetDirectoryName(node.SourceLocation.FilePath);
                string project = null;

                // determine project file location and type
                if(node.Project == null) {

                    // use function name to determine function project file
                    project = DetermineProjectFileLocation(Path.Combine(workingDirectory, node.Function.Value));
                } else {

                    // check if the project type can be determined by the project file extension
                    project = Path.Combine(workingDirectory, node.Project.Value);
                    if(File.Exists(project)) {

                        // check if project file extension is supported
                        switch(Path.GetExtension(project).ToLowerInvariant()) {
                        case ".csproj":
                        case ".js":
                        case ".sbt":

                            // known extension for an existing project file; nothing to do
                            break;
                        default:
                            project = null;
                            break;
                        }
                    } else {
                        project = DetermineProjectFileLocation(project);
                    }
                }
                if(project == null) {
                    _builder.Log(Error.ProjectAttributeInvalid, node);
                    return;
                }

                // update 'Project' attribute with known project file that exists
                node.Project = new LiteralExpression {
                    Parent = node,
                    SourceLocation = node.Project?.SourceLocation,
                    Value = project
                };

                // fill in missing attributes based on function type
                switch(Path.GetExtension(project).ToLowerInvariant()) {
                case ".csproj":
                    DetermineDotNetFunctionProperties(project);
                    break;
                case ".js":
                    DetermineJavascriptFunctionProperties();
                    break;
                case ".sbt":
                    DetermineScalaFunctionProperties();
                    break;
                default:
                    _builder.Log(Error.LanguageAttributeInvalid, node);
                    break;
                }
            }

            // check if lambdasharp DeadLetterQueue needs to be set
            if(
                !node.Properties.ContainsKey("DeadLetterConfig")
                && HasDeadLetterQueue(node)
                && _builder.TryGetItemDeclaration("Module::DeadLetterQueue", out _)
            ) {

                // initialize dead-letter queue
                node.Properties["DeadLetterConfig"] = new ObjectExpression {
                    ["TargetArn"] = FnRef("Module::DeadLetterQueue")
                };
            }

            // check if resource is conditional
            if((node.If != null) && !(node.If is ConditionExpression)) {

                // convert conditional expression to a condition literal
                var condition = AddDeclaration(node, new ConditionDeclaration {
                    Condition = Literal("If"),
                    Value = node.If
                });
                node.If = FnCondition(condition.FullName);
            }


            // initialize 'Properties' attribute
            if(node.Description != null) {
                SetProperty("Description", Literal(node.Description.Value.TrimEnd() + $" (v{_builder.ModuleVersion.ToString()})"));
            }
            SetProperty("Timeout", node.Timeout);
            SetProperty("Runtime", node.Runtime);
            SetProperty("MemorySize", node.Memory);
            SetProperty("Handler", node.Handler);
            SetProperty("Role", FnGetAtt("Module::Role", "Arn"));
            SetProperty("Environment", new ObjectExpression {
                ["Variables"] = new ObjectExpression()
            });
            if(!isInlineFunction) {

                // add variable for package name
                var packageVariable = AddDeclaration(node, new VariableDeclaration {
                    Variable = Literal("PackageName"),
                    Value = Literal($"{node.LogicalId}-DRYRUN.zip")
                });
                SetProperty("Code", new ObjectExpression {
                    ["S3Key"] = GetModuleArtifactExpression($"${{{packageVariable.FullName}}}"),
                    ["S3Bucket"] = FnRef("DeploymentBucketName")
                });
            }
            SetProperty("TracingConfig", new ObjectExpression {
                ["Mode"] = FnIf("XRayIsEnabled", Literal("Active"), Literal("PassThrough"))
            });

            // create function log-group with retention window
            AddDeclaration(node, new ResourceDeclaration {
                Resource = Literal("LogGroup"),
                Type = Literal("AWS::Logs::LogGroup"),
                Properties = new ObjectExpression {
                    ["LogGroupName"] = FnSub($"/aws/lambda/${{{node.FullName}}}"),

                    // TODO (2019-10-25, bjorg): allow 'LogRetentionInDays' attribute on 'Function' declaration
                    ["RetentionInDays"] = FnRef("Module::LogRetentionInDays")
                },

                // TODO: we should clone this
                If = node.If
            });

            // check if function is a Finalizer
            var isFinalizer = (node.Parents.OfType<ADeclaration>() is ModuleDeclaration) && (node.Function.Value == "Finalizer");
            if(isFinalizer) {

                // finalizer doesn't need a dead-letter queue or registration b/c it gets deleted anyway on failure or teardown
                node.Pragmas.Add(Literal("no-function-registration"));
                node.Pragmas.Add(Literal("no-dead-letter-queue"));

                // NOTE (2018-12-18, bjorg): always set the 'Finalizer' timeout to the maximum limit to prevent ugly timeout scenarios
                node.Properties["Timeout"] = Literal(900);

                // add finalizer invocation (dependsOn will be set later when all resources have been added)
                AddDeclaration(node, new ResourceDeclaration {
                    Resource = Literal("Invocation"),
                    Type = Literal("Module::Finalizer"),
                    Properties = new ObjectExpression {
                        ["ServiceToken"] = FnGetAtt(node.FullName, "Arn"),
                        ["DeploymentChecksum"] = FnRef("DeploymentChecksum"),
                        ["ModuleVersion"] = Literal(_builder.ModuleVersion.ToString())
                    },

                    // TODO: we should clone this
                    If = node.If
                });
            }

            // TODO: validate properties

            // check if function must be registered
            if(HasModuleRegistration(node.Parents.OfType<ModuleDeclaration>().First()) && HasFunctionRegistration(node)) {

                // create function registration
                AddDeclaration(node, new ResourceDeclaration {
                    Resource = Literal("Registration"),
                    Type = Literal("LambdaSharp::Registration::Function"),
                    Properties = new ObjectExpression {
                        ["ModuleId"] = FnRef("AWS::StackName"),
                        ["FunctionId"] = FnRef(node.FullName),
                        ["FunctionName"] = Literal(node.Function.Value),
                        ["FunctionLogGroupName"] = FnSub($"/aws/lambda/${{{node.FullName}}}"),
                        ["FunctionPlatform"] = Literal("AWS Lambda"),
                        ["FunctionFramework"] = node.Runtime,
                        ["FunctionLanguage"] = node.Language,
                        ["FunctionMaxMemory"] = node.Memory,
                        ["FunctionMaxDuration"] = node.Timeout
                    },
                    DependsOn = new List<LiteralExpression> {
                        Literal("Module::Registration")
                    },
                    If = (node.IfConditionName != null)
                        ? (AExpression)FnAnd(FnCondition("UseCoreServices"), FnCondition(node.IfConditionName))
                        : FnCondition("UseCoreServices"),
                });

                // create function log-group subscription
                if(
                    _builder.TryGetItemDeclaration("Module::LoggingStream", out _)
                    && _builder.TryGetItemDeclaration("Module::LoggingStreamRole", out _)
                ) {
                    AddDeclaration(node, new ResourceDeclaration {
                        Resource = Literal("LogGroupSubscription"),
                        Type = Literal("AWS::Logs::SubscriptionFilter"),
                        Properties = new ObjectExpression {
                            ["DestinationArn"] = FnRef("Module::LoggingStream"),
                            ["FilterPattern"] = Literal("-\"*** \""),
                            ["LogGroupName"] = FnRef($"{node.FullName}::LogGroup"),
                            ["RoleArn"] = FnRef("Module::LoggingStreamRole")
                        },
                        If = (node.IfConditionName != null)
                            ? (AExpression)FnAnd(FnCondition("UseCoreServices"), FnCondition(node.IfConditionName))
                            : FnCondition("UseCoreServices"),
                    });
                }
            }

            // local function
            string DetermineProjectFileLocation(string folderPath)
                => new[] {
                    Path.Combine(folderPath, $"{new DirectoryInfo(folderPath).Name}.csproj"),
                    Path.Combine(folderPath, "index.js"),
                    Path.Combine(folderPath, "build.sbt")
                }.FirstOrDefault(projectPath => File.Exists(projectPath));

            void DetermineDotNetFunctionProperties(string project) {

                // set the language
                if(node.Language == null) {
                    node.Language = new LiteralExpression {
                        Value = "csharp"
                    };
                }

                // check if the handler/runtime were provided or if they need to be extracted from the project file
                var csproj = XDocument.Load(node.Project.Value);
                var mainPropertyGroup = csproj.Element("Project")?.Element("PropertyGroup");

                // compile function project
                var projectName = mainPropertyGroup?.Element("AssemblyName")?.Value ?? Path.GetFileNameWithoutExtension(project);

                // check if we need to parse the <TargetFramework> element to determine the lambda runtime
                var targetFramework = mainPropertyGroup?.Element("TargetFramework").Value;
                if(node.Runtime == null) {
                    switch(targetFramework) {
                    case "netcoreapp1.0":
                    case "netcoreapp2.0":
                        _builder.Log(Error.UnsupportedVersionOfDotNetCore, node);
                        break;
                    case "netcoreapp2.1":
                        node.Runtime = new LiteralExpression {
                            Value = "dotnetcore2.1"
                        };
                        break;
                    default:
                        _builder.Log(Error.UnknownVersionOfDotNetCore, node);
                        break;
                    }
                }

                // check if we need to read the project file <RootNamespace> element to determine the handler name
                if(node.Handler == null) {
                    var rootNamespace = mainPropertyGroup?.Element("RootNamespace")?.Value;
                    if(rootNamespace != null) {
                        node.Handler = new LiteralExpression {
                            Value = $"{projectName}::{rootNamespace}.Function::FunctionHandlerAsync"
                        };
                    } else {
                        _builder.Log(Error.FailedToAutoDetectHandlerInDotNetFunctionProject, node);
                    }
                }
            }

            void DetermineJavascriptFunctionProperties() {

                // set the language
                if(node.Language == null) {
                    node.Language = new LiteralExpression {
                        Value = "javascript"
                    };
                }

                // set runtime
                if(node.Runtime == null) {
                    node.Runtime = new LiteralExpression {
                        Value = "nodejs8.10"
                    };
                }

                // set handler
                if(node.Handler == null) {
                    node.Handler = new LiteralExpression {
                        Value = "index.handler"
                    };
                }
            }

            void DetermineScalaFunctionProperties() {

                // set the language
                if(node.Language == null) {
                    node.Language = new LiteralExpression {
                        Value = "scala"
                    };
                }

                // set runtime
                if(node.Runtime == null) {
                    node.Runtime = new LiteralExpression {
                        Value = "java8"
                    };
                }

                // set handler
                if(node.Handler == null) {
                    _builder.Log(Error.HandlerAttributeIsRequiredForScalaFunction, node);
                }
            }

            void SetProperty(string key, AExpression expression) {
                if((expression != null) && !node.Properties.ContainsKey(key)) {
                    node.Properties[key] = expression;
                }
            }
        }

        public override void VisitStart(ASyntaxNode parent, ApiEventSourceDeclaration node) {

            // extract HTTP method from route
            var api = node.Api.Value.Trim();
            var pathSeparatorIndex = api.IndexOfAny(new[] { ':', ' ' });
            if(pathSeparatorIndex >= 0) {

                // extract the API method
                node.ApiMethod = api.Substring(0, pathSeparatorIndex).ToUpperInvariant();
                if(node.ApiMethod == "*") {
                    node.ApiMethod = "ANY";
                }

                // extract the API path
                node.ApiPath = api.Substring(pathSeparatorIndex + 1)
                    .TrimStart()
                    .Split('/', StringSplitOptions.RemoveEmptyEntries);

                // validate API path segments
                if(node.ApiPath.Where(segment =>
                    (segment.StartsWith("{", StringComparison.Ordinal) && !segment.EndsWith("}", StringComparison.Ordinal))
                    || (segment.StartsWith("{", StringComparison.Ordinal) && !segment.EndsWith("}", StringComparison.Ordinal))
                    || (segment == "{}")
                    || (segment == "{+}")
                ).Any()) {
                    _builder.Log(Error.ApiEventSourceInvalidApiFormat, node.Api);
                } else {

                    // check if the API path has a greedy parameter and ensure it is the last parameter
                    var greedyParameter = node.ApiPath.Select((segment, index) => new {
                        Index = index,
                        Segment = segment
                    }).FirstOrDefault(t => t.Segment.EndsWith("+}", StringComparison.Ordinal));
                    if((greedyParameter != null) && (greedyParameter.Index != (node.ApiPath.Length - 1))) {
                        _builder.Log(Error.ApiEventSourceInvalidGreedyParameterMustBeLast(greedyParameter.Segment), node.Api);
                    }
                }
            } else {
                _builder.Log(Error.ApiEventSourceInvalidApiFormat, node);
            }

            // parse integration into a valid enum
            if(!Enum.TryParse<ApiEventSourceDeclaration.IntegrationType>(node.Integration.Value ?? "RequestResponse", ignoreCase: true, out var integration)) {
                _builder.Log(Error.ApiEventSourceUnsupportedIntegrationType, node.Integration);
            }
            node.ApiIntegrationType = integration;
        }

        public override void VisitStart(ASyntaxNode parent, SchedulEventSourceDeclaration node) {

            // TODO: validate 'node.Schedule' is either valid cron or rate expression
        }

        public override void VisitStart(ASyntaxNode parent, S3EventSourceDeclaration node) {

            // validate events
            if(node.Events == null) {
                node.Events = new List<LiteralExpression> {
                    Literal("s3:ObjectCreated:*")
                };
            } else if(!node.Events.Any()) {

                // TODO: consider using ListExpression as type for 'node.Events' so that we can use 'node.Events' to report the position or the error
                _builder.Log(Error.S3EventSourceEventListCannotBeEmpty, node);
            }
            var unrecognizedEvents = node.Events
                .Where(e => !S3EventTypes.Contains(e.Value))
                .OrderBy(e => e.Value)
                .ToList();
            foreach(var unrecognizedEvent in unrecognizedEvents) {
                _builder.Log(Error.S3EventSourceUnrecognizedEventType(unrecognizedEvent.Value), unrecognizedEvent);
            }
        }

        public override void VisitStart(ASyntaxNode parent, SlackCommandEventSourceDeclaration node) {

            // extract the API path
            node.SlackPath = node.SlackCommand.Value
                .Split('/', StringSplitOptions.RemoveEmptyEntries);

            // validate API path segments
            if(node.SlackPath.Where(segment =>
                segment.StartsWith("{", StringComparison.Ordinal)
                || segment.EndsWith("}", StringComparison.Ordinal)
            ).Any()) {
                _builder.Log(Error.SlackCommandEventSourceInvalidRestPath, node.SlackCommand);
            }
        }

        public override void VisitStart(ASyntaxNode parent, TopicEventSourceDeclaration node) {

            // TODO: validate 'node.Filters'; see https://docs.aws.amazon.com/sns/latest/dg/sns-subscription-filter-policies.html

            // key: name of attribute to filter on
            // value: list of possible matches (OR'ed together)
            //  "text": exact match with 'text'
            //  { "anything-but": "text" }
            //  { "anything-but": number }
            //  { "anything-but": [ "text1", "text2", ... ]}
            //  { "anything-but": [ number1, number2, ... ]}
            //  { "prefix": "text" }
            //  { "numeric": [ "=", number ]}
            //  { "numeric": [ "<", number ]}
            //  { "numeric": [ "<=", number ]}
            //  { "numeric": [ ">", number ]}
            //  { "numeric": [ ">=", number ]}
            //  { "numeric": [ ">", number1, "<", number2 ]}
            //  { "numeric": [ ">=", number1, "<", number2 ]}
            //  { "numeric": [ ">", number1, "<=", number2 ]}
            //  { "numeric": [ ">=", number1, "<=", number2 ]}
            //  { "numeric": [ "<", number1, ">", number2 ]}
            //  { "numeric": [ "<=", number1, ">", number2 ]}
            //  { "numeric": [ "<", number1, ">=", number2 ]}
            //  { "numeric": [ "<=", number1, ">=", number2 ]}
            //  { "exists": bool }

            // NOTE:
            //  - The total combination of values must not exceed 150. Calculate the total combination by multiplying the number of values in each array.
            //  - A filter policy can have a maximum of 5 attribute names.
            //  - The maximum size of a policy is 256 KB.
        }

        public override void VisitStart(ASyntaxNode parent, SqsEventSourceDeclaration node) {

            // validate 'BatchSize' for SQS source
            if(node.BatchSize == null) {
                node.BatchSize = Literal(10);
            } else if(node.BatchSize is LiteralExpression batchSizeLiteral) {
                if(!int.TryParse(batchSizeLiteral.Value, out var batchSize) || ((batchSize < 1) || (batchSize > 10))) {
                    _builder.Log(Error.SqsEventSourceInvalidBatchSize, node.BatchSize);
                }
            }
        }

        public override void VisitStart(ASyntaxNode parent, AlexaEventSourceDeclaration node) {

            // nothing to do
        }

        public override void VisitStart(ASyntaxNode parent, DynamoDBEventSourceDeclaration node) {

            // validate 'BatchSize' for DynamoDB stream source
            if(node.BatchSize == null) {
                node.BatchSize = Literal(100);
            } else if(node.BatchSize is LiteralExpression batchSizeLiteral) {
                if(!int.TryParse(batchSizeLiteral.Value, out var batchSize) || ((batchSize < 1) || (batchSize > 1000))) {
                    _builder.Log(Error.DynamoDBEventSourceInvalidBatchSize, node.BatchSize);
                }
            }

            // validate 'StartingPosition' for DynamoDB stream source
            if(node.StartingPosition == null) {
                node.StartingPosition = Literal("LATEST");
            } else if(node.StartingPosition is LiteralExpression startingPositionLiteral) {
                switch(startingPositionLiteral.Value) {
                case "LATEST":
                case "TRIM_HORIZON":

                    // nothing to do
                    break;
                default:
                    _builder.Log(Error.DynamoDBEventSourceInvalidStartingPosition, node.StartingPosition);
                    break;
                }
            }

            // validate 'MaximumBatchingWindowInSeconds' for DynamoDB source
            if(node.MaximumBatchingWindowInSeconds is LiteralExpression maximumBatchingWindowInSecondsLiteral) {
                if(!int.TryParse(maximumBatchingWindowInSecondsLiteral.Value, out var maximumBatchingWindowInSeconds) || ((maximumBatchingWindowInSeconds < 0) || (maximumBatchingWindowInSeconds > 300))) {
                    _builder.Log(Error.DynamoDBEventSourceInvalidMaximumBatchingWindowInSeconds, node.BatchSize);
                }
            }
        }

        public override void VisitStart(ASyntaxNode parent, KinesisEventSourceDeclaration node) {

            // validate 'BatchSize' for Kinesis stream source
            if(node.BatchSize == null) {
                node.BatchSize = Literal(100);
            } else if(node.BatchSize is LiteralExpression batchSizeLiteral) {
                if(!int.TryParse(batchSizeLiteral.Value, out var batchSize) || ((batchSize < 1) || (batchSize > 10000))) {
                    _builder.Log(Error.KinesisEventSourceInvalidBatchSize, node.BatchSize);
                }
            }

            // validate 'StartingPosition' for DynamoDB stream source
            if(node.StartingPosition == null) {
                node.StartingPosition = Literal("LATEST");
            } else if(node.StartingPosition is LiteralExpression startingPositionLiteral) {
                switch(startingPositionLiteral.Value) {
                case "AT_TIMESTAMP":
                case "LATEST":
                case "TRIM_HORIZON":

                    // nothing to do
                    break;
                default:
                    _builder.Log(Error.KinesisEventSourceInvalidStartingPosition, node.StartingPosition);
                    break;
                }
            }

            // validate 'MaximumBatchingWindowInSeconds' for DynamoDB source
            if(node.MaximumBatchingWindowInSeconds is LiteralExpression maximumBatchingWindowInSecondsLiteral) {
                if(!int.TryParse(maximumBatchingWindowInSecondsLiteral.Value, out var maximumBatchingWindowInSeconds) || ((maximumBatchingWindowInSeconds < 0) || (maximumBatchingWindowInSeconds > 300))) {
                    _builder.Log(Error.KinesisEventSourceInvalidMaximumBatchingWindowInSeconds, node.BatchSize);
                }
            }
       }

        public override void VisitStart(ASyntaxNode parent, WebSocketEventSourceDeclaration node) {

            // see https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/aws-resource-apigatewayv2-route.html

            // TODO: ensure 'node.WebSocket' value is unique for every entry

            // validate pre-defined WebSocket routes
            if(node.WebSocket.Value.StartsWith("$", StringComparison.Ordinal)) {
                switch(node.WebSocket.Value) {
                case "$connect":
                case "$disconnect":
                case "$default":

                    // nothing to do
                    break;
                default:
                    _builder.Log(Error.WebSocketEventSourceInvalidPredefinedRoute, node.WebSocket);
                    break;
                }
            }

            // TODO: 'node.ApiKeyRequired' value must a boolean

            // validate 'AuthorizationType' for WebSocket source
            if(node.AuthorizationType != null) {
                switch(node.AuthorizationType.Value) {
                case "AWS_IAM":
                case "CUSTOM":

                    // nothing to do
                    break;
                default:
                    _builder.Log(Error.WebSocketEventSourceInvalidAuthorizationType, node.AuthorizationType);
                    break;
                }
            }

            // validate 'AuthorizerId' for WebSocket source
            if(node.AuthorizerId != null) {

                // 'AuthorizationType' must be CUSTOM in this case
                if(node.AuthorizationType == null) {
                    node.AuthorizationType = Literal("CUSTOM");
                } else if(node.AuthorizationType.Value != "CUSTOM") {
                    _builder.Log(Error.WebSocketEventSourceInvalidAuthorizationTypeForCustomAuthorizer, node.AuthorizationType);
                }
            }

            // ensure that authorization configuration can only be set for the '$connect' route
            if((node.AuthorizationType != null) && (node.WebSocket.Value != "$connect")) {
                _builder.Log(Error.WebSocketEventSourceInvalidAuthorizationConfigurationForRoute, node.AuthorizationType);
            }
        }
    }
}
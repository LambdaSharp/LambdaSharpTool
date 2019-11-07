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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using LambdaSharp.Tool.Compiler.Parser.Syntax;

namespace LambdaSharp.Tool.Compiler.Analyzers {

    public partial class DeclarationsVisitor {

        //--- Methods ---
        public override void VisitStart(ASyntaxNode parent, FunctionDeclaration node) {

            // validate attributes
            ValidateExpressionIsLiteralOrListOfLiteral(node.Scope);
            ValidateExpressionIsNumber(node, node.Memory, $"invalid 'Memory' value");
            ValidateExpressionIsNumber(node, node.Timeout, $"invalid 'Timeout' value");

            // TODO: validate function sources

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
                    _builder.LogError($"missing 'Runtime' attribute", node.SourceLocation);
                }
                if(node.Handler == null) {
                    _builder.LogError($"missing 'Handler' attribute", node.SourceLocation);
                }
                if(node.Language == null) {
                    _builder.LogError($"missing 'Language' attribute", node.SourceLocation);
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
                    _builder.LogError($"function project file could not be found or is not supported", node.SourceLocation);
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
                    _builder.LogError($"unsupported language for Lambda function", node.SourceLocation);
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
                        _builder.LogError($".NET Core 1.0 is no longer supported for Lambda functions", node.SourceLocation);
                        break;
                    case "netcoreapp2.0":
                        _builder.LogError($".NET Core 2.0 is no longer supported for Lambda functions", node.SourceLocation);
                        break;
                    case "netcoreapp2.1":
                        node.Runtime = new LiteralExpression {
                            Value = "dotnetcore2.1"
                        };
                        break;
                    default:
                        _builder.LogError($"could not determine runtime from target framework: {targetFramework}; specify 'Runtime' attribute explicitly", node.SourceLocation);
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
                        _builder.LogError($"could not auto-determine handler; either add 'Handler' attribute or <RootNamespace> to project file", node.SourceLocation);
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
                    _builder.LogError($"Handler attribute is required for Scala functions", node.SourceLocation);
                }
            }

            void SetProperty(string key, AExpression expression) {
                if((expression != null) && !node.Properties.ContainsKey(key)) {
                    node.Properties[key] = expression;
                }
            }
        }
    }
}
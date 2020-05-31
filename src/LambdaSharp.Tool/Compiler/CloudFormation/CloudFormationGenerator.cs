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
using LambdaSharp.Tool.Compiler.Syntax;
using Microsoft.CSharp.RuntimeBinder;

namespace LambdaSharp.Tool.Compiler.CloudFormation {

    public class CloudFormationGenerator {

        //--- Class Methods ---
        public static CloudFormationTemplate Translate(ModuleDeclaration module, Builder builder)
            => new CloudFormationGenerator(builder).Translate(module);

        private static ACloudFormationExpression CreateFunction(string functionName, params ACloudFormationExpression[] parameters)
            => new CloudFormationObjectExpression {
                [functionName ?? throw new ArgumentNullException(nameof(functionName))] = new CloudFormationListExpression(parameters ?? throw new ArgumentNullException(nameof(parameters)))
            };

        //--- Fields ---
        private readonly Builder _builder;

        //--- Constructors ---
        private CloudFormationGenerator(Builder builder) => _builder = builder ?? throw new ArgumentNullException(nameof(builder));

        //--- Methods ---
        private CloudFormationTemplate Translate(ModuleDeclaration module) {
            var template = new CloudFormationTemplate {
                Description = (module.Description != null)
                    ? module.Description.Value.TrimEnd() + $" (v{_builder.ModuleVersion})"
                    : null
            };

            // check if we need to add the SAM transform to the output template
            if(module.HasSamTransform) {
                template.Transforms.Add("AWS::Serverless-2016-10-31");
            }

            // add declarations
            foreach(var declaration in _builder.ItemDeclarations) {
                Translate(template, declaration);

                // check if declaration is public and needs to be exported
                if((declaration is IScopedDeclaration scopedDeclaration) && (scopedDeclaration.IsPublic)) {
                    template.Outputs.Add(declaration.LogicalId,  new CloudFormationOutput {
                        Description = declaration.Description?.Value,
                        Value = Translate(scopedDeclaration.ReferenceExpression ?? throw new NullValueException()),
                        Export = new Dictionary<string, ACloudFormationExpression> {
                            ["Name"] = Translate(Fn.Sub($"${{AWS::StackName}}::{declaration.FullName}"))
                        }
                    });
                }
            }

            // add module manifest
            var manifest = new CloudFormationModuleManifest {
                ModuleInfo = _builder.ModuleInfo,
                Description = module.Description?.Value,
                CoreServicesVersion = _builder.CoreServicesReferenceVersion,
                ParameterSections = _builder.ItemDeclarations.OfType<ParameterDeclaration>()
                    .GroupBy(input => input.Section?.Value ?? "Module Settings")
                    .Where(group => group.Key != "LambdaSharp Deployment Settings (DO NOT MODIFY)")
                    .Select(group => new CloudFormationModuleManifestParameterSection {
                        Title = group.Key,
                        Parameters = group.Select(input => new CloudFormationModuleManifestParameter {
                            Name = input.LogicalId,
                            Type = input.Type?.Value ?? "String",
                            Label = input.Label?.Value,
                            Default = input.Default?.Value,
                            Import = input.Import?.Value,
                            AllowedValues = input.AllowedValues.Any()
                                ? input.AllowedValues.Select(allowedValue => allowedValue.Value).ToList()
                                : null,
                            AllowedPattern = input.AllowedPattern?.Value,
                            ConstraintDescription = input.ConstraintDescription?.Value
                        }).ToList()
                    }).ToList(),
                Artifacts = _builder.Artifacts.ToList(),
                Dependencies = _builder.Dependencies

                    // no need to store LambdaSharp.Core dependency since the manifest already has a CoreServicesVersion property
                    .Where(dependency => dependency.ModuleLocation.ModuleInfo.FullName != "LambdaSharp.Core")
                    .Select(dependency => new CloudFormationModuleManifestDependency {
                        ModuleInfo = dependency.ModuleLocation.ModuleInfo,
                        Type = Enum.Parse<CloudFormationModuleManifestDependencyType>(dependency.Type.ToString())
                    })
                    .OrderBy(dependency => dependency.ModuleInfo?.ToString() ?? throw new ShouldNeverHappenException("missing ModuleInfo"))
                    .ToList(),
                ResourceTypes = _builder.LocalResourceTypes.Values
                    .Select(resourceType => new CloudFormationModuleManifestResourceType {
                        Type = resourceType.Type,
                        Description = resourceType.Description,
                        Properties = resourceType.Properties.Select(property => new CloudFormationModuleManifestResourceProperty {
                            Name = property.Name,
                            Description = property.Description,
                            Type = property.Type,
                            Required = property.Required
                        }).ToList(),
                        Attributes = resourceType.Attributes.Select(attribute => new CloudFormationModuleManifestResourceAttribute {
                            Name = attribute.Name,
                            Description = attribute.Description,
                            Type = attribute.Type
                        }).ToList()
                    }).ToList(),
                Outputs = _builder.ItemDeclarations
                    .OfType<IScopedDeclaration>()
                    .Where(item => item.IsPublic)
                    .Select(item => new CloudFormationModuleManifestOutput {
                        Name = item.FullName,
                        Description = item.Description?.Value,
                        Type = item.Type?.Value ?? "String"
                    })
                    .OrderBy(output => output.Name)
                    .ToList(),
            };
            template.Metadata.Add("LambdaSharp::Manifest", manifest);

            return template;
        }

        #region *** Translate Declarations ***
        private void Translate(CloudFormationTemplate template, AItemDeclaration declaration) {
            try {
                ((dynamic)this).TranslateDeclaration(template, declaration);
            } catch(RuntimeBinderException) {
                throw new ShouldNeverHappenException($"could not handle: {declaration?.GetType().Name ?? "<null>"}");
            }
        }

        private void TranslateDeclaration(CloudFormationTemplate template, ParameterDeclaration declaration)
            => template.Parameters.Add(declaration.LogicalId, new CloudFormationParameter {
                Type = declaration.Type?.Value ?? throw new NullValueException(),
                Description = declaration.Description?.Value,
                AllowedPattern = declaration.AllowedPattern?.Value,
                AllowedValues = declaration.AllowedValues.Any()
                    ? declaration.AllowedValues.Select(allowedValue => allowedValue.Value).ToList()
                    : null,
                ConstraintDescription = declaration.ConstraintDescription?.Value,
                Default = declaration.Default?.Value,
                MinLength = declaration.MinLength?.AsInt(),
                MaxLength = declaration.MaxLength?.AsInt(),
                MinValue = declaration.MinValue?.AsInt(),
                MaxValue = declaration.MaxValue?.AsInt(),
                NoEcho = declaration.NoEcho?.AsBool()
            });

        private void TranslateDeclaration(CloudFormationTemplate template, PseudoParameterDeclaration declaration) { }

        private void TranslateDeclaration(CloudFormationTemplate template, ImportDeclaration declaration) { }

        private void TranslateDeclaration(CloudFormationTemplate template, VariableDeclaration declaration) { }

        private void TranslateDeclaration(CloudFormationTemplate template, GroupDeclaration declaration) { }

        private void TranslateDeclaration(CloudFormationTemplate template, ConditionDeclaration declaration)
            => template.Conditions.Add(declaration.LogicalId, (CloudFormationObjectExpression)Translate(declaration.Value ?? throw new NullValueException()));

        private void TranslateDeclaration(CloudFormationTemplate template, ResourceDeclaration declaration)
            => template.Resources.Add(declaration.LogicalId, new CloudFormationResource {
                Type = declaration.Type?.Value ?? throw new NullValueException(),
                Properties = (CloudFormationObjectExpression)Translate(declaration.Properties),
                DependsOn = declaration.DependsOn.Select(dependOn => dependOn.Value).ToList(),
                Condition = declaration.IfConditionName,

                // TODO (2020-02-12, bjorg): ability to set Metadata and DeletionPolicy
                Metadata = new Dictionary<string, ACloudFormationExpression>(),
                DeletionPolicy = null
            });

        private void TranslateDeclaration(CloudFormationTemplate template, NestedModuleDeclaration declaration)

            // TODO:
            => throw new NotImplementedException();

        private void TranslateDeclaration(CloudFormationTemplate template, PackageDeclaration declaration)

            // TODO:
            => throw new NotImplementedException();

        private void TranslateDeclaration(CloudFormationTemplate template, FunctionDeclaration declaration)

            // TODO:
            => throw new NotImplementedException();

        private void TranslateDeclaration(CloudFormationTemplate template, MappingDeclaration declaration)

            // TODO:
            => throw new NotImplementedException();

        private void TranslateDeclaration(CloudFormationTemplate template, ResourceTypeDeclaration declaration) {

            // TODO: add to meta-data and export resource-type handler
        }

        private void TranslateDeclaration(CloudFormationTemplate template, MacroDeclaration declaration)

            // TODO:
            => throw new NotImplementedException();
        #endregion

        #region *** Translate Expressions ***
        private ACloudFormationExpression Translate(AExpression expression) {
            try {
                return ((dynamic)this).TranslateExpression(expression);
            } catch(RuntimeBinderException) {
                throw new ShouldNeverHappenException($"could not handle: {expression?.GetType().Name ?? "<null>"}");
            }
        }

        private ACloudFormationExpression TranslateExpression(ObjectExpression expression)
            => new CloudFormationObjectExpression(expression.Select(kv => new CloudFormationObjectExpression.KeyValuePair(kv.Key.Value, Translate(kv.Value))));

        private ACloudFormationExpression TranslateExpression(ListExpression expression)
            => new CloudFormationListExpression(expression.Select(value => Translate(value)));

        private ACloudFormationExpression TranslateExpression(LiteralExpression expression) {
            switch(expression.Type) {
                case LiteralType.String:
                    return new CloudFormationLiteralExpression(expression.Value);
                case LiteralType.Bool:
                case LiteralType.Float:
                case LiteralType.Integer:
                case LiteralType.Null:
                case LiteralType.Timestamp:

                    // TODO:
                    throw new NotImplementedException();
                default:
                    throw new ShouldNeverHappenException($"unrecognized literal type: {expression.Type}");
            }
        }

        private ACloudFormationExpression TranslateExpression(ConditionExpression expression)
            => CreateFunction("Fn::Condition", Translate(expression.ReferenceName));

        private ACloudFormationExpression TranslateExpression(EqualsConditionExpression expression)
            => CreateFunction("Fn::Equals", Translate(expression.LeftValue), Translate(expression.RightValue));

        private ACloudFormationExpression TranslateExpression(NotConditionExpression expression)
            => CreateFunction("Fn::Not", Translate(expression.Value));

        private ACloudFormationExpression TranslateExpression(AndConditionExpression expression)
            => CreateFunction("Fn::And", Translate(expression.LeftValue), Translate(expression.RightValue));

        private ACloudFormationExpression TranslateExpression(OrConditionExpression expression)
            => CreateFunction("Fn::Or", Translate(expression.LeftValue), Translate(expression.RightValue));

        private ACloudFormationExpression TranslateExpression(Base64FunctionExpression expression)
            => CreateFunction("Fn::Base64", Translate(expression.Value));

        private ACloudFormationExpression TranslateExpression(CidrFunctionExpression expression)
            => CreateFunction("Fn::Cidr", Translate(expression.IpBlock), Translate(expression.Count), Translate(expression.CidrBits));

        private ACloudFormationExpression TranslateExpression(FindInMapFunctionExpression expression)
            => CreateFunction("Fn::FindInMap", Translate(expression.MapName), Translate(expression.TopLevelKey), Translate(expression.SecondLevelKey));

        private ACloudFormationExpression TranslateExpression(GetAttFunctionExpression expression)
            => CreateFunction("Fn::GetAtt", Translate(expression.ReferenceName), Translate(expression.AttributeName));

        private ACloudFormationExpression TranslateExpression(GetAZsFunctionExpression expression)
            => CreateFunction("Fn::GetAZs", Translate(expression.Region));

        private ACloudFormationExpression TranslateExpression(IfFunctionExpression expression)
            => CreateFunction("Fn::If", Translate(expression.Condition), Translate(expression.IfTrue), Translate(expression.IfFalse));

        private ACloudFormationExpression TranslateExpression(ImportValueFunctionExpression expression)
            => CreateFunction("Fn::ImportValue", Translate(expression.SharedValueToImport));

        private ACloudFormationExpression TranslateExpression(JoinFunctionExpression expression)
            => CreateFunction("Fn::Join", Translate(expression.Delimiter), Translate(expression.Values));

        private ACloudFormationExpression TranslateExpression(SelectFunctionExpression expression)
            => CreateFunction("Fn::Select", Translate(expression.Index), Translate(expression.Values));

        private ACloudFormationExpression TranslateExpression(SplitFunctionExpression expression)
            => CreateFunction("Fn::Split", Translate(expression.Delimiter), Translate(expression.SourceString));

        private ACloudFormationExpression TranslateExpression(SubFunctionExpression expression)
            => expression.Parameters.Any()
                ? CreateFunction("Fn::Sub", Translate(expression.FormatString), Translate(expression.Parameters))
                : CreateFunction("Fn::Sub", Translate(expression.FormatString));

        private ACloudFormationExpression TranslateExpression(TransformFunctionExpression expression) {
            var result = new CloudFormationObjectExpression {
                ["Fn::Transform"] = new CloudFormationObjectExpression {
                    ["Name"] = Translate(expression.MacroName)
                }
            };
            if(expression.Parameters?.Any() ?? false) {
                result["Parameters"] = Translate(expression.Parameters);
            }
            return result;
        }

        private ACloudFormationExpression TranslateExpression(ReferenceFunctionExpression expression)
            => CreateFunction("Ref", Translate(expression.ReferenceName));
        #endregion
    }
}
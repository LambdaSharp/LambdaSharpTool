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
using System.Linq;
using LambdaSharp.Tool.Compiler.Syntax;
using Microsoft.CSharp.RuntimeBinder;

namespace LambdaSharp.Tool.Compiler.CloudFormation {

    public class CloudFormationGenerator {

        //--- Class Methods ---
        public static CloudFormationTemplate Translate(Builder builder)
            => throw new NotImplementedException();

        private static ACloudFormationExpression CreateFunction(string functionName, params ACloudFormationExpression[] parameters)
            => new CloudFormationObjectExpression {
                [functionName ?? throw new ArgumentNullException(nameof(functionName))] = new CloudFormationListExpression(parameters ?? throw new ArgumentNullException(nameof(parameters)))
            };

        //--- Fields ---
        private readonly Builder _builder;

        //--- Constructors ---
        private CloudFormationGenerator(Builder builder) => _builder = builder ?? throw new ArgumentNullException(nameof(builder));

        //--- Methods ---

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

        private void TranslateDeclaration(CloudFormationTemplate template, VariableDeclaration declaration) {

            // TODO: check for export scope
        }

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
                Metadata = new System.Collections.Generic.Dictionary<string, CloudFormationObjectExpression>(),
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
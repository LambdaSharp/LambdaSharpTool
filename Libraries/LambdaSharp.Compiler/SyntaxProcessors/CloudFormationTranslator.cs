/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2021
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


// TODO: enable nullable
#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using LambdaSharp.CloudFormation.ModuleManifest;
using LambdaSharp.CloudFormation.Template;
using LambdaSharp.Compiler.Syntax.Declarations;

namespace LambdaSharp.Compiler.SyntaxProcessors {
    using Fn = CloudFormationFunction;

    internal sealed class CloudFormationTranslator : ASyntaxProcessor {

        //--- Constructors ---
        public CloudFormationTranslator(ISyntaxProcessorDependencyProvider provider) : base(provider) { }

        //--- Methods ---
        public CloudFormationTemplate Process(ModuleDeclaration moduleDeclaration) {
            var template = new CloudFormationTemplate();
            var metadata = new CloudFormationModuleManifest();
            var referenceSubstitutions = new Dictionary<string, ACloudFormationExpression>();

            // TODO: set template information from module

            // translate declarations
            moduleDeclaration.Inspect(node => {
                switch(node) {
                case ImportDeclaration importDeclaration:
                    TranslateImportDeclaration(importDeclaration);
                    break;
                default:

                    // TODO: add missing declarations
                    throw new NotImplementedException();
                }
            });

            // TODO: translate expressions
            throw new NotImplementedException();

            // return template;

            // local function
            void TranslateImportDeclaration(ImportDeclaration node) {

                // generate parameter name for import declaration
                node.GetModuleAndExportName(out var moduleReference, out var exportName);
                var importParameterName = ToIdentifier(moduleReference) + ToIdentifier(exportName);
                var parameter = new CloudFormationParameter {
                    Type = TranslateToParameterType(node.Type.Value),
                    Description = $"Cross-module reference for {moduleReference}::{exportName}",
                    AllowedPattern = "^.+$",
                    ConstraintDescription = "must either be a cross-module reference or a non-empty value",
                    Default = $"${moduleReference.Replace(".", "-")}::{exportName}"
                };

                // check if a comparable import already exists or if one needs to be added
                if(template.Parameters.TryGetValue(importParameterName, out var existingParameter)) {

                    // NOTE (2020-02-27, bjorg): if an import declaration already exists for this value, it must be identical in every way; such duplicates
                    //  are allowed, because it is not possible to know ahead of time if a value was already imported
                    if(existingParameter.Default != parameter.Default) {
                        Logger.Log(Error.ImportDuplicateWithDifferentBinding(importParameterName), node);
                    } else if(existingParameter.Type != parameter.Type) {
                        Logger.Log(Error.ImportDuplicateWithDifferentType(importParameterName), node);
                    }
                } else {

                    // add parameter description to metadate section
                    var section = MetadataSection($"{moduleReference} Imports");
                    section.Parameters.Add(new CloudFormationModuleManifestParameter {
                        Name = importParameterName,
                        Type = node.Type.Value,
                        AllowedPattern = parameter.AllowedPattern,
                        ConstraintDescription = parameter.ConstraintDescription,
                        AllowedValues = parameter.AllowedValues,
                        Default = parameter.Default,
                        Import = $"{moduleReference}::{exportName}",
                        Label = exportName
                    });

                    // generate Condition for import
                    var conditionName = node.LogicalId + "IsImported";
                    template.Conditions.Add(conditionName, Fn.And(
                        Fn.Not(Fn.Equals(Fn.Ref(importParameterName), Fn.Literal(""))),
                        Fn.Equals(Fn.Select(0, Fn.Split("$", Fn.Ref(importParameterName))), Fn.Literal(""))
                    ));

                    // create reference substitutions
                    referenceSubstitutions.Add(node.FullName, Fn.If(
                        conditionName,
                        Fn.ImportValue(Fn.Sub("${DeploymentPrefix}${Import}", new CloudFormationObject {
                            ["Import"] = Fn.Select(1, Fn.Split("$", Fn.Ref(node.FullName)))
                        })),
                        Fn.Ref(node.FullName)
                    ));
                }
            }

            string TranslateToParameterType(string typeName) {

                // TODO:
                return "String";
            }

            CloudFormationModuleManifestParameterSection MetadataSection(string title) {

                // check if section already exists
                var section = metadata.ParameterSections.FirstOrDefault(section => section.Title == title);
                if(section == null) {

                    // add missing section
                    section = new CloudFormationModuleManifestParameterSection {
                        Title = title
                    };
                    metadata.ParameterSections.Add(section);
                }
                return section;
            }

            string ToIdentifier(string text) => new string(text.Where(char.IsLetterOrDigit).ToArray());
        }
    }
}
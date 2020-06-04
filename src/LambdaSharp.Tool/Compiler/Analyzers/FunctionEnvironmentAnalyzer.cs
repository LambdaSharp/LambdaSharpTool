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

using System.Linq;
using LambdaSharp.Compiler;
using LambdaSharp.Compiler.Syntax;

namespace LambdaSharp.Tool.Compiler.Analyzers {

    public class FunctionEnvironmentAnalyzer : ASyntaxAnalyzer {

        //--- Fields ---
        private readonly Builder _builder;

        //--- Constructors ---
        public FunctionEnvironmentAnalyzer(Builder builder) => _builder = builder ?? throw new System.ArgumentNullException(nameof(builder));

        //--- Methods ---
        public override bool VisitStart(FunctionDeclaration node) {
            var environment = node.Properties.GetOrCreate<ObjectExpression>("Environment", expression => _builder.Log(Error.FunctionPropertiesEnvironmentMustBeMap, expression));
            var variables = environment.GetOrCreate<ObjectExpression>("Variables", expression => _builder.Log(Error.FunctionPropertiesEnvironmentVariablesMustBeMap, expression));
            if(variables == null) {
                return true;
            }

            // set default environment variables
            variables["MODULE_ID"] = Fn.Ref("AWS::StackName");
            variables["MODULE_INFO"] = Fn.Literal(_builder.ModuleInfo.ToString());
            variables["LAMBDA_NAME"] = Fn.Literal(node.FullName);
            variables["LAMBDA_RUNTIME"] = Fn.Literal(node.Runtime.Value);
            variables["DEPLOYMENT_TIER"] = Fn.Select("0", Fn.Split("-", Fn.Ref("DeploymentPrefix")));
            variables["DEPLOYMENTBUCKETNAME"] = Fn.Ref("DeploymentBucketName");
            if(node.HasDeadLetterQueue && _builder.TryGetItemDeclaration("Module::DeadLetterQueue", out var _))  {
                variables["DEADLETTERQUEUE"] = Fn.Ref("Module::DeadLetterQueue");
            }

            // find all declarations scoped to this function; including wildcards when allowed
            var scopedDeclarations = _builder.ItemDeclarations
                .OfType<IScopedDeclaration>()
                .Where(declaration => declaration.ScopeValues.Any(scope =>
                    (scope == node.FullName)
                    || (
                        node.HasWildcardScopedVariables
                        && ((scope == "*") || (scope == "all"))
                    )
                )).ToList();

            // add all declarations scoped to this function
            foreach(var scopeDeclaration in scopedDeclarations) {
                var prefix = scopeDeclaration.HasSecretType ? "SEC_" : "STR_";
                var fullEnvName = prefix + scopeDeclaration.FullName.Replace("::", "_").ToUpperInvariant();

                // check if declaration has a condition associated with it
                variables[fullEnvName] = ((scopeDeclaration is IConditionalResourceDeclaration conditionalDeclaration) && (conditionalDeclaration.If != null))
                    ? Fn.If(conditionalDeclaration.IfConditionName, scopeDeclaration.ReferenceExpression, Fn.Ref("AWS::NoValue"))
                    : scopeDeclaration.ReferenceExpression;
            }

            // add all explicitly listed environment variables
            foreach(var kv in node.Environment) {

                // add explicit environment variable as string value
                var fullEnvName = "STR_" + kv.Key.Value.Replace("::", "_").ToUpperInvariant();
                variables[fullEnvName] = kv.Value;
            }
            return true;
        }
    }
}

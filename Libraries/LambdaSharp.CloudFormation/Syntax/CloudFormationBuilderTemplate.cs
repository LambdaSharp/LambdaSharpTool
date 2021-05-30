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

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using LambdaSharp.CloudFormation.Syntax.Declarations;
using LambdaSharp.CloudFormation.Syntax.Expressions;

namespace LambdaSharp.CloudFormation.Syntax {

    [AttributeUsage(AttributeTargets.Property)]
    internal class InspectAttribute : Attribute { }

    public class CloudFormationSyntaxTemplate : ACloudFormationSyntaxNode {

        // TODO
        // public Dictionary<string, ACloudFormationExpression> Metadata { get; set; } = new Dictionary<string, ACloudFormationExpression>();

        //--- Fields ---
        private CloudFormationSyntaxLiteral? _version;
        private CloudFormationSyntaxLiteral? _description;
        private CloudFormationSyntaxList<CloudFormationSyntaxLiteral>? _transforms;
        private CloudFormationSyntaxList<CloudFormationSyntaxParameter>? _parameters;
        private CloudFormationSyntaxList<CloudFormationSyntaxMapping>? _mappings;
        private CloudFormationSyntaxList<CloudFormationSyntaxCondition>? _conditions;
        private CloudFormationSyntaxList<CloudFormationSyntaxResource>? _resources;
        private CloudFormationSyntaxList<CloudFormationSyntaxOutput>? _outputs;

        //--- Constructors ---
        public CloudFormationSyntaxTemplate([CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) : base(filePath, lineNumber) { }

        //--- Properties ---

        [Inspect]
        public CloudFormationSyntaxLiteral? AWSTemplateFormatVersion {
            get => _version;
            set => _version = Adopt(value);
        }

        [Inspect]
        public CloudFormationSyntaxLiteral? Description {
            get => _description;
            set => _description = Adopt(value);
        }

        [Inspect]
        public CloudFormationSyntaxList<CloudFormationSyntaxLiteral>? Transforms {
            get => _transforms;
            set => _transforms = Adopt(value);
        }

        [Inspect]
        public CloudFormationSyntaxList<CloudFormationSyntaxParameter>? Parameters {
            get => _parameters;
            set => _parameters = Adopt(value);
        }

        [Inspect]
        public CloudFormationSyntaxList<CloudFormationSyntaxMapping>? Mappings {
            get => _mappings;
            set => _mappings = Adopt(value);
        }

        [Inspect]
        public CloudFormationSyntaxList<CloudFormationSyntaxCondition>? Conditions {
            get => _conditions;
            set => _conditions = Adopt(value);
        }

        [Inspect]
        public CloudFormationSyntaxList<CloudFormationSyntaxResource>? Resources {
            get => _resources;
            set => _resources = Adopt(value);
        }

        [Inspect]
        public CloudFormationSyntaxList<CloudFormationSyntaxOutput>? Outputs {
            get => _outputs;
            set => _outputs = Adopt(value);
        }

        public override ACloudFormationSyntaxNode CloneNode() => new CloudFormationSyntaxTemplate {
            SourceLocation = SourceLocation,
            AWSTemplateFormatVersion = AWSTemplateFormatVersion,
            Description = Description,
            Transforms = Transforms,
            Parameters = Parameters,
            Mappings = Mappings,
            Conditions = Conditions,
            Resources = Resources,
            Outputs = Outputs
        };
    }
}
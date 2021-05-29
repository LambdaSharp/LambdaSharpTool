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
using LambdaSharp.CloudFormation.Builder.Declarations;
using LambdaSharp.CloudFormation.Builder.Expressions;

namespace LambdaSharp.CloudFormation.Builder {

    [AttributeUsage(AttributeTargets.Property)]
    internal class InspectAttribute : Attribute { }

    public class CloudFormationBuilderTemplate : ACloudFormationBuilderNode {

        // TODO
        // public Dictionary<string, ACloudFormationExpression> Metadata { get; set; } = new Dictionary<string, ACloudFormationExpression>();

        //--- Fields ---
        private CloudFormationBuilderLiteral? _version;
        private CloudFormationBuilderLiteral? _description;
        private CloudFormationBuilderList<CloudFormationBuilderLiteral>? _transforms;
        private CloudFormationBuilderList<CloudFormationBuilderParameter>? _parameters;
        private CloudFormationBuilderList<CloudFormationBuilderMapping>? _mappings;
        private CloudFormationBuilderList<CloudFormationBuilderCondition>? _conditions;
        private CloudFormationBuilderList<CloudFormationBuilderResource>? _resources;
        private CloudFormationBuilderList<CloudFormationBuilderOutput>? _outputs;

        //--- Properties ---

        [Inspect]
        public CloudFormationBuilderLiteral? AWSTemplateFormatVersion {
            get => _version;
            set => _version = Adopt(value);
        }

        [Inspect]
        public CloudFormationBuilderLiteral? Description {
            get => _description;
            set => _description = Adopt(value);
        }

        [Inspect]
        public CloudFormationBuilderList<CloudFormationBuilderLiteral>? Transforms {
            get => _transforms;
            set => _transforms = Adopt(value);
        }

        [Inspect]
        public CloudFormationBuilderList<CloudFormationBuilderParameter>? Parameters {
            get => _parameters;
            set => _parameters = Adopt(value);
        }

        [Inspect]
        public CloudFormationBuilderList<CloudFormationBuilderMapping>? Mappings {
            get => _mappings;
            set => _mappings = Adopt(value);
        }

        [Inspect]
        public CloudFormationBuilderList<CloudFormationBuilderCondition>? Conditions {
            get => _conditions;
            set => _conditions = Adopt(value);
        }

        [Inspect]
        public CloudFormationBuilderList<CloudFormationBuilderResource>? Resources {
            get => _resources;
            set => _resources = Adopt(value);
        }

        [Inspect]
        public CloudFormationBuilderList<CloudFormationBuilderOutput>? Outputs {
            get => _outputs;
            set => _outputs = Adopt(value);
        }

        public override ACloudFormationBuilderNode CloneNode() => new CloudFormationBuilderTemplate {
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
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

using LambdaSharp.CloudFormation.Builder.Expressions;

namespace LambdaSharp.CloudFormation.Builder.Declarations {

    public class CloudFormationBuilderResource : ACloudFormationBuilderDeclaration {

        //--- Fields ---
        private CloudFormationBuilderLiteral? _type;
        private CloudFormationBuilderLiteral? _condition;
        private CloudFormationBuilderMap? _properties;
        private CloudFormationBuilderList? _dependsOn;
        private CloudFormationBuilderLiteral? _deletionPolicy;

        // TODO: Metadata: https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/aws-attribute-metadata.html
        // TODO: CreationPolicy: https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/aws-attribute-creationpolicy.html
        // TODO: UpdatePolicy: https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/aws-attribute-updatepolicy.html
        // TODO: UpdateReplacePolicy: https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/aws-attribute-updatereplacepolicy.html

        //--- Constructors ---
        public CloudFormationBuilderResource(CloudFormationBuilderLiteral logicalId) : base(logicalId) { }

        //--- Properties ---

        [Inspect]
        public CloudFormationBuilderLiteral? Condition {
            get => _condition;
            set => _condition = Adopt(value);
        }

        [Inspect]
        public CloudFormationBuilderLiteral? Type {
            get => _type;
            set => _type = Adopt(value);
        }

        [Inspect]
        public CloudFormationBuilderMap? Properties {
            get => _properties;
            set => _properties = Adopt(value);
        }

        [Inspect]
        public CloudFormationBuilderList? DependsOn {
            get => _dependsOn;
            set => _dependsOn = Adopt(value);
        }

        [Inspect]
        public CloudFormationBuilderLiteral? DeletionPolicy {
            get => _deletionPolicy;
            set => _deletionPolicy = Adopt(value);
        }

        //--- Methods ---
        public override ACloudFormationBuilderNode CloneNode() => new CloudFormationBuilderResource(LogicalId) {
            SourceLocation = SourceLocation,
            Condition = Condition,
            Type = Type,
            Properties = Properties,
            DependsOn = DependsOn,
            DeletionPolicy = DeletionPolicy
        };
    }
}
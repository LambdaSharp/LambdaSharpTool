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

using LambdaSharp.CloudFormation.Syntax.Expressions;

namespace LambdaSharp.CloudFormation.Syntax.Declarations {

    public class CloudFormationSyntaxOutputExport : ACloudFormationSyntaxNode {

        //--- Fields ---
        private ACloudFormationSyntaxExpression? _name;

        //--- Properties ---

        [Inspect]
        public ACloudFormationSyntaxExpression? Name {
            get => _name;
            set => _name = Adopt(value);
        }

        //--- Methods ---
        public override ACloudFormationSyntaxNode CloneNode() => new CloudFormationSyntaxOutputExport {
            SourceLocation = SourceLocation,
            Name = Name
        };
    }

    public class CloudFormationSyntaxOutput : ACloudFormationSyntaxDeclaration {

        //--- Fields ---
        private CloudFormationSyntaxLiteral? _description;
        private ACloudFormationSyntaxExpression? _value;
        private CloudFormationSyntaxLiteral? _condition;
        private CloudFormationSyntaxOutputExport? _export;

        //--- Constructors ---
        public CloudFormationSyntaxOutput(CloudFormationSyntaxLiteral logicalId) : base(logicalId) { }

        //--- Properties ---

        [Inspect]
        public CloudFormationSyntaxLiteral? Description {
            get => _description;
            set => _description = Adopt(value);
        }

        [Inspect]
        public ACloudFormationSyntaxExpression? Value {
            get => _value;
            set => _value = Adopt(value);
        }

        [Inspect]
        public CloudFormationSyntaxLiteral? Condition {
            get => _condition;
            set => _condition = Adopt(value);
        }

        [Inspect]
        public CloudFormationSyntaxOutputExport? Export {
            get => _export;
            set => _export = Adopt(value);
        }

        //--- Methods ---
        public override ACloudFormationSyntaxNode CloneNode() => new CloudFormationSyntaxOutput(LogicalId) {
            SourceLocation = SourceLocation,
            Description = Description,
            Value = Value,
            Condition = Condition,
            Export = Export
        };
    }
}
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

using System.Diagnostics;
using LambdaSharp.CloudFormation.Syntax.Expressions;

namespace LambdaSharp.CloudFormation.Syntax.Declarations {

    [DebuggerDisplay("LogicalID = {LogicalId.Value}")]
    public abstract class ACloudFormationSyntaxDeclaration : ACloudFormationSyntaxNode {

        //--- Constructors ---
        protected ACloudFormationSyntaxDeclaration(CloudFormationSyntaxLiteral logicalId, string filePath, int lineNumber = 0)
            : base(filePath, lineNumber)
        {
            LogicalId = Adopt(logicalId ?? throw new System.ArgumentNullException(nameof(logicalId)));
        }

        //--- Properties ---

        [Inspect]
        public CloudFormationSyntaxLiteral LogicalId { get; }
    }
}
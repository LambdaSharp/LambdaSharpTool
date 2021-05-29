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

    public class NullValueException : Exception {

        //--- Constructors ---
        public NullValueException() { }
        public NullValueException(string message) : base(message) { }
    }

    [AttributeUsage(AttributeTargets.Property)]
    internal class InspectAttribute : Attribute { }

    public class CloudFormationBuilderTemplate : ACloudFormationBuilderNode {

        //--- Properties ---

        // TODO: should probably be the literal type
        public string? AWSTemplateFormatVersion { get; set; }
        public string? Description { get; set; }
        public List<CloudFormationBuilderLiteral> Transforms { get; set; } = new List<CloudFormationBuilderLiteral>();
        public List<CloudFormationBuilderParameter> Parameters { get; set; } = new List<CloudFormationBuilderParameter>();
        public List<CloudFormationBuilderMapping> Mappings { get; set; } = new List<CloudFormationBuilderMapping>();
        public List<CloudFormationBuilderCondition> Conditions { get; set; } = new List<CloudFormationBuilderCondition>();
        public List<CloudFormationBuilderResource> Resources { get; set; } = new List<CloudFormationBuilderResource>();
        public List<CloudFormationBuilderOutput> Outputs { get; set; } = new List<CloudFormationBuilderOutput>();

        // TODO
        // public Dictionary<string, ACloudFormationExpression> Metadata { get; set; } = new Dictionary<string, ACloudFormationExpression>();
    }
}
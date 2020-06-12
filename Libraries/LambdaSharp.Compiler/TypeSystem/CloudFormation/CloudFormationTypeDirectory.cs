/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2020
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
using System.Diagnostics.CodeAnalysis;
using LambdaSharp.CloudFormation.Specification;

namespace LambdaSharp.Compiler.TypeSystem.CloudFormation {

    public sealed class CloudFormationTypeDirectory : ITypeDirectory {

        //--- Fields ---
        private readonly ExtendedCloudFormationSpecification _specification;

        //--- Constructors ---
        public CloudFormationTypeDirectory(ExtendedCloudFormationSpecification specification) {
            _specification = specification;
        }

        //--- Methods ---
        public bool TryGetResourceType(string resourceTypeName, [NotNullWhen(true)] out IResourceType? resourceType) {

            // check for 'Custom::' type-name prefix
            if(resourceTypeName.StartsWith("Custom::", StringComparison.Ordinal)) {
                resourceType = AnyResourceType.Instance;
                return true;
            }

            // check CloudFormation specification for matching resource type
            if(_specification.ResourceTypes.TryGetValue(resourceTypeName, out var type)) {
                resourceType = new CloudFormationResourceType(resourceTypeName, type, _specification);
                return true;
            }
            resourceType = null;
            return false;
        }
    }
}
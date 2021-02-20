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
using LambdaSharp.CloudFormation.Template;

namespace LambdaSharp.CloudFormation.ModuleManifest {

    public class CloudFormationModuleManifest : ACloudFormationExpression {

        //--- Constants ---
        public const string CurrentVersion = "2019-07-04";

        //--- Properties ---
        public string Version { get; set; } = CurrentVersion;
        public string? Module { get; set; }
        public string? Description { get; set; }
        public string? TemplateChecksum { get; set; }
        public DateTime Date { get; set; }
        public string? CoreServicesVersion { get; set; }
        public List<CloudFormationModuleManifestParameterSection> ParameterSections { get; set; } = new List<CloudFormationModuleManifestParameterSection>();
        public CloudFormationModuleManifestGitInfo? Git { get; set; }
        public List<string> Artifacts { get; set; } = new List<string>();
        public List<CloudFormationModuleManifestDependency> Dependencies { get; set; } = new List<CloudFormationModuleManifestDependency>();
        public List<CloudFormationModuleManifestResourceType> ResourceTypes { get; set; } = new List<CloudFormationModuleManifestResourceType>();
        public List<CloudFormationModuleManifestOutput> Outputs { get; set; } = new List<CloudFormationModuleManifestOutput>();
    }
}
/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2022
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

using System.Collections.Generic;
using LambdaSharp.CloudFormation.Template;

namespace LambdaSharp.Modules.Metadata {

    public class ModuleNameMappings : ACloudFormationExpression {

        //--- Constants ---
        public const string MetadataName = "LambdaSharp::NameMappings";
        public const string CurrentVersion = "2019-07-04";

        //--- Properties ---
        public string Version { get; set; } = CurrentVersion;
        public IDictionary<string, string> ResourceNameMappings { get; set; } = new Dictionary<string, string>();
        public IDictionary<string, string> TypeNameMappings { get; set; } = new Dictionary<string, string>();
    }
}
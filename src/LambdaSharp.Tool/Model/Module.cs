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

using System;
using System.Collections.Generic;
using System.Linq;
using LambdaSharp.Tool.Internal;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace LambdaSharp.Tool.Model {

    public class Module {

        //--- Properties ---
        public string Namespace { get; set; }
        public string Name { get; set; }
        public VersionInfo Version { get; set; }
        public string Description { get; set; }
        public IEnumerable<object> Pragmas { get; set; }
        public IEnumerable<object> Secrets { get; set; }
        public IEnumerable<AModuleItem> Items { get; set; }
        public IEnumerable<string> Artifacts { get; set; }
        public IEnumerable<KeyValuePair<string, ModuleBuilderDependency>> Dependencies { get; set; }
        public IEnumerable<ModuleManifestResourceType> CustomResourceTypes { get; set; }
        public IEnumerable<string> MacroNames { get; set; }
        public IDictionary<string, string> ResourceTypeNameMappings { get; set; }

        //--- Properties ---
        public ModuleInfo ModuleInfo => new ModuleInfo(Namespace, Name, Version, origin: null);

        //--- Methods ---
        public bool HasPragma(string pragma) => Pragmas?.Contains(pragma) == true;
        public bool HasSamTransform => HasPragma("sam-transform");
    }
}
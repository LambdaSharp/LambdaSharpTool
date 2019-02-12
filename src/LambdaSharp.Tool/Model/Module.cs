/*
 * MindTouch λ#
 * Copyright (C) 2018-2019 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit mindtouch.com;
 * please review the licensing section.
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
        public string Owner { get; set; }
        public string Name { get; set; }
        public VersionInfo Version { get; set; }
        public string Description { get; set; }
        public IEnumerable<object> Pragmas { get; set; }
        public IEnumerable<object> Secrets { get; set; }
        public IEnumerable<AModuleItem> Items { get; set; }
        public IEnumerable<string> Assets { get; set; }
        public IEnumerable<KeyValuePair<string, ModuleDependency>> Dependencies { get; set; }
        public IEnumerable<ModuleManifestResourceType> CustomResourceTypes { get; set; }
        public IEnumerable<string> MacroNames { get; set; }
        public IDictionary<string, string> ResourceTypeNameMappings { get; set; }

        //--- Properties ---
        public string FullName => Owner + "." + Name;
        public string Info => FullName + ":" + Version;

        //--- Methods ---
        public bool HasPragma(string pragma) => Pragmas?.Contains(pragma) == true;
        public bool HasRuntimeCheck => !HasPragma("no-core-version-check");
    }
}
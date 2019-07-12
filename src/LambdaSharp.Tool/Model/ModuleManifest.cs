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
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LambdaSharp.Tool.Model {

    public class ModuleManifest {

        //--- Constants ---
        public const string CurrentVersion = "2019-07-04";

        //--- Properties ---
        public string Version { get; set; } = CurrentVersion;
        public ModuleInfo Module { get; set; }
        public string Description { get; set; }
        public string TemplateChecksum { get; set; }
        public VersionInfo CoreServicesVersion { get; set; }
        public IList<ModuleManifestParameterSection> ParameterSections { get; set; } = new List<ModuleManifestParameterSection>();
        public ModuleManifestGitInfo Git { get; set; }
        public IList<string> Assets { get; set; } = new List<string>();
        public IList<ModuleManifestDependency> Dependencies { get; set; } = new List<ModuleManifestDependency>();
        public IList<ModuleManifestResourceType> ResourceTypes { get; set; } = new List<ModuleManifestResourceType>();
        public IList<ModuleManifestOutput> Outputs { get; set; } = new List<ModuleManifestOutput>();
        public IDictionary<string, string> ResourceNameMappings { get; set; } = new Dictionary<string, string>();
        public IDictionary<string, string> TypeNameMappings { get; set; } = new Dictionary<string, string>();

        //--- Methods ---
        public string GetModuleTemplatePath() => Module.GetAssetPath($"cloudformation_{Module.FullName}_{TemplateChecksum}.json");
        public string GetFullName() => Module.FullName;
        public string GetOwner() => Module.Owner;
        public string GetName() => Module.Name;
        public VersionInfo GetVersion() => Module.Version;

        public IEnumerable<ModuleManifestParameter> GetAllParameters()
            => ParameterSections.SelectMany(section => section.Parameters);
    }

    public class ModuleManifestGitInfo {

        //--- Properties ---
        public string Branch { get; set; }
        public string SHA { get; set; }
    }

    public class ModuleManifestResourceType {

       //--- Properties ---
       public string Type { get; set; }
       public string Description { get; set; }
       public IEnumerable<ModuleManifestResourceProperty> Properties { get; set; } = new List<ModuleManifestResourceProperty>();
       public IEnumerable<ModuleManifestResourceProperty> Attributes { get; set; } = new List<ModuleManifestResourceProperty>();
    }

    public class ModuleManifestResourceProperty {

       //--- Properties ---
       public string Name { get; set; }
       public string Description { get; set; }
       public string Type { get; set; } = "String";
       public bool Required { get; set; } = true;
    }

    public class ModuleManifestOutput {

        //--- Properties ---
        public string Name { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
    }

    public class ModuleManifestMacro {

        //--- Properties ---
        public string Name { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ModuleManifestDependencyType {
        Unknown,
        Nested,
        Shared
    }

    // TODO: change to `ModuleManifest.Dependency`
    public class ModuleManifestDependency {

        //--- Properties ---
        public ModuleInfo ModuleInfo { get; set; }
        public ModuleManifestDependencyType Type { get; set; }
    }

    public class ModuleManifestParameterSection {

        //--- Properties ---
        public string Title { get; set; }
        public IList<ModuleManifestParameter> Parameters { get; set; } = new List<ModuleManifestParameter>();
    }

    public class ModuleManifestParameter {

        //--- Properties ---
        public string Name { get; set; }
        public string Type { get; set; }
        public string Label { get; set; }
        public string Default { get; set; }
        public string Import { get; set; }
        public List<string> AllowedValues { get; set; }
        public string AllowedPattern { get; set; }
        public string ConstraintDescription { get; set; }
    }
}
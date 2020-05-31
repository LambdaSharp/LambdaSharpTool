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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LambdaSharp.Tool.Compiler.CloudFormation {

    public class CloudFormationTemplate {

        //--- Properties ---
        public string AWSTemplateFormatVersion => "2010-09-09";
        public string? Description { get; set; }
        public List<string> Transforms { get; set; } = new List<string>();
        public Dictionary<string, CloudFormationParameter> Parameters { get; set; } = new Dictionary<string, CloudFormationParameter>();
        public Dictionary<string, Dictionary<string, string>> Mappings { get; set; } = new Dictionary<string, Dictionary<string, string>>();
        public Dictionary<string, CloudFormationObjectExpression> Conditions { get; set; } = new Dictionary<string, CloudFormationObjectExpression>();
        public Dictionary<string, CloudFormationResource> Resources { get; set; } = new Dictionary<string, CloudFormationResource>();
        public Dictionary<string, CloudFormationOutput> Outputs { get; set; } = new Dictionary<string, CloudFormationOutput>();
        public Dictionary<string, ACloudFormationExpression> Metadata { get; set; } = new Dictionary<string, ACloudFormationExpression>();
    }

    public class CloudFormationParameter {

        //--- Properties ---
        public string? Type { get; set; }
        public string? Description { get; set; }
        public string? AllowedPattern { get; set; }
        public List<string>? AllowedValues { get; set; }
        public string? ConstraintDescription { get; set; }
        public string? Default { get; set; }
        public int? MinLength { get; set; }
        public int? MaxLength { get; set; }
        public int? MinValue { get; set; }
        public int? MaxValue { get; set; }
        public bool? NoEcho { get; set; }
    }

    public class CloudFormationResource {

        //--- Properties ---
        public string? Type { get; set; }
        public CloudFormationObjectExpression Properties { get; set; } = new CloudFormationObjectExpression();
        public List<string> DependsOn { get; set; } = new List<string>();
        public Dictionary<string, ACloudFormationExpression> Metadata { get; set; } = new Dictionary<string, ACloudFormationExpression>();
        public string? Condition { get; set; }
        public string? DeletionPolicy { get; set; }
    }

    public class CloudFormationOutput {

        //--- Properties ---
        public ACloudFormationExpression? Value { get; set; }
        public Dictionary<string, ACloudFormationExpression>? Export { get; set; }
        public string? Description { get; set; }
        public string? Condition { get; set; }
    }

    public class ACloudFormationExpression { }

    public class CloudFormationObjectExpression : ACloudFormationExpression, IEnumerable, IEnumerable<CloudFormationObjectExpression.KeyValuePair> {

        //--- Types ---
        public class KeyValuePair {

            //--- Constructors ---
            public KeyValuePair(string key, ACloudFormationExpression value) {
                Key = key;
                Value = value;
            }

            //--- Properties ---
            public string Key { get; }
            public ACloudFormationExpression Value { get; set; }
        }

        //--- Fields ---
        private readonly List<KeyValuePair> _pairs = new List<KeyValuePair>();

        //--- Constructors ---
        public CloudFormationObjectExpression() => _pairs = new List<KeyValuePair>();

        public CloudFormationObjectExpression(IEnumerable<KeyValuePair> pairs)
            => _pairs = pairs.Select(pair => new KeyValuePair(pair.Key, pair.Value)).ToList();

        //--- Operators ---
        public ACloudFormationExpression this[string key] {
            get {
                if(key == null) {
                    throw new ArgumentNullException(nameof(key));
                }
                return _pairs.First(item => item.Key == key).Value;
            }
            set {
                if(key == null) {
                    throw new ArgumentNullException(nameof(key));
                }
                if(value == null) {
                    throw new ArgumentNullException(nameof(value));

                }
                Remove(key);
                _pairs.Add(new KeyValuePair(key, value));
            }
        }

        //--- Properties ---
        public int Count => _pairs.Count;

        //--- Methods ---
        public bool TryGetValue(string key, [NotNullWhen(true)] out ACloudFormationExpression? value) {
            var found = _pairs.FirstOrDefault(item => item.Key == key);
            value = found?.Value;
            return value != null;
        }

        public bool Remove(string key) {
            if(key == null) {
                throw new ArgumentNullException(nameof(key));
            }
            return _pairs.RemoveAll(kv => kv.Key == key) > 0;
        }

        public bool ContainsKey(string key) => _pairs.Any(item => item.Key == key);

        //--- IEnumerable Members ---
        IEnumerator IEnumerable.GetEnumerator() => _pairs.GetEnumerator();

        //--- IEnumerable<AExpression> Members ---
        IEnumerator<KeyValuePair> IEnumerable<KeyValuePair>.GetEnumerator() => _pairs.GetEnumerator();
    }

    public class CloudFormationListExpression : ACloudFormationExpression, IEnumerable, IEnumerable<ACloudFormationExpression> {

        //--- Fields ---
        private readonly List<ACloudFormationExpression> _items;

        //--- Constructors ---
        public CloudFormationListExpression() => _items = new List<ACloudFormationExpression>();

        public CloudFormationListExpression(IEnumerable<ACloudFormationExpression> items)
            => _items = new List<ACloudFormationExpression>(items);

        //--- Properties ---
        public int Count => _items.Count;

        //--- Operators ---
        public ACloudFormationExpression this[int index] {
            get => _items[index];
            set => _items[index] = value ?? throw new ArgumentNullException(nameof(value));
        }

        //--- Methods ---
        public void Add(ACloudFormationExpression expression) => _items.Add(expression);

        //--- IEnumerable Members ---
        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

        //--- IEnumerable<AExpression> Members ---
        IEnumerator<ACloudFormationExpression> IEnumerable<ACloudFormationExpression>.GetEnumerator() => _items.GetEnumerator();
    }

    public class CloudFormationLiteralExpression : ACloudFormationExpression {

        //--- Constructors ---
        public CloudFormationLiteralExpression(string value) => Value = value ?? throw new ArgumentNullException(nameof(value));
        public CloudFormationLiteralExpression(int value) => Value = value.ToString();

        //--- Properties ---
        public string Value { get; }
    }

    public class CloudFormationModuleNameMappings {

        //--- Constants ---
        public const string CurrentVersion = "2019-07-04";


        //--- Properties ---
        public string Version { get; set; } = CurrentVersion;
        public IDictionary<string, string> ResourceNameMappings { get; set; } = new Dictionary<string, string>();
        public IDictionary<string, string> TypeNameMappings { get; set; } = new Dictionary<string, string>();
    }

    public class CloudFormationModuleManifest : ACloudFormationExpression {

        //--- Constants ---
        public const string CurrentVersion = "2019-07-04";

        //--- Fields ---

        [JsonIgnore]
        private ModuleInfo? _moduleInfo;

        //--- Properties ---
        public string Version { get; set; } = CurrentVersion;

        [JsonProperty("Module")]
        public ModuleInfo ModuleInfo {
            get => _moduleInfo ?? throw new InvalidOperationException();
            set => _moduleInfo = value ?? throw new ArgumentNullException();
        }

        public string? Description { get; set; }
        public string? TemplateChecksum { get; set; }
        public DateTime Date { get; set; }
        public VersionInfo? CoreServicesVersion { get; set; }
        public List<CloudFormationModuleManifestParameterSection> ParameterSections { get; set; } = new List<CloudFormationModuleManifestParameterSection>();
        public CloudFormationModuleManifestGitInfo? Git { get; set; }
        public List<string> Artifacts { get; set; } = new List<string>();
        public List<CloudFormationModuleManifestDependency> Dependencies { get; set; } = new List<CloudFormationModuleManifestDependency>();
        public List<CloudFormationModuleManifestResourceType> ResourceTypes { get; set; } = new List<CloudFormationModuleManifestResourceType>();
        public List<CloudFormationModuleManifestOutput> Outputs { get; set; } = new List<CloudFormationModuleManifestOutput>();

        //--- Methods ---
        public string GetModuleTemplatePath() => ModuleInfo.GetArtifactPath($"cloudformation_{ModuleInfo.FullName}_{TemplateChecksum}.json");
        public string GetFullName() => ModuleInfo.FullName;
        public string GetNamespace() => ModuleInfo.Namespace;
        public string GetName() => ModuleInfo.Name;
        public VersionInfo GetVersion() => ModuleInfo.Version;

        public IEnumerable<CloudFormationModuleManifestParameter> GetAllParameters()
            => ParameterSections.SelectMany(section => section.Parameters);
    }

    public class CloudFormationModuleManifestGitInfo {

        //--- Properties ---
        public string? Branch { get; set; }
        public string? SHA { get; set; }
    }

    public class CloudFormationModuleManifestResourceType {

       //--- Properties ---
       public string? Type { get; set; }
       public string? Description { get; set; }
       public IEnumerable<CloudFormationModuleManifestResourceProperty> Properties { get; set; } = new List<CloudFormationModuleManifestResourceProperty>();
       public IEnumerable<CloudFormationModuleManifestResourceAttribute> Attributes { get; set; } = new List<CloudFormationModuleManifestResourceAttribute>();
    }

    public class CloudFormationModuleManifestResourceProperty {

       //--- Properties ---
       public string? Name { get; set; }
       public string? Description { get; set; }
       public string Type { get; set; } = "String";
       public bool Required { get; set; } = true;
    }

    public class CloudFormationModuleManifestResourceAttribute {

       //--- Properties ---
       public string? Name { get; set; }
       public string? Description { get; set; }
       public string Type { get; set; } = "String";
    }

    public class CloudFormationModuleManifestOutput {

        //--- Properties ---
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Type { get; set; }
    }

    public class CloudFormationModuleManifestMacro {

        //--- Properties ---
        public string? Name { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum CloudFormationModuleManifestDependencyType {
        Unknown,
        Root,
        Nested,
        Shared
    }

    public class CloudFormationModuleManifestDependency {

        //--- Properties ---
        public ModuleInfo? ModuleInfo { get; set; }
        public CloudFormationModuleManifestDependencyType Type { get; set; }
    }

    public class CloudFormationModuleManifestParameterSection {

        //--- Properties ---
        public string? Title { get; set; }
        public List<CloudFormationModuleManifestParameter> Parameters { get; set; } = new List<CloudFormationModuleManifestParameter>();
    }

    public class CloudFormationModuleManifestParameter {

        //--- Properties ---
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? Label { get; set; }
        public string? Default { get; set; }
        public string? Import { get; set; }
        public List<string>? AllowedValues { get; set; }
        public string? AllowedPattern { get; set; }
        public string? ConstraintDescription { get; set; }
    }
}
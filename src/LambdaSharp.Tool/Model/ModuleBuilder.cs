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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LambdaSharp.Tool.Internal;
using Newtonsoft.Json.Linq;

namespace LambdaSharp.Tool.Model {
    using static ModelFunctions;

    public class ModuleBuilderDependency {

        //--- Properties ---
        public ModuleManifest Manifest { get; set; }
        public ModuleLocation ModuleLocation { get; set; }
        public ModuleManifestDependencyType Type;
    }

    public class ModuleBuilder : AModelProcessor {

        //--- Fields ---
        private string _namespace;
        private string _name;
        private string _description;
        private IList<object> _pragmas;
        private IList<object> _secrets;
        private Dictionary<string, AModuleItem> _itemsByFullName;
        private List<AModuleItem> _items;
        private IList<Humidifier.Statement> _resourceStatements = new List<Humidifier.Statement>();
        private IList<string> _artifacts;
        private IDictionary<string, ModuleBuilderDependency> _dependencies;
        private IList<ModuleManifestResourceType> _customResourceTypes;
        private IList<string> _macroNames;
        private IDictionary<string, string> _resourceTypeNameMappings;

        //--- Constructors ---
        public ModuleBuilder(Settings settings, string sourceFilename, Module module) : base(settings, sourceFilename) {
            _namespace = module.Namespace;
            _name = module.Name;
            Version = module.Version;
            _description = module.Description;
            _pragmas = new List<object>(module.Pragmas ?? new object[0]);
            _secrets = new List<object>(module.Secrets ?? new object[0]);
            _items = new List<AModuleItem>(module.Items ?? new AModuleItem[0]);
            _itemsByFullName = _items.ToDictionary(item => item.FullName);
            _artifacts = new List<string>(module.Artifacts ?? new string[0]);
            _dependencies = (module.Dependencies != null)
                ? new Dictionary<string, ModuleBuilderDependency>(module.Dependencies)
                : new Dictionary<string, ModuleBuilderDependency>();
            _customResourceTypes = (module.CustomResourceTypes != null)
                ? new List<ModuleManifestResourceType>(module.CustomResourceTypes)
                : new List<ModuleManifestResourceType>();
            _macroNames = new List<string>(module.MacroNames ?? new string[0]);
            _resourceTypeNameMappings = module.ResourceTypeNameMappings ?? new Dictionary<string, string>();

            // extract existing resource statements when they exist
            if(TryGetItem("Module::Role", out var moduleRoleItem)) {
                var role = (Humidifier.IAM.Role)((ResourceItem)moduleRoleItem).Resource;
                _resourceStatements = new List<Humidifier.Statement>(role.Policies[0].PolicyDocument.Statement);
                role.Policies[0].PolicyDocument.Statement = new List<Humidifier.Statement>();
            } else {
                _resourceStatements = new List<Humidifier.Statement>();
            }
        }

        //--- Properties ---
        public string Namespace => _namespace;
        public string Name => _name;
        public string FullName => $"{_namespace}.{_name}";
        public string Info => $"{FullName}:{Version}";
        public ModuleInfo ModuleInfo => new ModuleInfo(_namespace, _name, Version, origin: null);
        public VersionInfo Version { get; set; }
        public IEnumerable<object> Secrets => _secrets;
        public IEnumerable<AModuleItem> Items => _items;
        public IEnumerable<Humidifier.Statement> ResourceStatements => _resourceStatements;
        public bool HasPragma(string pragma) => _pragmas.Contains(pragma);
        public bool HasModuleRegistration => !HasPragma("no-module-registration");
        public bool HasLambdaSharpDependencies => !HasPragma("no-lambdasharp-dependencies");

        public bool TryGetLabeledPragma(string key, out object value) {
            foreach(var dictionaryPragma in _pragmas.OfType<IDictionary>()) {
                var entry = dictionaryPragma[key];
                if(entry != null) {
                    value = entry;
                    return true;
                }
            }
            value = null;
            return false;
        }

        public bool TryGetOverride(string key, out object expression) {
            if(
                TryGetLabeledPragma("Overrides", out var value)
                && (value is IDictionary dictionary)
            ) {
                var entry = dictionary[key];
                if(entry != null) {
                    expression = entry;
                    return true;
                }
            }
            expression = null;
            return false;
        }

        //--- Methods ---
        public AModuleItem GetItem(string fullNameOrResourceName) {
            if(fullNameOrResourceName.StartsWith("@", StringComparison.Ordinal)) {
                return _items.FirstOrDefault(e => e.ResourceName == fullNameOrResourceName) ?? throw new KeyNotFoundException(fullNameOrResourceName);
            }
            return _itemsByFullName[fullNameOrResourceName];
        }

        public void AddPragma(object pragma) => _pragmas.Add(pragma);

        public bool TryGetItem(string fullNameOrResourceName, out AModuleItem item) {
            if(fullNameOrResourceName == null) {
                item = null;
                return false;
            }
            if(fullNameOrResourceName.StartsWith("@", StringComparison.Ordinal)) {
                item = _items.FirstOrDefault(e => e.ResourceName == fullNameOrResourceName);
                return item != null;
            }
            return _itemsByFullName.TryGetValue(fullNameOrResourceName, out item);
        }

        public void RemoveItem(string fullName) {
            if(TryGetItem(fullName, out var item)) {

                // check if the module role is being removed
                if(fullName == "Module::Role") {

                    // remove all resource statements
                    _resourceStatements.Clear();

                    // remove all secrets
                    _secrets.Clear();
                }
                _items.Remove(item);
                _itemsByFullName.Remove(item.FullName);
            }
        }

        public void AddArtifact(string fullName, string artifact) {
            _artifacts.Add(Path.GetRelativePath(Settings.OutputDirectory, artifact));

            // update item with the name of the artifact
            GetItem(fullName).Reference = Path.GetFileName(artifact);
        }

        public async Task<ModuleBuilderDependency> AddDependencyAsync(ModuleInfo moduleInfo, ModuleManifestDependencyType dependencyType) {
            string moduleKey;
            switch(dependencyType) {
            case ModuleManifestDependencyType.Nested:

                // nested dependencies can reference different versions
                moduleKey = moduleInfo.ToString();
                if(_dependencies.ContainsKey(moduleKey)) {
                    return null;
                }
                break;
            case ModuleManifestDependencyType.Shared:

                // shared dependencies can only have one version
                moduleKey = moduleInfo.WithoutVersion().ToString();

                // check if a dependency was already registered
                if(_dependencies.TryGetValue(moduleKey, out var existingDependency)) {
                    if(
                        (moduleInfo.Version == null)
                        || (
                            (existingDependency.ModuleLocation.ModuleInfo.Version != null)
                            && existingDependency.ModuleLocation.ModuleInfo.Version.IsGreaterOrEqualThanVersion(moduleInfo.Version)
                        )
                    ) {

                        // keep existing shared dependency
                        return null;
                    }
                }
                break;
            default:
                LogError($"unsupported depency type '{dependencyType}' for {moduleInfo.ToString()}");
                return null;
            }

            // validate dependency
            var loader = new ModelManifestLoader(Settings, SourceFilename);
            ModuleBuilderDependency dependency;
            if(!Settings.NoDependencyValidation) {
                dependency = new ModuleBuilderDependency {
                    Type = dependencyType,
                    ModuleLocation = await loader.ResolveInfoToLocationAsync(moduleInfo, dependencyType, allowImport: true, showError: true)
                };
                if(dependency.ModuleLocation == null) {

                    // nothing to do; loader already emitted an error
                    return null;
                }
                dependency.Manifest = await loader.LoadManifestFromLocationAsync(dependency.ModuleLocation);
                if(dependency.Manifest == null) {

                    // nothing to do; loader already emitted an error
                    return null;
                }
            } else {
                LogWarn("unable to validate dependency");
                dependency = new ModuleBuilderDependency {
                    Type = dependencyType,
                    ModuleLocation = new ModuleLocation(Settings.DeploymentBucketName, moduleInfo, "00000000000000000000000000000000")
                };
            }
            _dependencies[moduleKey] = dependency;
            return dependency;
        }

        public bool AddSecret(object secret) {
            if(secret is string textSecret) {
                if(textSecret.StartsWith("arn:")) {

                    // decryption keys provided with their ARN can be added as is; no further steps required
                    _secrets.Add(secret);
                    return true;
                }

                // assume key name is an alias and resolve it to its ARN
                try {
                    var response = Settings.KmsClient.DescribeKeyAsync(textSecret).Result;
                    _secrets.Add(response.KeyMetadata.Arn);
                    return true;
                } catch(Exception e) {
                    LogError($"failed to resolve key alias: {textSecret}", e);
                    return false;
                }
            } else {
                _secrets.Add(secret);
                return true;
            }
        }

        public AModuleItem AddParameter(
            string name,
            string section,
            string label,
            string description,
            string type,
            IList<string> scope,
            bool? noEcho,
            string defaultValue,
            string constraintDescription,
            string allowedPattern,
            IList<string> allowedValues,
            int? maxLength,
            int? maxValue,
            int? minLength,
            int? minValue,
            object allow,
            IDictionary<string, object> properties,
            string arnAttribute,
            IDictionary<string, string> encryptionContext,
            IList<object> pragmas
        ) {

            // create input parameter item
            var parameter = new Humidifier.Parameter {
                Type = ResourceMapping.ToCloudFormationParameterType(type),
                Description = description,
                Default = defaultValue,
                ConstraintDescription = constraintDescription,
                AllowedPattern = allowedPattern,
                AllowedValues = allowedValues?.ToList(),
                MaxLength = maxLength,
                MaxValue = maxValue,
                MinLength = minLength,
                MinValue = minValue,
                NoEcho = noEcho
            };
            var result = AddItem(new ParameterItem(
                parent: null,
                name: name,
                section: section,
                label: label,
                description: description,
                type: type,
                scope: scope,
                reference: null,
                parameter: parameter,
                import: null
            ));

            // check if a resource-type is associated with the input parameter
            if(result.HasSecretType) {
                var decoder = AddResource(
                    parent: result,
                    name: "Plaintext",
                    description: null,
                    scope: null,
                    resource: CreateDecryptSecretResourceFor(result),
                    resourceExportAttribute: null,
                    dependsOn: null,
                    condition: null,
                    pragmas: null
                );
                decoder.Reference = FnGetAtt(decoder.ResourceName, "Plaintext");
                decoder.DiscardIfNotReachable = true;
            } else if(!result.HasAwsType) {

                // nothing to do
            } else if(properties == null) {

                // request input parameter resource grants
                AddGrant(result.LogicalId, type, result.Reference, allow, condition: null);
            } else {

                // create conditional resource
                var condition = AddCondition(
                    parent: result,
                    name: "IsBlank",
                    description: null,
                    value: FnEquals(FnRef(result.ResourceName), "")
                );
                var instance = AddResource(
                    parent: result,
                    name: "Resource",
                    description: null,
                    scope: null,
                    type: type,
                    allow: null,
                    properties: properties,
                    arnAttribute: arnAttribute,
                    dependsOn: null,
                    condition: condition.FullName,
                    pragmas: pragmas
                );

                // register input parameter reference
                result.Reference = FnIf(
                    condition.ResourceName,
                    instance.GetExportReference(),
                    result.Reference
                );

                // request input parameter or conditional managed resource grants
                AddGrant(instance.LogicalId, type, result.Reference, allow, condition: null);
            }
            return result;
        }

        public AModuleItem AddImport(
            AModuleItem parent,
            string name,
            string description,
            string type,
            IList<string> scope,
            object allow,
            string module,
            IDictionary<string, string> encryptionContext
        ) {

            // extract optional export name from module reference
            var export = name;
            var moduleParts = module.Split("::", 2);
            if(moduleParts.Length == 2) {
                module = moduleParts[0];
                export = moduleParts[1];
            }

            // validate module name
            if(!ModuleInfo.TryParse(module, out var moduleInfo)) {
                LogError("invalid 'Module' attribute");
            } else {
                module = moduleInfo.FullName;
            }
            if(moduleInfo.Version != null) {
                LogError("'Module' attribute cannot have a version");
            }
            if(moduleInfo.Origin != null) {
                LogError("'Module' attribute cannot have an origin");
            }

            // create input parameter item
            var parameter = new Humidifier.Parameter {
                Type = ResourceMapping.ToCloudFormationParameterType(type),
                Description = $"Cross-module reference for {module}::{export}",

                // default value for an imported parameter is always the cross-module reference
                Default = $"${module.Replace(".", "-")}::{export}",

                // set default settings for import parameters
                ConstraintDescription = "must either be a cross-module reference or a non-empty value",
                AllowedPattern =  @"^.+$"
            };
            var import = new ParameterItem(
                parent: null,
                name: module.ToIdentifier() + export.ToIdentifier(),
                section: $"{module} Imports",
                label: export,
                description: null,
                type: type,
                scope: null,
                reference: null,
                parameter: parameter,
                import: $"{module}::{export}"
            );
            import.DiscardIfNotReachable = true;

            // check if an import parameter for this reference exists already
            var found = _items.FirstOrDefault(item => item.FullName == import.FullName);
            if(found is ParameterItem existing) {
                if(existing.Parameter.Default != parameter.Default) {
                    LogError($"import parameter '{import.FullName}' is already defined with a different binding");
                }
                import = existing;
            } else {

                // add parameter and map it to variable
                AddItem(import);
                var condition = AddCondition(
                    parent: import,
                    name: "IsImported",
                    description: null,
                    value: FnAnd(
                        FnNot(FnEquals(FnRef(import.ResourceName), "")),
                        FnEquals(FnSelect("0", FnSplit("$", FnRef(import.ResourceName))), "")
                    )
                );

                // check if import itself is conditional
                import.Reference = FnIf(
                    condition.ResourceName,
                    FnImportValue(FnSub("${DeploymentPrefix}${Import}", new Dictionary<string, object> {
                        ["Import"] = FnSelect("1", FnSplit("$", FnRef(import.ResourceName)))
                    })),
                    FnRef(import.ResourceName)
                );
            }

            // TODO (2019-02-07, bjorg): since the variable is created for each import, it also duplicates the '::Plaintext' sub-resource
            //  for imports of type 'Secret'; while it's technically not wrong, it's not efficient when multiple secrets are being imported.

            // register import parameter reference
            return AddVariable(
                parent: parent,
                name: name,
                description: description,
                type: type,
                scope: scope,
                value: FnRef(import.FullName),
                allow: allow,
                encryptionContext: encryptionContext
            );
        }

        public void AddResourceType(
            string resourceType,
            string description,
            string handler,
            IEnumerable<ModuleManifestResourceProperty> properties,
            IEnumerable<ModuleManifestResourceProperty> attributes
        ) {

            // TODO (2018-09-20, bjorg): add custom resource name validation
            if(_customResourceTypes.Any(existing => existing.Type == resourceType)) {
                LogError($"Resource type '{resourceType}' is already defined.");
            }

            // add resource type definition
            AddItem(new ResourceTypeItem(resourceType, description, handler));
            _customResourceTypes.Add(new ModuleManifestResourceType {
                Type = resourceType,
                Description = description,
                Properties = properties ?? Enumerable.Empty<ModuleManifestResourceProperty>(),
                Attributes = attributes ?? Enumerable.Empty<ModuleManifestResourceProperty>()
            });
        }

        public AModuleItem AddMacro(string macroName, string description, string handler) {
            Validate(Regex.IsMatch(macroName, CLOUDFORMATION_ID_PATTERN), "name is not valid");

            // check if a root macros collection needs to be created
            if(!TryGetItem("Macros", out var macrosItem)) {
                macrosItem = AddVariable(
                    parent: null,
                    name: "Macros",
                    description: "Macro definitions",
                    type: "String",
                    scope: null,
                    value: "",
                    allow: null,
                    encryptionContext: null
                );
            }

            // add macro resource
            var result = AddResource(
                parent: macrosItem,
                name: macroName,
                description: description,
                scope: null,
                resource: new Humidifier.CloudFormation.Macro {

                    // TODO (2018-10-30, bjorg): we may want to set 'LogGroupName' and 'LogRoleARN' as well

                    Name = FnSub($"${{DeploymentPrefix}}{macroName}"),
                    Description = description,
                    FunctionName = FnRef(handler)
                },
                resourceExportAttribute: null,
                dependsOn: null,
                condition: null,
                pragmas: null
            );
            _macroNames.Add(macroName);
            return result;
        }

        public AModuleItem AddVariable(
            AModuleItem parent,
            string name,
            string description,
            string type,
            IList<string> scope,
            object value,
            object allow,
            IDictionary<string, string> encryptionContext
        ) {
            if(value == null) {
                throw new ArgumentNullException(nameof(value));
            }
            var result = AddItem(new VariableItem(parent, name, description, type, scope, reference: null));

            // the format for secrets with encryption keys is: SECRET|KEY1=VALUE1|KEY2=VALUE2
            if(encryptionContext != null) {
                Validate(type == "Secret", "type must be 'Secret' to use 'EncryptionContext'");
                result.Reference = FnJoin(
                    "|",
                    new object[] {
                        value
                    }.Union(
                        encryptionContext.Select(kv => $"{kv.Key}={kv.Value}")
                    ).ToArray()
                );
            } else {
                result.Reference = (value is IList<object> values)
                    ? FnJoin(",", values)
                    : value;
            }

            // check if value must be decrypted
            if(result.HasSecretType) {
                var decoder = AddResource(
                    parent: result,
                    name: "Plaintext",
                    description: null,
                    scope: null,
                    resource: CreateDecryptSecretResourceFor(result),
                    resourceExportAttribute: null,
                    dependsOn: null,
                    condition: null,
                    pragmas: null
                );
                decoder.Reference = FnGetAtt(decoder.ResourceName, "Plaintext");
                decoder.DiscardIfNotReachable = true;
            }

            // add optional grants
            if(allow != null) {
                AddGrant(result.LogicalId, type, value, allow, condition: null);
            }
            return result;
        }

        public AModuleItem AddResource(
            AModuleItem parent,
            string name,
            string description,
            IList<string> scope,
            Humidifier.Resource resource,
            string resourceExportAttribute,
            IList<string> dependsOn,
            object condition,
            IList<object> pragmas
        ) {

            // set a default export attribute if none is provided
            if(resourceExportAttribute == null) {
                var resourceTypeName = (resource is Humidifier.CustomResource customResource)
                    ? customResource.OriginalTypeName
                    : resource.AWSTypeName;
                if(ResourceMapping.HasAttribute(resourceTypeName, "Arn")) {

                    // for built-in type, use the 'Arn' attribute if it exists
                    resourceExportAttribute = "Arn";
                } else if(TryGetResourceType(resourceTypeName, out var resourceType)) {

                    // for custom resource types, use the first defined response attribute
                    resourceExportAttribute = resourceType.Attributes.FirstOrDefault()?.Name;
                }
            }

            // create resource
            var result = new ResourceItem(
                parent: parent,
                name: name,
                description: description,
                scope: scope,
                resource: resource,
                resourceExportAttribute: resourceExportAttribute,
                dependsOn: dependsOn,
                condition: null,
                pragmas: pragmas
            );
            AddItem(result);

            // add condition
            if(condition is string conditionName) {
                result.Condition = conditionName;
            } else if(condition != null) {
                var conditionItem = AddCondition(
                    parent: result,
                    name: "Condition",
                    description: null,
                    value: condition
                );
                result.Condition = conditionItem.FullName;
            }
            return result;
        }

        public AModuleItem AddResource(
            AModuleItem parent,
            string name,
            string description,
            string type,
            IList<string> scope,
            object allow,
            IDictionary<string, object> properties,
            IList<string> dependsOn,
            string arnAttribute,
            object condition,
            IList<object> pragmas
        ) {

            // create resource item
            var customResource = RegisterCustomResourceNameMapping(new Humidifier.CustomResource(type, properties));

            // add resource
            var result = AddResource(
                parent: parent,
                name: name,
                description: description,
                scope: scope,
                resource: customResource,
                resourceExportAttribute: arnAttribute,
                dependsOn: dependsOn,
                condition: condition,
                pragmas: pragmas
            );

            // validate resource properties
            if(result.HasTypeValidation) {
                ValidateProperties(type, customResource);
            }

            // add optional grants
            if(allow != null) {
                AddGrant(result.LogicalId, type, result.GetExportReference(), allow, condition: null);
            }
            return result;
        }

        public AModuleItem AddNestedModule(
            AModuleItem parent,
            string name,
            string description,
            ModuleInfo moduleInfo,
            IList<string> scope,
            object dependsOn,
            IDictionary<string, object> parameters
        ) {
            var moduleParameters = (parameters != null)
                ? new Dictionary<string, object>(parameters)
                : new Dictionary<string, object>();
            if(moduleInfo.Version == null) {
                LogError("missing module version");
            }

            // add nested module resource
            var stack = new Humidifier.CloudFormation.Stack {
                NotificationARNs = FnRef("AWS::NotificationARNs"),
                Parameters = moduleParameters,
                Tags = new List<Humidifier.Tag> {
                    new Humidifier.Tag {
                        Key = "LambdaSharp:Module",
                        Value = moduleInfo.FullName
                    }
                },

                // this value gets set once the template was successfully loaded for validation
                TemplateURL = "<BAD>",

                // TODO (2018-11-29, bjorg): make timeout configurable
                TimeoutInMinutes = 15
            };
            var resource = AddResource(
                parent: parent,
                name: name,
                description: description,
                scope: scope,
                resource: stack,
                resourceExportAttribute: null,
                dependsOn: ConvertToStringList(dependsOn),
                condition: null,
                pragmas: null
            );
            var dependency = AddDependencyAsync(moduleInfo, ModuleManifestDependencyType.Nested).Result;

            // validate module parameters
            AtLocation("Parameters", () => {
                if(dependency?.Manifest != null) {
                    if(!Settings.NoDependencyValidation) {
                        var manifest = dependency.Manifest;

                        // update stack resource source with hashed cloudformation key
                        stack.TemplateURL = $"https://{ModuleInfo.MODULE_ORIGIN_PLACEHOLDER}.s3.amazonaws.com/{dependency.ModuleLocation.ModuleTemplateKey}";

                        // validate that all required parameters are supplied
                        var formalParameters = manifest.GetAllParameters().ToDictionary(p => p.Name);
                        foreach(var formalParameter in formalParameters.Values.Where(p => (p.Default == null) && !moduleParameters.ContainsKey(p.Name))) {
                            LogError($"missing module parameter '{formalParameter.Name}'");
                        }

                        // validate that all supplied parameters exist
                        foreach(var moduleParameter in moduleParameters.Where(kv => !formalParameters.ContainsKey(kv.Key))) {
                            LogError($"unknown module parameter '{moduleParameter.Key}'");
                        }

                        // inherit dependencies from nested module
                        foreach(var manifestDependency in manifest.Dependencies) {
                            AddDependencyAsync(manifestDependency.ModuleInfo, manifestDependency.Type).Wait();
                        }

                        // inherit import parameters that are not provided by the declaration
                        foreach(var nestedImport in manifest.GetAllParameters()
                            .Where(parameter => parameter.Import != null)
                            .Where(parameter => !moduleParameters.ContainsKey(parameter.Name))
                        ) {
                            var import = AddImport(
                                parent: resource,
                                name: nestedImport.Name,
                                description: null,
                                type: nestedImport.Type,
                                scope: null,
                                allow: null,
                                module: nestedImport.Import,
                                encryptionContext: null
                            );
                            moduleParameters.Add(nestedImport.Name, FnRef(import.FullName));
                        }

                        // check if x-ray tracing should be enabled in nested module
                        if(formalParameters.ContainsKey("XRayTracing") && !moduleParameters.ContainsKey("XRayTracing")) {
                            moduleParameters.Add("XRayTracing", FnIf("XRayNestedIsEnabled", XRayTracingLevel.AllModules.ToString(), XRayTracingLevel.Disabled.ToString()));
                        }
                    } else {
                        LogWarn("unable to validate nested module parameters");
                    }
                } else {

                    // nothing to do; loader already emitted an error
                }

                // add expected parameters
                MandatoryAdd("DeploymentBucketName", FnRef("DeploymentBucketName"));
                MandatoryAdd("DeploymentPrefix", FnRef("DeploymentPrefix"));
                MandatoryAdd("DeploymentPrefixLowercase", FnRef("DeploymentPrefixLowercase"));
                MandatoryAdd("DeploymentRoot", FnRef("Module::RootId"));
                MandatoryAdd("LambdaSharpCoreServices", FnRef("LambdaSharpCoreServices"));
            });
            return resource;

            // local function
            void MandatoryAdd(string key, object value) {
                if(!moduleParameters.ContainsKey(key)) {
                    moduleParameters.Add(key, value);
                } else {
                    LogError($"'{key}' is a reserved attribute and cannot be specified");
                }
            }
        }

        public AModuleItem AddPackage(
            AModuleItem parent,
            string name,
            string description,
            IList<string> scope,
            IList<KeyValuePair<string, string>> files
        ) {

            // create variable corresponding to the package definition
            var package = new PackageItem(
                parent: parent,
                name: name,
                description: description,
                scope: scope,
                files: files
            );
            AddItem(package);

            // create nested variable for tracking the package-name
            var packageName = AddVariable(
                parent: package,
                name: "PackageName",
                description: null,
                type: "String",
                scope: null,
                value: $"{package.LogicalId}-DRYRUN.zip",
                allow: null,
                encryptionContext: null
            );

            // update the package variable to use the package-name variable
            package.Reference = ModuleInfo.GetModuleArtifactExpression($"${{{packageName.FullName}}}");
            return package;
        }

        public AModuleItem AddFunction(
            AModuleItem parent,
            string name,
            string description,
            IList<string> scope,
            string project,
            string language,
            IDictionary<string, object> environment,
            IList<AFunctionSource> sources,
            object condition,
            IList<object> pragmas,
            string timeout,
            string runtime,
            string memory,
            string handler,
            IDictionary<string, object> properties
        ) {
            var definition = (properties != null)
                ? new Dictionary<string, object>(properties)
                : new Dictionary<string, object>();

            // set optional function resource properties
            if(description != null) {

                // append version number to function description
                definition["Description"] = description.TrimEnd() + $" (v{Version})";
            }
            if(timeout != null) {
                definition["Timeout"] = timeout;
            }
            if(runtime != null) {
                definition["Runtime"] = runtime;
            }
            if(memory != null) {
                definition["MemorySize"] = memory;
            }
            if(handler != null) {
                definition["Handler"] = handler;
            }

            // set function resource properties to defaults when not defined
            if(!definition.ContainsKey("Role")) {
                definition["Role"] = FnGetAtt("Module::Role", "Arn");
            }
            if(!definition.ContainsKey("Environment")) {
                definition["Environment"] = new Dictionary<string, object> {
                    ["Variables"] = new Dictionary<string, dynamic>()
                };
            }
            if(!definition.ContainsKey("Code")) {
                definition["Code"] = new Dictionary<string, object> {
                    ["S3Key"] = "<TBD>",
                    ["S3Bucket"] = FnRef("DeploymentBucketName")
                };
            }
            if(!definition.ContainsKey("TracingConfig")) {
                definition["TracingConfig"] = new Dictionary<string, object> {
                    ["Mode"] = FnIf("XRayIsEnabled", "Active", "PassThrough")
                };
            }
            AtLocation("Properties", () => ValidateProperties("AWS::Lambda::Function", definition));

            // initialize function resource from definition
            var resource = (Humidifier.Lambda.Function)JObject.FromObject(definition).ToObject<Humidifier.Lambda.Function>().ConvertJTokenToNative();

            // create function item
            var function = new FunctionItem(
                parent: parent,
                name: name,
                description: description,
                scope: scope,
                project: project,
                language: language,
                environment: environment ?? new Dictionary<string, object>(),
                sources: sources ?? new AFunctionSource[0],
                condition: null,
                pragmas: pragmas ?? new object[0],
                function: resource
            );
            AddItem(function);

            // add condition
            if(condition is string conditionName) {
                function.Condition = conditionName;
            } else if(condition != null) {
                var conditionItem = AddCondition(
                    parent: function,
                    name: "If",
                    description: null,
                    value: condition
                );
                function.Condition = conditionItem.FullName;
            }

            // create nested variable for tracking the package-name
            var packageName = AddVariable(
                parent: function,
                name: "PackageName",
                description: null,
                type: "String",
                scope: null,
                value: $"{function.LogicalId}-DRYRUN.zip",
                allow: null,
                encryptionContext: null
            );
            function.Function.Code.S3Key = ModuleInfo.GetModuleArtifactExpression($"${{{packageName.FullName}}}");

            // check if function is a finalizer
            var isFinalizer = (parent == null) && (name == "Finalizer");
            if(isFinalizer) {

                // finalizer doesn't need a log-group or registration b/c it gets deleted anyway on failure or teardown
                function.Pragmas = new List<object>(function.Pragmas) {
                    "no-function-registration",
                    "no-dead-letter-queue"
                };

                // NOTE (2018-12-18, bjorg): always set the 'Finalizer' timeout to the maximum limit to prevent ugly timeout scenarios
                function.Function.Timeout = 900;

                // add finalizer invocation (dependsOn will be set later when all resources have been added)
                AddResource(
                    parent: function,
                    name: "Invocation",
                    description: null,
                    scope: null,
                    resource: RegisterCustomResourceNameMapping(new Humidifier.CustomResource("Module::Finalizer") {
                        ["ServiceToken"] = FnGetAtt(function.FullName, "Arn"),
                        ["DeploymentChecksum"] = FnRef("DeploymentChecksum"),
                        ["ModuleVersion"] = Version.ToString()
                    }),
                    resourceExportAttribute: null,
                    dependsOn: null,
                    condition: condition,
                    pragmas: null
                );
            } else {

                // create function log-group with retention window
                AddResource(
                    parent: function,
                    name: "LogGroup",
                    description: null,
                    scope: null,
                    resource: new Humidifier.Logs.LogGroup {
                        LogGroupName = FnSub($"/aws/lambda/${{{function.ResourceName}}}"),
                        RetentionInDays = FnRef("Module::LogRetentionInDays")
                    },
                    resourceExportAttribute: null,
                    dependsOn: null,
                    condition: condition,
                    pragmas: null
                );
            }
            return function;
        }

        public AModuleItem AddInlineFunction(
            AModuleItem parent,
            string name,
            string description,
            IDictionary<string, object> environment,
            IList<AFunctionSource> sources,
            string condition,
            IList<object> pragmas,
            string timeout,
            string memory,
            string code
        ) {

            // create function resource
            var resource = new Humidifier.Lambda.Function {

                // append version number to function description
                Description = (description != null)
                    ? description.TrimEnd() + $" (v{Version})"
                    : null,
                Timeout = timeout,
                Runtime = "nodejs8.10",
                MemorySize = memory,
                Handler = "index.handler",
                Role = FnGetAtt("Module::Role", "Arn"),
                Environment = new Humidifier.Lambda.FunctionTypes.Environment {
                    Variables = new Dictionary<string, dynamic>()
                },
                Code = new Humidifier.Lambda.FunctionTypes.Code {
                    ZipFile = code
                }
            };

            // create inline function item
            var function = new FunctionItem(
                parent: parent,
                name: name,
                description: description,
                scope: new string[0],
                project: "",
                language: "javascript",
                environment: environment ?? new Dictionary<string, object>(),
                sources: sources ?? new AFunctionSource[0],
                condition: condition,
                pragmas: pragmas ?? new object[0],
                function: resource
            );
            AddItem(function);
            return function;
        }

        public AModuleItem AddCondition(
            AModuleItem parent,
            string name,
            string description,
            object value
        ) {
            return AddItem(new ConditionItem(
                parent: parent,
                name: name,
                description: description,
                value: value
            ));
        }

        public AModuleItem AddMapping(
            AModuleItem parent,
            string name,
            string description,
            IDictionary<string, IDictionary<string, string>> value
        ) {
            return AddItem(new MappingItem(
                parent: parent,
                name: name,
                description: description,
                value: value
            ));
        }

        public void AddGrant(string name, string awsType, object reference, object allow, object condition) {

            // resolve shorthands and deduplicate statements
            var allowStatements = new List<string>();
            foreach(var allowStatement in ConvertToStringList(allow)) {
                if(allowStatement == "None") {

                    // nothing to do
                } else if(allowStatement.Contains(':')) {

                    // AWS permission statements always contain a ':' (e.g 'ssm:GetParameter')
                    allowStatements.Add(allowStatement);
                } else if((awsType != null) && ResourceMapping.TryResolveAllowShorthand(awsType, allowStatement, out var allowedList)) {
                    allowStatements.AddRange(allowedList);
                } else {
                    LogError($"could not find IAM mapping for short-hand '{allowStatement}' on AWS type '{awsType ?? "<omitted>"}'");
                }
            }
            if(!allowStatements.Any()) {
                return;
            }

            // check if statement can be added to role directly or needs to be attached as a conditional policy resource
            if(condition != null) {
                AddResource(
                    parent: GetItem("Module::Role"),
                    name: name + "Policy",
                    description: null,

                    // by scoping this resource to all Lambda functions, we ensure the policy is created before a Lambda executes
                    scope: new[] { "all" },

                    resource: new Humidifier.IAM.Policy {
                        PolicyName = FnSub($"${{AWS::StackName}}ModuleRole{name}"),
                        PolicyDocument = new Humidifier.PolicyDocument {
                            Version = "2012-10-17",
                            Statement = new List<Humidifier.Statement> {
                                new Humidifier.Statement {
                                    Sid = name.ToIdentifier(),
                                    Effect = "Allow",
                                    Resource = ResourceMapping.ExpandResourceReference(awsType, reference),
                                    Action = allowStatements.Distinct().OrderBy(text => text).ToList()
                                }
                            }
                        },
                        Roles = new List<object> {
                            FnRef("Module::Role")
                        }
                    },
                    resourceExportAttribute: null,
                    dependsOn: null,
                    condition: condition,
                    pragmas: null
                ).DiscardIfNotReachable = true;
            } else {

                // add role resource statement
                var statement = new Humidifier.Statement {
                    Sid = name.ToIdentifier(),
                    Effect = "Allow",
                    Resource = ResourceMapping.ExpandResourceReference(awsType, reference),
                    Action = allowStatements.Distinct().OrderBy(text => text).ToList()
                };

                // check if an existing statement is being updated
                for(var i = 0; i < _resourceStatements.Count; ++i) {
                    if(_resourceStatements[i].Sid == name) {
                        _resourceStatements[i] = statement;
                        return;
                    }
                }

                // add new statement
                _resourceStatements.Add(statement);
            }
        }

        public void VisitAll(ModuleVisitorDelegate visitor) {
            if(visitor == null) {
                throw new ArgumentNullException(nameof(visitor));
            }

            // resolve references in secrets
            AtLocation("Secrets", () => {
                _secrets = (IList<object>)visitor(null, _secrets);
            });

            // resolve references in items
            AtLocation("Items", () => {
                foreach(var item in _items) {
                    AtLocation(item.FullName, () => {
                        item.Visit(visitor);
                    });
                }
            });

            // resolve references in output values
            AtLocation("ResourceStatements", () => {
                TryGetItem("Module::Role", out var moduleRole);
                _resourceStatements = (IList<Humidifier.Statement>)visitor(moduleRole, _resourceStatements);
            });
        }

        public bool HasAttribute(AModuleItem item, string attribute) {
            if(TryGetResourceType(item.Type, out var resourceType)) {
                return resourceType.Attributes.Any(field => field.Name == attribute);
            }
            return ResourceMapping.HasAttribute(item.Type, attribute);
        }

        public Module ToModule() {

            // update existing resource statements when they exist
            if(TryGetItem("Module::Role", out var moduleRoleItem)) {
                var role = (Humidifier.IAM.Role)((ResourceItem)moduleRoleItem).Resource;
                role.Policies[0].PolicyDocument.Statement = _resourceStatements.ToList();
            }
            return new Module {
                Namespace = _namespace,
                Name = _name,
                Version = Version,
                Description = _description,
                Pragmas = _pragmas,
                Secrets = _secrets,
                Items = _items,
                Artifacts = _artifacts.OrderBy(value => value).ToList(),
                Dependencies = _dependencies.OrderBy(kv => kv.Key).ToList(),
                CustomResourceTypes = _customResourceTypes.OrderBy(resourceType => resourceType.Type).ToList(),
                MacroNames = _macroNames.OrderBy(value => value).ToList(),
                ResourceTypeNameMappings = _resourceTypeNameMappings
            };
        }

        private AModuleItem AddItem(AModuleItem item) {
            Validate(Regex.IsMatch(item.Name, CLOUDFORMATION_ID_PATTERN), "name is not valid");

            // set default reference
            if(item.Reference == null) {
                item.Reference = FnRef(item.ResourceName);
            }

            // add item
            if(_itemsByFullName.TryAdd(item.FullName, item)) {
                _items.Add(item);
            } else {
                LogError($"duplicate name '{item.FullName}'");
            }
            return item;
        }

        private void ValidateProperties(
            string awsType,
            IDictionary properties
        ) {
            if(ResourceMapping.CloudformationSpec.ResourceTypes.TryGetValue(awsType, out var resource)) {
                ValidateProperties("", resource, properties);
            } else if(!awsType.StartsWith("Custom::", StringComparison.Ordinal)) {
                var dependency = _dependencies.Values.FirstOrDefault(d => d.Manifest?.ResourceTypes.Any(existing => existing.Type == awsType) ?? false);
                if(dependency == null) {
                    if(_dependencies.Values.Any(d => d.Manifest == null)) {

                        // NOTE (2018-12-13, bjorg): one or more manifests were not loaded; give the benefit of the doubt
                        LogWarn($"unable to validate properties for {awsType}");
                    } else {
                        LogError($"unrecognized resource type {awsType}");
                    }
                } else if(properties != null) {
                    var definition = dependency.Manifest?.ResourceTypes.FirstOrDefault(existing => existing.Type == awsType);
                    if(definition != null) {
                        foreach(var key in properties.Keys) {
                            var stringKey = (string)key;
                            if(
                                (stringKey != "ServiceToken")
                                && (stringKey != "ResourceType")
                                && !definition.Properties.Any(field => field.Name == stringKey)) {
                                LogError($"unrecognized attribute '{key}' on type {awsType}");
                            }
                        }
                    }
                }
            }

            // local functions
            void ValidateProperties(string prefix, ResourceType currentResource, IDictionary currentProperties) {

                // 'Fn::Transform' can add arbitrary properties at deployment time, so we can't validate the properties at compile time
                if(!currentProperties.Contains("Fn::Transform")) {

                    // check that all required properties are defined
                    foreach(var property in currentResource.Properties.Where(kv => kv.Value.Required)) {
                        if(currentProperties[property.Key] == null) {
                            LogError($"missing property '{prefix + property.Key}");
                        }
                    }
                }

                // check that all defined properties exist
                foreach(DictionaryEntry property in currentProperties) {
                    if(!currentResource.Properties.TryGetValue((string)property.Key, out var propertyType)) {
                        LogError($"unrecognized property '{prefix + property.Key}'");
                    } else {
                        switch(propertyType.Type) {
                        case "List": {

                                // check if property value is a function invocation
                                if(
                                    (property.Value is IDictionary fnMap)
                                    && (fnMap.Count == 1)
                                    && (fnMap.Keys.OfType<string>().FirstOrDefault() is string fnName)
                                    && ((fnName == "Ref") || fnName.StartsWith("Fn::"))
                                ) {

                                    // TODO (2019-01-25, bjorg): validate the return type of the function is a list
                                } else if(!(property.Value is IList nestedList)) {
                                    LogError($"property type mismatch for '{prefix + property.Key}', expected a list [{property.Value?.GetType().Name ?? "<null>"}]");
                                } else if(propertyType.ItemType != null) {
                                    ResourceMapping.TryGetPropertyItemType(awsType, propertyType.ItemType, out var nestedResource);
                                    ValidateList(prefix + property.Key + ".", nestedResource, ListToEnumerable(nestedList));
                                } else {

                                    // TODO (2018-12-06, bjorg): validate list items using the primitive type
                                }
                            }
                            break;
                        case "Map": {
                                if(
                                    (property.Value is IDictionary fnMap)
                                    && (fnMap.Count == 1)
                                    && (fnMap.Keys.OfType<string>().FirstOrDefault() is string fnName)
                                    && ((fnName == "Ref") || fnName.StartsWith("Fn::"))
                                ) {

                                    // TODO (2019-01-25, bjorg): validate the return type of the function is a map
                                } else if(!(property.Value is IDictionary nestedProperties1)) {
                                    LogError($"property type mismatch for '{prefix + property.Key}', expected a map [{property.Value?.GetType().FullName ?? "<null>"}]");
                                } else if(propertyType.ItemType != null) {
                                    ResourceMapping.TryGetPropertyItemType(awsType, propertyType.ItemType, out var nestedResource);
                                    ValidateList(prefix + property.Key + ".", nestedResource, DictionaryToEnumerable(nestedProperties1));
                                } else {

                                    // TODO (2018-12-06, bjorg): validate map entries using the primitive type
                                }
                            }
                            break;
                        case null:

                            // TODO (2018-12-06, bjorg): validate property value with the primitive type
                            break;
                        default: {
                                if(
                                    (property.Value is IDictionary fnMap)
                                    && (fnMap.Count == 1)
                                    && (fnMap.Keys.OfType<string>().FirstOrDefault() is string fnName)
                                    && ((fnName == "Ref") || fnName.StartsWith("Fn::"))
                                ) {

                                    // TODO (2019-01-25, bjorg): validate the return type of the function is a map
                                } else if(!(property.Value is IDictionary nestedProperties2)) {
                                    LogError($"property type mismatch for '{prefix + property.Key}', expected a map [{property.Value?.GetType().FullName ?? "<null>"}]");
                                } else {
                                    ResourceMapping.TryGetPropertyItemType(awsType, propertyType.Type, out var nestedResource);
                                    ValidateProperties(prefix + property.Key + ".", nestedResource, nestedProperties2);
                                }
                            }
                            break;
                        }
                    }
                }
            }

            void ValidateList(string prefix, ResourceType currentResource, IEnumerable<KeyValuePair<string, object>> items) {
                foreach(var item in items) {
                    if(!(item.Value is IDictionary nestedProperties)) {
                        LogError($"property type mismatch for '{prefix + item.Key}', expected a map [{item.Value?.GetType().FullName ?? "<null>"}]");
                    } else {
                        ValidateProperties(prefix + item.Key + ".", currentResource, nestedProperties);
                    }
                }
            }

            IEnumerable<KeyValuePair<string, object>> DictionaryToEnumerable(IDictionary dictionary) {
                var result = new List<KeyValuePair<string, object>>();
                foreach(DictionaryEntry entry in dictionary) {
                    result.Add(new KeyValuePair<string, object>("." + entry.Key, entry.Value));
                }
                return result;
            }

            IEnumerable<KeyValuePair<string, object>> ListToEnumerable(IList list) {
                var result = new List<KeyValuePair<string, object>>();
                var index = 0;
                foreach(var item in list) {
                    result.Add(new KeyValuePair<string, object>($"{++index}".ToString(), item));
                }
                return result;
            }
        }

        private Humidifier.CustomResource CreateDecryptSecretResourceFor(AModuleItem item)
            => RegisterCustomResourceNameMapping(new Humidifier.CustomResource("Module::DecryptSecret") {
                ["ServiceToken"] = FnGetAtt("Module::DecryptSecretFunction", "Arn"),
                ["Ciphertext"] = FnRef(item.FullName)
            });

        private Humidifier.CustomResource RegisterCustomResourceNameMapping(Humidifier.CustomResource customResource) {
            if(customResource.AWSTypeName != customResource.OriginalTypeName) {
                _resourceTypeNameMappings[customResource.AWSTypeName] = customResource.OriginalTypeName;
            }
            return customResource;
        }

        private bool TryGetResourceType(string resourceTypeName, out ModuleManifestResourceType resourceType) {
            var matches = _dependencies
                .Where(kv => kv.Value.Type == ModuleManifestDependencyType.Shared)
                .Select(kv => new {
                    Found = kv.Value.Manifest?.ResourceTypes.FirstOrDefault(existing => existing.Type == resourceTypeName),
                    From = kv.Key
                })
                .Where(foundResourceType => foundResourceType.Found != null)
                .ToArray();
            switch(matches.Length) {
            case 0:
                resourceType = null;
                return false;
            case 1:
                resourceType = matches[0].Found;
                return true;
            default:
                LogWarn($"ambiguous resource type '{resourceTypeName}' [{string.Join(", ", matches.Select(t => t.From))}]");
                resourceType = matches[0].Found;
                return true;
            }
        }
    }
}
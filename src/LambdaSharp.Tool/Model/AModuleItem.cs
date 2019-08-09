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
using System.IO;
using System.Linq;
using LambdaSharp.Tool.Internal;
using Newtonsoft.Json;

namespace LambdaSharp.Tool.Model {
    using static ModelFunctions;

    public delegate object ModuleVisitorDelegate(AModuleItem item, object value);

    public abstract class AModuleItem {

        //--- Constructors ---
        public AModuleItem(
            AModuleItem parent,
            string name,
            string description,
            string type,
            IList<string> scope,
            object reference
        ) {
            Name = name ?? throw new ArgumentNullException(nameof(name));;
            FullName = (parent == null)
                ? name
                : parent.FullName + "::" + name;
            Description = description;

            // TODO (2018-11-29, bjorg): logical ID should be computed by module builder to disambiguate hierarchical names when name collisions occur
            LogicalId = (parent == null)
                ? name
                : parent.LogicalId + name;
            ResourceName = "@" + LogicalId;
            Reference = reference;
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Scope = scope ?? new string[0];
        }

        //--- Properties ---
        public string Name { get; }
        public string FullName { get; }
        public string ResourceName { get; }
        public string LogicalId { get; }
        public string Description { get; }
        public string Type { get; }
        public IList<string> Scope { get; set; }
        public object Reference { get; set; }
        public bool DiscardIfNotReachable { get; set; }
        public bool HasSecretType => Type == "Secret";
        public bool HasAwsType => ResourceMapping.IsCloudFormationType(Type);
        public bool HasTypeValidation => !HasPragma("no-type-validation");
        public bool IsPublic => Scope.Contains("public");

        //--- Methods ---
        public virtual object GetExportReference() => Reference;
        public virtual bool HasPragma(string pragma) => false;
        public virtual void Visit(ModuleVisitorDelegate visitor) {
            Reference = visitor(this, Reference);
        }
    }

    public class VariableItem : AModuleItem {

        //--- Constructors ---
        public VariableItem(
            AModuleItem parent,
            string name,
            string description,
            string type,
            IList<string> scope,
            object reference
        ) : base(parent, name, description, type, scope, reference) { }
    }

    public class PackageItem : AModuleItem {

        //--- Constructors ---
        public PackageItem(
            AModuleItem parent,
            string name,
            string description,
            IList<string> scope,
            IList<KeyValuePair<string, string>> files
        ) : base(parent, name, description, "String", scope, false) {
            Files = files ?? throw new ArgumentNullException(nameof(files));
        }

        //--- Properties ---
        public IList<KeyValuePair<string, string>> Files { get; }
    }

    public class ParameterItem : AModuleItem {

        //--- Constructors ---
        public ParameterItem(
            AModuleItem parent,
            string name,
            string section,
            string label,
            string description,
            string type,
            IList<string> scope,
            object reference,
            Humidifier.Parameter parameter,
            string import
        ) : base(parent, name, description, type, scope, reference) {
            Section = section ?? "Module Settings";
            Label = label;
            Parameter = parameter;
            Import = import;
        }

        //--- Properties ---
        public string Section { get; }
        public string Label { get; }
        public Humidifier.Parameter Parameter { get; }
        public string Import { get; }
    }

    public abstract class AResourceItem : AModuleItem {

        //--- Constructors ---
        public AResourceItem(
            AModuleItem parent,
            string name,
            string description,
            string type,
            IList<string> scope,
            object reference,
            IList<string> dependsOn,
            string condition,
            IList<object> pragmas
        ) : base(parent, name, description, type, scope, reference) {
            DependsOn = dependsOn ?? new string[0];
            Condition = condition;
            Pragmas = pragmas ?? new object[0];
        }

        //--- Properties ---
        public string Condition { get; set; }
        public IList<string> DependsOn { get; set; }
        public IList<object> Pragmas { get; set; }

        //--- Methods ---
        public override void Visit(ModuleVisitorDelegate visitor) {
            base.Visit(visitor);

            // TODO (2018-11-29, bjorg): we need to make sure that only other resources are referenced (no literal items, or itself, no loops either)
            if(Condition != null) {
                TryGetFnCondition(visitor(this, FnCondition(Condition)), out var result);
                Condition = result ?? throw new InvalidOperationException($"invalid expression returned (condition)");
            }

            // TODO (2018-11-29, bjorg): we need to make sure that only other resources are referenced (no literal items, or itself, no loops either)
            // TODO (2019-0509, bjorg): we need a mechanism to handle dependencies on conditional resources
            for(var i = 0; i < DependsOn.Count; ++i) {
                var dependency = DependsOn[i];
                TryGetFnRef(visitor(this, FnRef(dependency)), out var result);
                DependsOn[i] = result ?? throw new InvalidOperationException($"invalid expression returned (DependsOn[{i}])");
            }
        }

        public override bool HasPragma(string pragma) => Pragmas.Contains(pragma);
    }

    public class ResourceItem : AResourceItem {

        //--- Constructors ---
        public ResourceItem(
            AModuleItem parent,
            string name,
            string description,
            IList<string> scope,
            Humidifier.Resource resource,
            string resourceExportAttribute,
            IList<string> dependsOn,
            string condition,
            IList<object> pragmas
        ) : base(parent, name, description, (resource is Humidifier.CustomResource customResource) ? customResource.OriginalTypeName : resource.AWSTypeName, scope, reference: null, dependsOn, condition, pragmas) {
            Resource = resource ?? throw new ArgumentNullException(nameof(resource));
            ResourceExportAttribute = resourceExportAttribute;
        }

        //--- Properties ---
        public Humidifier.Resource Resource { get; set; }
        public string ResourceExportAttribute { get; set; }

        //--- Methods ---
        public override void Visit(ModuleVisitorDelegate visitor) {
            base.Visit(visitor);
            Resource = (Humidifier.Resource)visitor(this, Resource);
        }

        public override object GetExportReference()
            => (ResourceExportAttribute != null)
                ? FnGetAtt(FullName, ResourceExportAttribute)
                : FnRef(FullName);
    }

    public class FunctionItem : AResourceItem {

        //--- Constructors ---
        public FunctionItem(
            AModuleItem parent,
            string name,
            string description,
            IList<string> scope,
            string project,
            string language,
            IDictionary<string, object> environment,
            IList<AFunctionSource> sources,
            string condition,
            IList<object> pragmas,
            Humidifier.Lambda.Function function

            // TODO (2019-01-05, bjorg): add 'dependsOn'

        ) : base(parent, name, description, function.AWSTypeName, scope, reference: null, dependsOn: null, condition: condition, pragmas) {
            Project = project;
            Language = language;
            Environment = environment;
            Sources = sources ?? new AFunctionSource[0];
            Function = function ?? throw new ArgumentNullException(nameof(function));
            ExportReference = FnGetAtt(FullName, "Arn");
        }

        //--- Properties ---
        public string Project { get; set; }
        public string Language { get; set; }
        public IDictionary<string, object> Environment { get; set; }
        public IList<AFunctionSource> Sources { get; set; }
        public Humidifier.Lambda.Function Function { get; set; }
        public object ExportReference { get; set; }
        public bool HasFunctionRegistration => !HasPragma("no-function-registration");
        public bool HasDeadLetterQueue => !HasPragma("no-dead-letter-queue");
        public bool HasAssemblyValidation => !HasPragma("no-assembly-validation");
        public bool HasHandlerValidation => !HasPragma("no-handler-validation");
        public bool HasWildcardScopedVariables => !HasPragma("no-wildcard-scoped-variables");

        //--- Methods ---
        public override void Visit(ModuleVisitorDelegate visitor) {
            base.Visit(visitor);
            Environment = (IDictionary<string, object>)visitor(this, Environment);
            Function = (Humidifier.Lambda.Function)visitor(this, Function);
            ExportReference = visitor(this, ExportReference);
            foreach(var source in Sources) {
                source.Visit(this, visitor);
            }
        }

        public override object GetExportReference() => ExportReference;
        public override bool HasPragma(string pragma) => Pragmas.Contains(pragma);
    }

    public class ConditionItem : AModuleItem {

        //--- Constructors ---
        public ConditionItem(
            AModuleItem parent,
            string name,
            string description,
            object value
        ) : base(parent, name, description, type: "Condition", scope: null, reference: value) {

            // NOTE (2018-12-19, bjorg): conditionals should be deleted unless used
            DiscardIfNotReachable = true;
        }
    }

    public class MappingItem : AModuleItem {

        //--- Constructors ---
        public MappingItem(
            AModuleItem parent,
            string name,
            string description,
            IDictionary<string, IDictionary<string, string>> value
        ) : base(parent, name, description, type: "Mapping", scope: null, reference: value) {

            // NOTE (2018-12-19, bjorg): mappings should be deleted unless used
            DiscardIfNotReachable = true;
        }

        //--- Properties ---
        public IDictionary<string, IDictionary<string, string>> Mapping => (IDictionary<string, IDictionary<string, string>>)Reference;
    }

    public class ResourceTypeItem : AModuleItem {

        //--- Constructors ---
        public ResourceTypeItem(
            string customResourceType,
            string description,
            string handler
        ) : base(parent: null, customResourceType.ToIdentifier(), description, "String", scope: null, reference: null) {
            CustomResourceType = customResourceType;
            Handler = handler;
        }

        //--- Properties ---
        public string CustomResourceType { get; set; }
        public string Handler { get; set; }

        //--- Methods ---
        public override void Visit(ModuleVisitorDelegate visitor) {
            base.Visit(visitor);
            var result = visitor(this, FnRef(Handler));
            TryGetFnRef(result, out var newHandler);
            Handler = newHandler;
        }
    }
}
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

namespace LambdaSharp.Tool.Model.AST {

    public class ModuleItemNode {

        //--- Class Fields ---
        public static readonly Dictionary<string, IEnumerable<string>> FieldCombinations = new Dictionary<string, IEnumerable<string>> {

            // leaf nodes
            ["Parameter"] = new[] {
                "Allow",
                "AllowedPattern",
                "AllowedValues",
                "ConstraintDescription",
                "Default",
                "DefaultAttribute",
                "Description",
                "EncryptionContext",
                "Label",
                "MaxLength",
                "MaxValue",
                "MinLength",
                "MinValue",
                "NoEcho",
                "Pragmas",
                "Properties",
                "Scope",
                "Section",
                "Type"
            },
            ["Import"] = new[] {
                "Allow",
                "Description",
                "EncryptionContext",
                "Module",
                "Scope",
                "Type"
            },
            ["Condition"] = new[] {
                "Description",
                "Value"
            },
            ["Nested"] = new[] {
                "DependsOn",
                "Description",
                "Module",
                "Parameters"
            },
            ["Function"] = new[] {
                "Description",
                "Environment",
                "Handler",
                "If",
                "Language",
                "Memory",
                "Pragmas",
                "Project",
                "Properties",
                "Runtime",
                "Scope",
                "Sources",
                "Timeout",
                "VPC"
            },
            ["Mapping"] = new[] {
                "Description",
                "Value"
            },
            ["Variable"] = new[] {
                "Description",
                "EncryptionContext",
                "Scope",
                "Type",
                "Value"
            },
            ["Resource"] = new[] {
                "Allow",
                "DefaultAttribute",
                "DependsOn",
                "Description",
                "If",
                "Pragmas",
                "Properties",
                "Scope",
                "Type",
                "Value"
            },
            ["Package"] = new[] {
                "Description",
                "Files",
                "Scope"
            },
            ["ResourceType"] = new[] {
                "Attributes",
                "Description",
                "Handler",
                "Properties"
            },
            ["Macro"] = new[] {
                "Description",
                "Handler"
            },

            // nodes with optional nested items
            ["Group"] = new[] {
                "Items",
                "Description"
            }
        };

        //--- Properties ---

        /*
         * Parameter: string
         * Description: string
         * Section: string
         * Label: string
         * Type: string
         * Scope: string -or- list<string>
         * NoEcho: bool
         * Default: string
         * ConstraintDescription: string
         * AllowedPattern: string
         * AllowedValues: list<string>
         * MaxLength: int
         * MaxValue: int
         * MinLength: int
         * MinValue: int
         * Allow: string or list<string>
         * Properties: map
         * EncryptionContext: map
         * Pragmas: list<any>
         */
        public string Parameter { get; set; }
        public string Section { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public object Scope { get; set; }
        public bool? NoEcho { get; set; }
        public string Default { get; set; }
        public string ConstraintDescription { get; set; }
        public string AllowedPattern { get; set; }
        public IList<string> AllowedValues { get; set; }
        public int? MaxLength { get; set; }
        public int? MaxValue { get; set; }
        public int? MinLength { get; set; }
        public int? MinValue { get; set; }
        public object Allow { get; set; }
        public object Properties { get; set; }
        public IDictionary<string, string> EncryptionContext { get; set; }
        public IList<object> Pragmas { get; set; }

        /*
         * Import: string
         * Description: string
         * Type: string
         * Scope: string -or- list<string>
         * Allow: string or list<string>
         * Module: string
         * EncryptionContext: map
         */
        public string Import { get; set; }
        public string Module { get; set; }

        /*
         * Variable: string
         * Description: string
         * Type: string
         * Scope: string -or- list<string>
         * Value: any
         * EncryptionContext: map
         */
        public string Variable { get; set; }
        public object Value { get; set; }

        /*
         * Group: string
         * Description: string
         * Items: list<Item>
         */
        public string Group { get; set; }
        public IList<ModuleItemNode> Items { get; set; }

        /*
         * Condition: string
         * Description: string
         * Value: any
         */
        public string Condition { get; set; }

        /*
         * Resource: string
         * Description: string
         * If: string -or- expression
         * Type: string
         * Scope: string -or- list<string>
         * Allow: string or list<string>
         * Value: any
         * DependsOn: string -or- list<string>
         * Properties: map
         * DefaultAttribute: string
         * Pragmas: list<any>
         */
        public string Resource { get; set; }
        public object If { get; set; }
        public object DependsOn { get; set; }
        public string DefaultAttribute { get; set; }

        /*
         * Nested: string
         * Description: string
         * Module: string
         * DependsOn: string -or- list<string>
         * Parameters: map
         */
        public string Nested { get; set; }
        public IDictionary<string, object> Parameters { get; set; }

        /*
         * Package: string
         * Description: string
         * Scope: string -or- list<string>
         * Files: string
         */
        public string Package { get; set; }
        public string Files { get; set; }

        /*
         * Function: string
         * Description: string
         * Scope: string -or- list<string>
         * If: string
         * Memory: int
         * Timeout: int
         * Project: string
         * Runtime: string
         * Language: string
         * Handler: string
         * VPC:
         *   SubnetIds: string -or- list<string>
         *   SecurityGroupIds: string -or- list<string>
         * Environment: map
         * Properties: map
         * Sources: list<function-source>
         * Pragmas: list<any>
         */
        public string Function { get; set; }
        public string Memory { get; set; }
        public string Timeout { get; set; }
        public string Project { get; set; }
        public string Runtime { get; set; }
        public string Language { get; set; }
        public Dictionary<string, object> Environment { get; set; }
        public IList<FunctionSourceNode> Sources { get; set; }

        /*
         * Mapping: string
         * Description: string
         * Value: object
         */
         public string Mapping { get; set; }

        /*
         * ResourceType: string
         * Description: string
         * Handler: string
         * Properties: list
         * Attributes: list
         */
        public string ResourceType { get; set; }
        public string Handler { get; set; }
        public object Attributes { get; set; }

        /*
         * Macro: string
         * Description: string
         * Handler: object
         */
        public string Macro { get; set; }
    }
}
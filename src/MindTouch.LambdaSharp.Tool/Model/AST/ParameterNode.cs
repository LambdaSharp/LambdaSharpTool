/*
 * MindTouch λ#
 * Copyright (C) 2018 MindTouch, Inc.
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

using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace MindTouch.LambdaSharp.Tool.Model.AST {

    public class ParameterNode {

        //--- Constructors ---
        public ParameterNode() { }

        public ParameterNode(ParameterNode parameter) {
            Name = parameter.Name;
            Description = parameter.Description;
            Resource = parameter.Resource;
            Secret = parameter.Secret;
            EncryptionContext = parameter.EncryptionContext;
            Values = parameter.Values;
            Value = parameter.Value;
            Import = parameter.Import;
            Package = parameter.Package;
            Export = parameter.Export;
            Parameters = parameter.Parameters;
        }

        //--- Properties ---
        public string Name { get; set; }
        public string Description { get; set; }
        public ResourceNode Resource { get; set; }
        public string Secret { get; set; }
        public IDictionary<string, string> EncryptionContext { get; set; }
        public IList<string> Values { get; set; }
        public string Value { get; set; }
        public string Import { get; set; }
        public PackageNode Package { get; set; }
        public string Export { get; set; }
        public IList<ParameterNode> Parameters { get; set; }
    }

    public class PackageNode {

        //--- Properties ---
        public string Files { get; set; }
        public string Bucket { get; set; }
        public string Prefix { get; set; }

        [YamlIgnore]
        public string PackagePath { get; set; }
    }
}
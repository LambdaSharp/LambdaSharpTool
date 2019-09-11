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

using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace LambdaSharp.Tool.Model.AST {

    public class ModuleNode {

        //--- Properties ---
        public string Module { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public IList<object> Pragmas { get; set; } = new List<object>();
        public IList<string> Secrets { get; set; } = new List<string>();
        public IList<ModuleDependencyNode> Using { get; set; } = new List<ModuleDependencyNode>();
        public IList<ModuleItemNode> Items { get; set; } = new List<ModuleItemNode>();
    }
}
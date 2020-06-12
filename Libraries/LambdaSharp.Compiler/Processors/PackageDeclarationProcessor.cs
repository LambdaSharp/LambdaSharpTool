/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2020
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

using LambdaSharp.Compiler.Syntax.Declarations;

namespace LambdaSharp.Compiler.Processors {

    internal sealed class PackageDeclarationProcessor : AProcessor {

        //--- Constructors ---
        public PackageDeclarationProcessor(IProcessorDependencyProvider provider) : base(provider) { }

        //--- Methods ---
        public void Process(ModuleDeclaration moduleDeclaration) {

            // TODO:


            // // 'Files' reference is relative to YAML file it originated from
            // var workingDirectory = Path.GetDirectoryName(node.SourceLocation.FilePath);
            // var absolutePath = Path.Combine(workingDirectory, node.Files.Value);

            // // determine if 'Files' is a file or a folder
            // if(File.Exists(absolutePath)) {

            //     // TODO: add support for using a single item that has no key
            //     node.ResolvedFiles.Add(new KeyValuePair<string, string>("", absolutePath));
            // } else if(Directory.Exists(absolutePath)) {

            //     // add all files from folder
            //     foreach(var filePath in Directory.GetFiles(absolutePath, "*", SearchOption.AllDirectories)) {
            //         var relativeFilePathName = Path.GetRelativePath(absolutePath, filePath);
            //         node.ResolvedFiles.Add(new KeyValuePair<string, string>(relativeFilePathName, filePath));
            //     }
            //     node.ResolvedFiles = node.ResolvedFiles.OrderBy(kv => kv.Key).ToList();
            // } else {
            //     _builder.Log(Error.FilesAttributeInvalid, node.Files);
            // }

            // // add variable to resolve package location
            // var variable = AddDeclaration(node, new VariableDeclaration(Fn.Literal("PackageName")) {
            //     Type = Fn.Literal("String"),
            //     Value = Fn.Literal($"{node.LogicalId}-DRYRUN.zip")
            // });

            // // set declaration expression
            // node.ReferenceExpression = GetModuleArtifactExpression($"${{{variable.FullName}}}");
        }
    }
}
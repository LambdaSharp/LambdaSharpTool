/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2021
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
using LambdaSharp.Compiler.Syntax.Declarations;
using LambdaSharp.Compiler.Syntax.Expressions;

namespace LambdaSharp.Compiler.SyntaxProcessors {
    using ErrorFunc = Func<string, Error>;

    internal sealed class PackageDeclarationProcessor : ASyntaxProcessor {

        //--- Class Fields ---

        #region Errors/Warnings
        private static readonly ErrorFunc DirectoryInvalid = parameter => new Error($"unrecognized property '{parameter}'");
        public static readonly Error FilesAttributeInvalid = new Error("'Files' attribute must refer to an existing file or folder");
        #endregion

        //--- Constructors ---
        public PackageDeclarationProcessor(ISyntaxProcessorDependencyProvider provider) : base(provider) { }

        //--- Methods ---
        public void ValidateDeclaration(ModuleDeclaration moduleDeclaration) {
            moduleDeclaration.InspectType<PackageDeclaration>(node => {

                // 'Files' reference is relative to YAML file it originated from
                var workingDirectory = Path.GetDirectoryName(node.SourceLocation.FilePath);
                if((workingDirectory == null) || !Directory.Exists(workingDirectory)) {
                    Logger.Log(DirectoryInvalid(workingDirectory ?? "<null>"));
                } else {
                    var absolutePath = Path.Combine(workingDirectory, node.Files.Value);

                    // determine if 'Files' is a file or a folder
                    if(File.Exists(absolutePath)) {

                        // TODO (2021-02-27, bjorg): if file is a .zip file, consider it the final package
                        node.ResolvedFiles.Add(new KeyValuePair<string, string>(Path.GetFileName(absolutePath), absolutePath));
                    } else if(Directory.Exists(absolutePath)) {

                        // add all files from folder
                        node.ResolvedFiles = Directory.GetFiles(absolutePath, "*", SearchOption.AllDirectories)
                            .Select(filePath => new KeyValuePair<string, string>(Path.GetRelativePath(absolutePath, filePath), filePath))
                            .OrderBy(kv => kv.Key)
                            .ToList();;
                    } else {
                        Logger.Log(FilesAttributeInvalid, node.Files);
                    }
                }

                // add variable to resolve package location; it will be set later when the package is built
                Provider.DeclareValueExpression(node.FullName, Fn.Undefined());
            });
        }
    }
}
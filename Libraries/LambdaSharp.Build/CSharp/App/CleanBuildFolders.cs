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

namespace LambdaSharp.Build.CSharp {

    public class CleanBuildFolders : ABuildEventsSource {

        //--- Constructors ---
        public CleanBuildFolders(BuildEventsConfig? buildEventsConfig = null) : base(buildEventsConfig) { }

        //--- Methods ---
        public void Do(IEnumerable<string> files) {
            foreach(var projectFolder in files.Where(file => file.EndsWith(".csproj", StringComparison.Ordinal)).Select(file => Path.GetDirectoryName(file))) {
                if(projectFolder == null) {
                    throw new InvalidOperationException("project folder name cannot be null");
                }
                LogInfoVerbose($"... deleting build folders for {projectFolder}");
                DeleteFolder(Path.Combine(projectFolder, "obj"));
                DeleteFolder(Path.Combine(projectFolder, "bin"));

                // local functions
                void DeleteFolder(string folder) {
                    if(Directory.Exists(folder)) {
                        try {
                            Directory.Delete(folder, recursive: true);
                        } catch {
                            LogWarn($"unable to delete: {folder}");
                        }
                    }
                }
            }
        }
    }
}
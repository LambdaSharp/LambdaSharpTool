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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MindTouch.LambdaSharp.Tool.Model.AST;
using MindTouch.LambdaSharp.Tool.Internal;
using YamlDotNet.Serialization;
using System.IO;
using System.Threading.Tasks;

namespace MindTouch.LambdaSharp.Tool {

    public class ModelSerializer : AModelProcessor {

        //--- Constructors ---
        public ModelSerializer(Settings settings) : base(settings) { }

        //--- Methods ---
        public async Task Process(ModuleNode module) {
            ProcessModule(module);

            // serialize module as YAML file
            var serializer = new SerializerBuilder().Build();
            var yaml = serializer.Serialize(module);
            await File.WriteAllTextAsync(Path.Combine(Settings.OutputDirectory, "Module.yml"), yaml);
        }

        private void ProcessModule(ModuleNode module) {
            ProcessSecrets(module);
            ProcessParameters(module);
            ProcessFunctions(module);
        }

        private void ProcessSecrets(ModuleNode module) {

            // remove empty section
            if(!module.Secrets.Any()) {
                module.Secrets = null;
            }
        }

        private void ProcessParameters(ModuleNode module) {
            foreach(var paramater in module.Parameters.Where(p => p.Package != null)) {

                // files have been packed and uploaded already
                paramater.Package.Files = null;

                // clear package path since it's only used internally
                paramater.Package.PackagePath = null;
            }

            // remove empty section
            if(!module.Parameters.Any()) {
                module.Secrets = null;
            }
       }

        private void ProcessFunctions(ModuleNode module) {
            foreach(var function in module.Functions) {

                // project has been compiled and uploaded already
                function.Project = null;

                // clear package path since it's only used internally
                function.PackagePath = null;
            }

            // remove empty section
            if(!module.Functions.Any()) {
                module.Secrets = null;
            }
       }
    }
}
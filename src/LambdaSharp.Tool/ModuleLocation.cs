/*
 * MindTouch λ#
 * Copyright (C) 2006-2018-2019 MindTouch, Inc.
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

using System.Linq;
using System.Text;

namespace LambdaSharp.Tool {

    public class ModuleLocation {

        //--- Constructors ---
        public ModuleLocation() { }

        public ModuleLocation(string owner, string name, VersionInfo version, string bucketName) {
            ModuleFullName = $"{owner}.{name}";
            ModuleVersion = version;
            ModuleBucketName = bucketName;
        }

        //--- Properties ---
        public string ModuleFullName { get; set; }
        public VersionInfo ModuleVersion { get; set; }
        public string ModuleBucketName { get; set; }
        public string TemplatePath { get; set; }

        //--- Methods ---
        public override string ToString() {
            var result = new StringBuilder();
            if(ModuleFullName != null) {
                result.Append(ModuleFullName);
                if(ModuleVersion != null) {
                    result.Append($" (v{ModuleVersion})");
                }
                result.Append(" from ");
                result.Append(ModuleBucketName);
            } else {
                result.Append($"s3://{ModuleBucketName}/{TemplatePath}");
            }
            return result.ToString();
        }

        public string ToModuleReference() {
            var result = new StringBuilder();
            result.Append(ModuleFullName);
            result.Append(":");
            if(ModuleVersion != null) {
                result.Append(ModuleVersion);
            } else {
                result.Append("*");
            }
            if(ModuleBucketName != null) {
                result.Append("@");
                result.Append(ModuleBucketName);
            }
            return result.ToString();
        }
    }
}
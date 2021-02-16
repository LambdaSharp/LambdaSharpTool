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

namespace LambdaSharp.Modules {

    public class ModuleLocation {

        //--- Fields ---
        public readonly string SourceBucketName;
        public readonly ModuleInfo ModuleInfo;
        public readonly string Hash;

        //--- Properties ---
        public string ModuleTemplateUrl => $"https://{SourceBucketName}.s3.amazonaws.com/{ModuleTemplateKey}";
        public string ModuleTemplateKey => ModuleInfo.GetArtifactPath($"cloudformation_{ModuleInfo.FullName}_{Hash}.json");

        //--- Constructors ---
        public ModuleLocation(string sourceBucketName, ModuleInfo moduleInfo, string hash) {
            SourceBucketName = sourceBucketName ?? throw new ArgumentNullException(nameof(sourceBucketName));
            ModuleInfo = moduleInfo ?? throw new ArgumentNullException(nameof(moduleInfo));
            Hash = hash ?? throw new ArgumentNullException(nameof(hash));
        }
    }
}
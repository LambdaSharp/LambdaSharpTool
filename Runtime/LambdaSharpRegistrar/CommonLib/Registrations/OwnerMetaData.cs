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

namespace MindTouch.LambdaSharpRegistrar.Registrations {
    public class OwnerMetaData {

        //--- Properties ---
        public string ModuleId { get; set; }
        public string ModuleName { get; set; }
        public string ModuleVersion { get; set; }
        public string FunctionId { get; set; }
        public string FunctionName { get; set; }
        public string FunctionLogGroupName { get; set; }
        public string FunctionPlatform { get; set; }
        public string FunctionFramework { get; set; }
        public string FunctionLanguage { get; set; }
        public int FunctionMaxMemory { get; set; }
        public TimeSpan FunctionMaxDuration { get; set; }
        public int RollbarProjectId { get; set; }
        public string RollbarAccessToken { get; set; }
    }
}

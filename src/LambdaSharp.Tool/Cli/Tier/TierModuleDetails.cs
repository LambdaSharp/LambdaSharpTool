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
using System.Text;
using Amazon.CloudFormation.Model;

namespace LambdaSharp.Tool.Cli.Tier {

    internal class TierModuleDetails {

        //--- Properties ---
        public string StackName { get; set; }
        public string ModuleDeploymentName { get; set; }
        public string StackStatus { get; set; }
        public DateTime DeploymentDate { get; set; }
        public Stack Stack { get; set; }
        public string ModuleReference { get; set; }
        public string CoreServices { get; set; }
        public bool IsRoot { get; set; }
        public bool HasDefaultSecretKeyParameter { get; set; }
    }
}

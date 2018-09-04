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

namespace MindTouch.LambdaSharp.Tool.Model {

    public class Function {

        //--- Properties ---
        public string Name { get; set; }
        public string Description { get; set; }
        public IList<AFunctionSource> Sources { get; set; }
        public string S3Location { get; set; }
        public string Handler { get; set; }
        public string Runtime { get; set; }
        public string Memory { get; set; }
        public string Timeout { get; set; }
        public string ReservedConcurrency { get; set; }
        public FunctionVpc VPC;
        public Dictionary<string, string> Environment { get; set; }
        public string Export { get; set; }
        public string PackagePath { get; set; }
   }

   public class FunctionVpc {

       //--- Properties ---
       public object SubnetIds { get; set; }
       public object SecurityGroupIds { get; set; }
   }
}
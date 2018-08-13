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
    
    public abstract class AParameter {

        //--- Properties ---
        public string Name { get; set; }
        public string Description { get; set; }
        public string Export { get; set; }
    }

    public class SecretParameter : AParameter {

        //--- Properties ---
        public object Secret { get; set; }
        public Dictionary<string, string> EncryptionContext { get; set; }
    }

    public class CollectionParameter : AParameter {

        //--- Properties ---
        public IList<AParameter> Parameters { get; set; }
    }

    public class StringParameter : AParameter {

        //--- Properties ---
        public string Value { get; set; }
    }

    public class PackageParameter : AParameter {

        //--- Properties ---
        public string Package { get; set; }
        public string Bucket { get; set; }
        public string PackageS3Key { get; set; }
        public string Prefix { get; set; }
    }

    public abstract class AResourceParameter : AParameter {

        //--- Properties ---
        public Resource Resource { get; set; }
    }

    public class ReferencedResourceParameter : AResourceParameter { }

    public class CloudFormationResourceParameter : AResourceParameter { }
}
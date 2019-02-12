/*
 * MindTouch λ#
 * Copyright (C) 2018-2019 MindTouch, Inc.
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

namespace LambdaSharp {

    public abstract class ALambdaConfigException : Exception {

        //--- Constructors ---
        protected ALambdaConfigException(string message) : base(message) { }
        protected ALambdaConfigException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class LambdaConfigUnexpectedException : ALambdaConfigException {

        //--- Constructors ---
        public LambdaConfigUnexpectedException(string path, string reason, Exception innerException) : base($"unexpected error accessing: '{path}' ({reason})", innerException) { }
    }

    public class LambdaConfigIllegalKeyException : ALambdaConfigException {

        //--- Constructors ---
        public LambdaConfigIllegalKeyException(string key) : base($"config key must be alphanumeric: '{key}'") { }
    }

    public class LambdaConfigMissingKeyException : ALambdaConfigException {

        //--- Constructors ---
        public LambdaConfigMissingKeyException(string path) : base($"missing value for config key: '{path}'") { }
    }

    public class LambdaConfigBadValueException : ALambdaConfigException {

        //--- Constructors ---
        public LambdaConfigBadValueException(string path, Exception innerException) : base($"error while validating config key value: '{path}'", innerException) { }
    }
}

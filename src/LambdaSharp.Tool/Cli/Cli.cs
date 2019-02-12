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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LambdaSharp.Tool.Cli {


    public class CliBase {

        //--- Class Fields ---
        private static VersionInfo _version;

        //--- Class Constructor ---
        static CliBase() {
            _version = VersionInfo.Parse(typeof(CliBase).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion);
        }

        //--- Class Properties ---
        protected static VersionInfo Version => _version;
        protected static bool HasErrors => Settings.HasErrors;

        //--- Class Methods ---
        protected static void AddWarning(string message)
            => Settings.AddWarning(message);

        protected static void AddError(string message, Exception exception = null)
            => Settings.AddError(message, exception);

        protected static void AddError(Exception exception)
            => Settings.AddError(exception);
    }
}

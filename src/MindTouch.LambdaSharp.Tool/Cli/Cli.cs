/*
 * MindTouch λ#
 * Copyright (C) 2006-2018 MindTouch, Inc.
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

namespace MindTouch.LambdaSharp.Tool.Cli {


    public class CliBase {

        //--- Class Fields ---
        private static Version _version;

        //--- Class Constructor ---
        static CliBase() {
            var version = FullVersion;
            if(version.Build != 0) {
                _version = new Version(version.Major, version.Minor, version.Build);
            } else {
                _version = new Version(version.Major, version.Minor);
            }
        }

        //--- Class Properties ---
        protected static Version FullVersion => typeof(CliBase).Assembly.GetName().Version;
        protected static string VersionPrefixAndSuffix => typeof(CliBase).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
        protected static Version Version => _version;
        protected static bool HasErrors => Settings.HasErrors;

        //--- Class Methods ---
        protected static void AddError(string message, Exception exception = null)
            => Settings.AddError(message, exception);

        protected static void AddError(Exception exception)
            => Settings.AddError(exception);
    }
}

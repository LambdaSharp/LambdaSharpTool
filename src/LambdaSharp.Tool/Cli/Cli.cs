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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LambdaSharp.Tool.Cli {


    public class CliBase {

        //--- Class Fields ---
        private static VersionInfo _version;

        //--- Class Constructor ---
        static CliBase() {

            // initialize from assembly build version
            _version = VersionInfo.Parse(typeof(CliBase).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion);
        }

        //--- Class Properties ---
        protected static VersionInfo Version => _version;
        protected static bool HasErrors => Settings.HasErrors;

        //--- Class Methods ---
        protected static void LogWarn(string message)
            => Settings.LogWarn(message);

        protected static void LogError(string message, Exception exception = null)
            => Settings.LogError(message, exception);

        protected static void LogError(Exception exception)
            => Settings.LogError(exception);
    }
}

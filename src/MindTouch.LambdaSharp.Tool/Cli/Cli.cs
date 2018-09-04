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

namespace MindTouch.LambdaSharp.Tool.Cli {


    public class CliBase {

        //--- Class Fields ---
        protected static VerboseLevel _verboseLevel = VerboseLevel.Normal;
        private static IList<(string Message, Exception Exception)> _errors = new List<(string Message, Exception Exception)>();
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
        protected static int ErrorCount => _errors.Count;
        protected static bool HasErrors => ErrorCount > 0;
        protected static Version FullVersion => typeof(Program).Assembly.GetName().Version;
        protected static Version Version => _version;

        //--- Class Methods ---
        protected static void ShowErrors() {
            foreach(var error in _errors) {
                if((error.Exception != null) && (_verboseLevel >= VerboseLevel.Exceptions)) {
                    Console.WriteLine("ERROR: " + error.Message + Environment.NewLine + error.Exception);
                } else {
                    Console.WriteLine("ERROR: " + error.Message);
                }
            }
            var setupException = _errors.Select(error => error.Exception).OfType<LambdaSharpDeploymentTierSetupException>().FirstOrDefault();
            if(setupException != null) {
                Console.WriteLine();
                Console.WriteLine($"IMPORTANT: complete the LambdaSharp Environment bootstrap procedure for deployment tier '{setupException.Tier}'");
            }
        }

        protected static void AddError(string message, Exception exception = null)
            => _errors.Add((Message: message, Exception: exception));

        protected static void AddError(Exception exception)
            => AddError(exception.Message, exception);
    }
}

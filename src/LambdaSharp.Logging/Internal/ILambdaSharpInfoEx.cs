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

using System.Text.RegularExpressions;

namespace LambdaSharp.Logging.Internal {

    internal static class ILambdaSharpInfoEx {

        //--- Class Fields ---
        private static readonly Regex ModuleKeyPattern = new Regex(@"^(?<Namespace>\w+)\.(?<Name>[\w\.]+)(:(?<Version>\*|[\w\.\-]+))?(@(?<Origin>[\w\-%]+))?$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        //--- Class Methods ---
        private static void ParseModuleInfoString(string moduleInfo, out string moduleNamespace, out string moduleName, out string moduleVersion, out string moduleOrigin) {

            // try parsing module reference
            var match = ModuleKeyPattern.Match(moduleInfo);
            if(match.Success) {
                moduleNamespace = GetMatchValue("Namespace");
                moduleName = GetMatchValue("Name");
                moduleOrigin = GetMatchValue("Origin");
                moduleVersion = GetMatchValue("Version");
            } else {
                moduleNamespace = null;
                moduleName = null;
                moduleVersion = null;
                moduleOrigin = null;
            }

            // local function
            string GetMatchValue(string groupName) {
                var group = match.Groups[groupName];
                return group.Success ? group.Value : null;
            }
        }

        //--- Extension Methods ---
        public static string GetModuleFullName(this ILambdaSharpInfo info) {
            ParseModuleInfoString(info.ModuleInfo, out var moduleNamespace, out var moduleName, out var _, out var _);
            return moduleNamespace + "." + moduleName;
        }

        public static string GetModuleNamespace(this ILambdaSharpInfo info) {
            ParseModuleInfoString(info.ModuleInfo, out var moduleNamespace, out var _, out var _, out var _);
            return moduleNamespace;
        }

        public static string GetModuleName(this ILambdaSharpInfo info) {
            ParseModuleInfoString(info.ModuleInfo, out var _, out var moduleName, out var _, out var _);
            return moduleName;
        }

        public static string GetModuleVersion(this ILambdaSharpInfo info) {
            ParseModuleInfoString(info.ModuleInfo, out var _, out var _, out var moduleVersion, out var _);
            return moduleVersion;
        }

        public static string GetModuleOrigin(this ILambdaSharpInfo info) {
            ParseModuleInfoString(info.ModuleInfo, out var _, out var _, out var _, out var moduleOrigin);
            return moduleOrigin;
        }
    }
}

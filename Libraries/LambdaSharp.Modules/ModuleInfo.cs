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

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using LambdaSharp.Modules.Serialization;

namespace LambdaSharp.Modules {

    [JsonConverter(typeof(JsonModuleInfoConverter))]
    [Newtonsoft.Json.JsonConverter(typeof(ModuleInfoConverter))]
    public class ModuleInfo {

        // NOTE: module reference formats:
        // * Namespace.Name
        // * Namespace.Name:*
        // * Namespace.Name:Version
        // * Namespace.Name@Origin
        // * Namespace.Name:*@Bucket
        // * Namespace.Name:Version@Bucket
        // * Namespace.Name:Version@<%MODULE_ORIGIN%>

        //--- Constants ---
        public const string MODULE_ORIGIN_PLACEHOLDER = "<%MODULE_ORIGIN%>";

        //--- Class Fields ---
        private static readonly Regex ModuleKeyPattern = new Regex(@"^(?<Namespace>\w+)\.(?<Name>[\w\.]+)(:(?<Version>\*|[\w\.\-]+))?(@(?<Origin>[\w\-%]+))?$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        //--- Class Methods ---
        public static ModuleInfo Parse(string moduleReference) {
            if(TryParse(moduleReference, out var result)) {
                return result;
            }
            throw new FormatException("Input string was not in a correct format.");
        }

        public static bool TryParse(string moduleReference, [NotNullWhen(true)] out ModuleInfo? moduleInfo) {
            if(moduleReference == null) {
                moduleInfo = null;
                return false;
            }

            // try parsing module reference
            var match = ModuleKeyPattern.Match(moduleReference);
            if(!match.Success) {
                moduleInfo = null;
                return false;
            }
            var ns = GetMatchValue("Namespace");
            var name = GetMatchValue("Name");
            if((ns == null) || (name == null)) {
                moduleInfo = null;
                return false;
            }
            var origin = GetMatchValue("Origin");

            // parse optional version
            var versionText = GetMatchValue("Version");
            var version = ((versionText != null) && (versionText != "*"))
                ? VersionInfo.Parse(versionText)
                : null;
            moduleInfo = new ModuleInfo(ns, name, version, origin);
            return true;

            // local function
            string? GetMatchValue(string groupName) {
                var group = match.Groups[groupName];
                return group.Success ? group.Value : null;
            }
        }

        //--- Constructors ---
        public ModuleInfo(string ns, string name, VersionInfo? version, string? origin) {
            Namespace = ns ?? throw new ArgumentNullException(nameof(ns));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            FullName = $"{Namespace}.{Name}";
            Version = version;
            Origin = origin;
        }

        //--- Properties ---
        public string Namespace { get; }
        public string Name { get; }
        public VersionInfo? Version { get; }
        public string? Origin { get; }
        public string FullName { get; }
        public string VersionPath => $"{Origin ?? throw new ApplicationException("missing Origin information")}/{Namespace}/{Name}/{Version ?? throw new ApplicationException("missing Version information")}";

        //--- Methods ---
        public string GetArtifactPath(string artifactName) => $"{Origin ?? MODULE_ORIGIN_PLACEHOLDER}/{Namespace}/{Name}/.artifacts/{artifactName}";

        public string ToPrettyString() {
            var result = new StringBuilder();
            result.Append(FullName);
            if(Version != null) {
                result.Append($" (v{Version})");
            }
            if(Origin != null) {
                result.Append(" from ");
                result.Append(Origin);
            }
            return result.ToString();
        }

        public override string ToString() {
            var result = new StringBuilder();
            result.Append(FullName);
            if(Version != null) {
                result.Append(":");
                result.Append(Version);
            }
            if(Origin != null) {
                result.Append("@");
                result.Append(Origin);
            }
            return result.ToString();
        }

        public ModuleInfo WithoutVersion() => new ModuleInfo(Namespace, Name, version: null, Origin);
        public ModuleInfo WithVersion(VersionInfo version) => new ModuleInfo(Namespace, Name, version ?? throw new ArgumentNullException(nameof(version)), Origin);
        public ModuleInfo WithoutOrigin() => new ModuleInfo(Namespace, Name, Version, origin: null);
        public ModuleInfo WithOrigin(string origin) => new ModuleInfo(Namespace, Name, Version, origin);
    }
}
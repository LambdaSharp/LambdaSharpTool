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
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace LambdaSharp.Tool {
    using static ModelFunctions;

    public class ModuleLocation {

        //--- Fields ---
        public readonly string SourceBucketName;
        public readonly ModuleInfo ModuleInfo;
        public readonly string Hash;

        //--- Properties ---
        public string ModuleTemplateUrl => $"https://{SourceBucketName}.s3.amazonaws.com/{ModuleTemplateKey}";
        public string ModuleTemplateKey => ModuleInfo.GetAssetPath($"cloudformation_{ModuleInfo.FullName}_{Hash}.json");

        //--- Constructors ---
        public ModuleLocation(string sourceBucketName, ModuleInfo moduleInfo, string hash) {
            SourceBucketName = sourceBucketName ?? throw new ArgumentNullException(nameof(sourceBucketName));
            ModuleInfo = moduleInfo ?? throw new ArgumentNullException(nameof(moduleInfo));
            Hash = hash ?? throw new ArgumentNullException(nameof(hash));
        }
    }

    [JsonConverter(typeof(ModuleInfoConverter))]
    public class ModuleInfo {

        // NOTE: module reference formats:
        // * Owner.Name
        // * Owner.Name:*
        // * Owner.Name:Version
        // * Owner.Name@Origin
        // * Owner.Name:*@Bucket
        // * Owner.Name:Version@Bucket
        // * Owner.Name:Version@<%MODULE_ORIGIN%>

        //--- Constants ---

        // TODO: could this be replaced with `Settings.DeploymentBucketName` in most cases?
        public const string MODULE_ORIGIN_PLACEHOLDER = "<%MODULE_ORIGIN%>";

        //--- Class Fields ---
        private static readonly Regex ModuleKeyPattern = new Regex(@"^(?<Owner>\w+)\.(?<Name>[\w\.]+)(:(?<Version>\*|[\w\.\-]+))?(@(?<Origin>[\w\-%]+))?$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        //--- Class Methods ---

        // TODO: can we get rid of this?
        public static object GetModuleAssetExpression(string filename) => FnSub($"{MODULE_ORIGIN_PLACEHOLDER}/${{Module::Owner}}/${{Module::Name}}/.assets/{filename}");

        public static ModuleInfo Parse(string moduleReference) {
            if(TryParse(moduleReference, out var result)) {
                return result;
            }
            throw new FormatException("Input string was not in a correct format.");
        }

        public static bool TryParse(string moduleReference, out ModuleInfo moduleInfo) {
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
            var owner = GetMatchValue("Owner");
            var name = GetMatchValue("Name");
            var origin = GetMatchValue("Origin");

            // parse optional version
            var versionText = GetMatchValue("Version");
            var version = ((versionText != null) && (versionText != "*"))
                ? VersionInfo.Parse(versionText)
                : null;
            moduleInfo = new ModuleInfo(owner, name, version, origin);
            return true;

            // local function
            string GetMatchValue(string groupName) {
                var group = match.Groups[groupName];
                return group.Success ? group.Value : null;
            }
        }

        //--- Fields ---
        public readonly string Owner;
        public readonly string Name;
        public readonly VersionInfo Version;
        public readonly string Origin;
        public readonly string FullName;

        //--- Constructors ---
        public ModuleInfo(string owner, string name, VersionInfo version, string origin) {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            FullName = $"{Owner}.{Name}";
            Version = version;
            Origin = origin;
        }

        //--- Properties ---
        public string VersionPath => $"{Origin ?? throw new ApplicationException("missing Origin information")}/{Owner}/{Name}/{Version ?? throw new ApplicationException("missing Version information")}";

        //--- Methods ---
        public string GetAssetPath(string assetName) => $"{Origin ?? MODULE_ORIGIN_PLACEHOLDER}/{Owner}/{Name}/.assets/{assetName}";

        // TODO: make this the `ToString()` method
        public string ToModuleReference() {
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

        public override string ToString() {
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

        public ModuleInfo WithoutVersion() => new ModuleInfo(Owner, Name, version: null, Origin);
        public ModuleInfo WithVersion(VersionInfo version) => new ModuleInfo(Owner, Name, version ?? throw new ArgumentNullException(nameof(version)), Origin);
        public ModuleInfo WithoutOrigin() => new ModuleInfo(Owner, Name, Version, origin: null);
        public ModuleInfo WithOrigin(string origin) => new ModuleInfo(Owner, Name, Version, origin);
    }

    public class ModuleInfoConverter : JsonConverter {

        //--- Methods ---
        public override bool CanConvert(Type objectType)
            => objectType == typeof(ModuleInfo);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            => (reader.Value != null)
                ? ModuleInfo.Parse((string)reader.Value)
                : null;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            => writer.WriteValue(((ModuleInfo)value).ToModuleReference());
    }
}
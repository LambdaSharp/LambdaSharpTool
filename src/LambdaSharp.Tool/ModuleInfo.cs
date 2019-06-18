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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LambdaSharp.Tool {
    using static ModelFunctions;

    public class ModuleLocation {

        //--- Fields ---
        public readonly string SourceBucketName;
        public readonly ModuleInfo ModuleInfo;

        //--- Properties ---
        public string S3Url => $"s3://{SourceBucketName}/{ModuleInfo.TemplatePath}";

        //--- Constructors ---
        public ModuleLocation(string sourceBucketName, ModuleInfo moduleInfo) {
            SourceBucketName = sourceBucketName ?? throw new ArgumentNullException(nameof(sourceBucketName));
            ModuleInfo = moduleInfo ?? throw new ArgumentNullException(nameof(moduleInfo));
        }
    }

    public class ModuleInfo {

        // NOTE: module reference formats:
        // * Owner.Name
        // * Owner.Name:*
        // * Owner.Name:Version
        // * Owner.Name@Origin
        // * Owner.Name:*@Bucket
        // * Owner.Name:Version@Bucket
        // * s3://{Origin}/{Owner}/Modules/{Name}/Versions/{Version}/
        // * s3://{Origin}/{Owner}/Modules/{Name}/Versions/{Version}/cloudformation.json

        //--- Class Fields ---
        private static readonly Regex ModuleKeyPattern = new Regex(@"^(?<Owner>\w+)\.(?<Name>[\w\.]+)(:(?<Version>\*|[\w\.\-]+))?(@(?<Origin>[\w\-]+))?$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        //--- Class Methods ---
        public static object GetModuleAssetExpression(string filename) => FnSub($"%%MODULEORIGIN%%/${{Module::Owner}}/Modules/${{Module::Name}}/Assets/{filename}");
        public static string GetModuleVersionsBucketPrefix(string moduleOwner, string moduleName, string moduleOrigin) => $"{moduleOrigin}/{moduleOwner}/Modules/{moduleName}/Versions/";

        public static bool TryParse(string moduleReference, out ModuleInfo moduleInfo) {
            string owner;
            string name;
            VersionInfo version;
            string origin;
            if(moduleReference == null) {
                moduleInfo = null;
                return false;
            }

            // check if module reference is given in S3 URI format
            if(moduleReference.StartsWith("s3://", StringComparison.Ordinal)) {
                var uri = new Uri(moduleReference);

                // absolute path always starts with '/', which needs to be removed
                var pathSegments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();
                if((pathSegments.Count < 5) || (pathSegments[1] != "Modules") || (pathSegments[3] != "Versions")) {
                    moduleInfo = null;
                    return false;
                }
                owner = pathSegments[0];
                name = pathSegments[2];
                version = VersionInfo.Parse(pathSegments[4]);
                origin = uri.Host;
            } else {

                // try parsing module reference
                var match = ModuleKeyPattern.Match(moduleReference);
                if(!match.Success) {
                    moduleInfo = null;
                    return false;
                }
                owner = GetMatchValue("Owner");
                name = GetMatchValue("Name");
                origin = GetMatchValue("Origin");

                // parse optional version
                var versionText = GetMatchValue("Version");
                version = ((versionText != null) && (versionText != "*"))
                    ? VersionInfo.Parse(versionText)
                    : null;

                // local function
                string GetMatchValue(string groupName) {
                    var group = match.Groups[groupName];
                    return group.Success ? group.Value : null;
                }
            }
            moduleInfo = new ModuleInfo(owner, name, version, origin);
            return true;

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
        public string TemplatePath => $"{Origin ?? throw new ApplicationException("missing Origin information")}/{Owner}/Modules/{Name}/Versions/{Version ?? throw new ApplicationException("missing Version information")}/cloudformation.json";

        //--- Methods ---
        public string GetAssetPath(string filename) => $"{Origin ?? throw new ApplicationException("missing Origin information")}/{Owner}/Modules/{Name}/Assets/{filename}";

        public object GetTemplateUrlExpression() {

            // TODO: this should now always come from the 'DeploymentBucket'

            // TODO (2019-05-09, bjorg); path-style S3 bucket references will be deprecated in September 30th 2020
            //  see https://aws.amazon.com/blogs/aws/amazon-s3-path-deprecation-plan-the-rest-of-the-story/
            return FnSub("https://s3.amazonaws.com/${ModuleOrigin}/${ModuleOrigin}/${ModuleOwner}/Modules/${ModuleName}/Versions/${ModuleVersion}/cloudformation.json", new Dictionary<string, object> {
                ["ModuleOwner"] = Owner,
                ["ModuleName"] = Name,
                ["ModuleVersion"] = Version.ToString(),

                // TODO: shouldn't 'Origin' always be set?
                ["ModuleOrigin"] = Origin /*  ?? FnRef("DeploymentBucketName") */
            });
        }

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
    }
}
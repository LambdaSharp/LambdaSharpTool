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
using Newtonsoft.Json;

namespace LambdaSharp.Tool {

    [JsonConverter(typeof(VersionInfoConverter))]
    public class VersionInfo {

        //--- Class Methods ---
        public static VersionInfo Parse(string text) {
            var index = text.IndexOf('-');
            if(index < 0) {
                return new VersionInfo(Version.Parse(text), "");
            } else {
                return new VersionInfo(Version.Parse(text.Substring(0, index)), text.Substring(index).TrimEnd('*'));
            }
        }

        public static bool TryParse(string text, out VersionInfo version) {
            var index = text.IndexOf('-');
            string prefix;
            string suffix;
            if(index < 0) {
                prefix = text;
                suffix = "";
            } else {
                prefix = text.Substring(0, index);
                suffix = text.Substring(index).TrimEnd('*');
            }
            if(!Version.TryParse(prefix, out var prefixVersion)) {
                version = null;
                return false;
            }
            version = new VersionInfo(prefixVersion, suffix);
            return true;
        }

        //--- Fields ---
        public readonly Version Version;
        public readonly string Suffix;

        //--- Constructors ---
        private VersionInfo(Version version, string suffix) {
            Version = version ?? throw new ArgumentNullException(nameof(version));
            Suffix = suffix ?? throw new ArgumentNullException(nameof(suffix));
        }

        //--- Properties ---
        public int Major => Version.Major;
        public int Minor => Version.Minor;
        public bool IsPreRelease => Suffix.Length > 0;

        //--- Methods ---
        public override string ToString() => Version.ToString() + Suffix;
        public override int GetHashCode() => Version.GetHashCode() ^ Suffix.GetHashCode();

        public bool IsAssemblyCompatibleWith(VersionInfo other) {
            if(Suffix != other.Suffix) {
                return false;
            }
            if(Major != other.Major) {
                return false;
            }
            if(Version.Major != 0) {
                return Minor == other.Minor;
            }

            // when Major version is 0, we rely on Minor and Build to match
            return ((Minor == other.Minor) && (Math.Max(0, Version.Build) == Math.Max(0, other.Version.Build)));
        }

        public int? CompareToVersion(VersionInfo other) {
            if(object.ReferenceEquals(other, null)) {
                return null;
            }

            // version number dominates other comparisions
            var result = Version.CompareTo(other.Version);
            if(result != 0) {
                return result;
            }

            // a suffix indicates a pre-release version, which is always less than the stable version
            if((Suffix == "") && (other.Suffix != "")) {
                return 1;
            }
            if((Suffix != "") && (other.Suffix == "")) {
                return -1;
            }
            if(Suffix == other.Suffix) {
                return 0;
            }

            // check if the suffixes have a trailing number that can be compared
            var shortestLength = Math.Min(Suffix.Length, other.Suffix.Length);
            var i = 1;
            for(; (i < shortestLength) && char.IsLetter(Suffix[i]) && (Suffix[i] == other.Suffix[i]); ++i);
            if(
                int.TryParse(Suffix.Substring(i, Suffix.Length - i), out var leftSuffixValue)
                && int.TryParse(other.Suffix.Substring(i, other.Suffix.Length - i), out var rightSuffixVersion)
            ) {
                if(leftSuffixValue > rightSuffixVersion) {
                    return 1;
                }
                if(leftSuffixValue < rightSuffixVersion) {
                    return -1;
                }
                return 0;
            }

            // versions cannot be compared
            return null;
        }

        public bool IsLessThanVersion(VersionInfo info) => CompareToVersion(info) < 0;
        public bool IsLessOrEqualThanVersion(VersionInfo info) => CompareToVersion(info) <= 0;
        public bool IsGreaterThanVersion(VersionInfo info) => CompareToVersion(info) > 0;
        public bool IsGreaterOrEqualThanVersion(VersionInfo info) => CompareToVersion(info) >= 0;
        public bool IsEqualToVersion(VersionInfo info) => CompareToVersion(info) == 0;

        public string GetWildcardVersion() {
            if(IsPreRelease) {

                // NOTE (2018-12-16, bjorg): for pre-release version, there is no wildcard; the version must match everything
                return ToString();
            }
            if(Major == 0) {

                // when Major version is 0, the build number is relevant
                return $"{Major}.{Minor}.{Math.Max(0, Version.Build)}.*";
            }
            return $"{Major}.{Minor}.*";
        }

        public VersionInfo GetCompatibleCoreServicesVersion() {
            if(IsPreRelease) {

                // NOTE (2019-02-19, bjorg): for pre-release version, the base version is this version
                return this;
            }
            if((Major == 0) && (Version.Build >= 0)) {

                // when Major version is 0, the build number is relevant
                return new VersionInfo(new Version(Major, Minor, Version.Build), suffix: "");
            }
            return new VersionInfo(new Version(Major, Minor), suffix: "");
        }

        public bool MatchesConstraints(VersionInfo minVersion, VersionInfo maxVersion) {

            // check if min-max versions are the same; which indicates a tight version match
            if((minVersion != null) && (maxVersion != null) && (minVersion.CompareToVersion(maxVersion) == 0)) {
                return (Major == minVersion.Major)
                    && (Minor == minVersion.Minor)
                    && (Suffix == minVersion.Suffix)
                    && ((minVersion.Version.Build == -1) || (minVersion.Version.Build == Version.Build))
                    && ((minVersion.Version.Revision == -1) || (minVersion.Version.Revision == Version.Revision));
            }
            return ((minVersion == null) || IsGreaterOrEqualThanVersion(minVersion))
                && ((maxVersion == null) || IsLessThanVersion(maxVersion));
        }
    }

    public class VersionInfoConverter : JsonConverter {

        //--- Methods ---
        public override bool CanConvert(Type objectType)
            => objectType == typeof(VersionInfo);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            => (reader.Value != null)
                ? VersionInfo.Parse((string)reader.Value)
                : null;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            => writer.WriteValue(value.ToString());
    }
}
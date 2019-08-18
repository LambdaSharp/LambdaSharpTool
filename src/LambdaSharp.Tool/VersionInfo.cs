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
using System.Text;
using Newtonsoft.Json;

namespace LambdaSharp.Tool {

    [JsonConverter(typeof(VersionInfoConverter))]
    public class VersionInfo {

        //--- Class Methods ---
        public static VersionInfo Parse(string text) {
            var index = text.IndexOf('-');
            Version version;
            string suffix;
            if(index < 0) {
                version = Version.Parse(text);
                suffix = "";
            } else {
                version = Version.Parse(text.Substring(0, index));
                suffix = text.Substring(index).TrimEnd('*');
            }
            return new VersionInfo(
                version.Major,
                version.Minor,
                (version.Build != -1) ? (int?)version.Build : null,
                (version.Revision != -1) ? (int?)version.Revision : null,
                suffix
            );
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
            version = new VersionInfo(
                prefixVersion.Major,
                prefixVersion.Minor,
                (prefixVersion.Build != -1) ? (int?)prefixVersion.Build : null,
                (prefixVersion.Revision != -1) ? (int?)prefixVersion.Revision : null,
                suffix
            );
            return true;
        }

        public static VersionInfo Max(IEnumerable<VersionInfo> versionInfos, bool strict = false) {
            if(!versionInfos.Any()) {
                return null;
            }
            var result = versionInfos.First();
            foreach(var current in versionInfos.Skip(1)) {
                if(current.IsGreaterThanVersion(result, strict)) {
                    result = current;
                }
            }
            return result;
        }

        public static VersionInfo FindLatestMatchingVersion(IEnumerable<VersionInfo> versionInfos, VersionInfo minVersion, Predicate<VersionInfo> validate) {
            var candidates = new List<VersionInfo>(versionInfos);
            while(candidates.Any()) {

                // find latest version
                var candidate = VersionInfo.Max(candidates);
                candidates.Remove(candidate);

                // check if latest version meets minimum version constraint
                if(minVersion?.IsGreaterThanVersion(candidate) ?? false) {
                    continue;
                }

                // validate candidate
                if(validate?.Invoke(candidate) ?? true) {
                    return candidate;
                }
            }

            // not match found
            return null;
        }

        //--- Fields ---
        public readonly int Major;
        public readonly int Minor;
        public readonly int? Patch;
        public readonly int? PatchRevision;
        public readonly string Suffix;

        //--- Constructors ---
        private VersionInfo(int major, int minor, int? patch, int? patchMinor, string suffix) {
            Major = major;
            Minor = minor;
            Patch = patch;
            PatchRevision = patchMinor;
            Suffix = suffix ?? throw new ArgumentNullException(nameof(suffix));
            if(PatchRevision.HasValue && !Patch.HasValue) {
                throw new ArgumentException($"{nameof(Patch)} must have a value when {nameof(PatchRevision)} has a value");
            }
        }

        //--- Properties ---
        public bool IsPreRelease => Suffix.Length > 0;
        public bool HasFloatingConstraints => !Patch.HasValue || !PatchRevision.HasValue;

        //--- Methods ---
        public override string ToString() {
            var result = new StringBuilder();
            result.Append(Major);
            result.Append('.');
            result.Append(Minor);
            if(PatchRevision.HasValue) {
                result.Append('.');
                result.Append(Patch);
                result.Append('.');
                result.Append(PatchRevision);
            } else if(Patch.HasValue || (Major == 0)) {
                result.Append('.');
                result.Append(Patch ?? 0);
            }
            result.Append(Suffix);
            return result.ToString();
        }

        public override int GetHashCode() => (Major << 16) ^ (Minor << 8) ^ ((Patch ?? 0) << 4) ^ (PatchRevision ?? 0)  ^ Suffix.GetHashCode();

        public bool IsAssemblyCompatibleWith(VersionInfo other) {
            if(Suffix != other.Suffix) {
                return false;
            }
            if(Major != other.Major) {
                return false;
            }
            if(Major != 0) {
                return Minor == other.Minor;
            }

            // when Major version is 0, we rely on Minor and Patch to match
            return ((Minor == other.Minor) && (Patch == other.Patch));
        }

        public int? CompareToVersion(VersionInfo other, bool strict = false) {
            if(object.ReferenceEquals(other, null)) {
                return null;
            }
            if(strict) {

                // suffixes must match to be comparable in strict mode
                if(Suffix != other.Suffix) {
                    return null;
                }
                var result = Major - other.Major;
                if(result != 0) {
                    return Sign(result);
                }
                result = Minor - other.Minor;
                if(result != 0) {
                    return Sign(result);
                }
                result = (Patch ?? 0) - (other.Patch ?? 0);
                if(result != 0) {
                    return Sign(result);
                }
                result = (PatchRevision ?? 0) - (other.PatchRevision ?? 0);
                if(result != 0) {
                    return Sign(result);
                }
                return 0;
            } else {

                // version number dominates other comparisions
                var result = Major - other.Major;
                if(result != 0) {
                    return Sign(result);
                }
                result = Minor - other.Minor;
                if(result != 0) {
                    return Sign(result);
                }
                result = (Patch ?? 0) - (other.Patch ?? 0);
                if(result != 0) {
                    return Sign(result);
                }
                result = (PatchRevision ?? 0) - (other.PatchRevision ?? 0);
                if(result != 0) {
                    return Sign(result);
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
            }

            // versions cannot be compared
            return null;

            // local functions
            int Sign(int value) => (value < 0) ? -1 : ((value > 0) ? 1 : 0);
        }

        public bool IsLessThanVersion(VersionInfo info, bool strict = false) => CompareToVersion(info, strict) < 0;
        public bool IsLessOrEqualThanVersion(VersionInfo info, bool strict = false) => CompareToVersion(info, strict) <= 0;
        public bool IsGreaterThanVersion(VersionInfo info, bool strict = false) => CompareToVersion(info, strict) > 0;
        public bool IsGreaterOrEqualThanVersion(VersionInfo info, bool strict = false) => CompareToVersion(info, strict) >= 0;
        public bool IsEqualToVersion(VersionInfo info, bool strict = false) => CompareToVersion(info, strict) == 0;

        public string GetWildcardVersion() {
            if(IsPreRelease) {

                // NOTE (2018-12-16, bjorg): for pre-release version, there is no wildcard; the version must match everything
                return ToString();
            }
            if(Major == 0) {

                // when Major version is 0, the build number is relevant
                return $"{Major}.{Minor}.{Patch ?? 0}.*";
            }
            return $"{Major}.{Minor}.*";
        }

        public VersionInfo GetCompatibleCoreServicesVersion() {
            if(Major == 0) {

                // when Major version is 0, the build number is relevant
                return new VersionInfo(Major, Minor, Patch, patchMinor: null, Suffix);
            }
            return new VersionInfo(Major, Minor, patch: null, patchMinor: null, Suffix);
        }

        public bool MatchesConstraint(VersionInfo versionConstraint) {
            return (Major == versionConstraint.Major)
                && (Minor == versionConstraint.Minor)
                && (Suffix == versionConstraint.Suffix)
                && (!versionConstraint.Patch.HasValue || (Patch == versionConstraint.Patch))
                && (!versionConstraint.PatchRevision.HasValue || (PatchRevision == versionConstraint.PatchRevision));
        }

        public bool IsCoreServicesCompatible(VersionInfo info) {
            return (Major == info.Major) && ((Major != 0) || (Minor == info.Minor)) && (Suffix == info.Suffix);
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
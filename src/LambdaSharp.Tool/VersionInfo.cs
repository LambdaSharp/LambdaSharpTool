/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2020
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
            VersionWithSuffix.Parse(text, out var major, out var minor, out var build, out var revision, out var suffix);

            // if major version is 0, we need to assign the minor version as major suffix
            if(major == 0) {
                return new VersionInfo(
                    major,
                    minor,
                    (build != -1) ? build : 0,
                    (revision != -1) ? (int?)revision : null,
                    suffix
                );
            } else {
                if(revision != -1) {
                    throw new ArgumentException("unsupported format", nameof(text));
                }
                return new VersionInfo(
                    major,
                    majorPartial: null,
                    minor,
                    (build != -1) ? (int?)build : null,
                    suffix
                );
            }
        }

        public static bool TryParse(string text, out VersionInfo version) {
            version = null;
            if(!VersionWithSuffix.TryParse(text, out var major, out var minor, out var build, out var revision, out var suffix)) {
                return false;
            }

            // if major version is 0, we need to assign the minor version as major suffix
            if(major == 0) {
                version = new VersionInfo(
                    major,
                    minor,
                    (build != -1) ? build : 0,
                    (revision != -1) ? (int?)revision : null,
                    suffix
                );
            } else {
                if(revision != -1) {
                    return false;
                }
                version = new VersionInfo(
                    major,
                    majorPartial: null,
                    minor,
                    (build != -1) ? (int?)build : null,
                    suffix
                );
            }
            return true;
        }

        public static VersionInfo From(VersionWithSuffix version) {
            return (version.Major == 0)
                ? new VersionInfo(
                    version.Major,
                    version.Minor,
                    (version.Build != -1) ? version.Build : 0,
                    (version.Revision != -1) ? (int?)version.Revision : null,
                    version.Suffix
                )
                : new VersionInfo(
                    version.Major,
                    majorPartial: null,
                    version.Minor,
                    (version.Build != -1) ? (int?)version.Build : null,
                    version.Suffix
                );
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

        public static int? CompareTierVersionToToolVersion(VersionInfo tierVersion, VersionInfo toolVersion)
            => tierVersion.GetMajorVersion().CompareToVersion(toolVersion.GetMajorVersion());

        public static bool IsTierVersionCompatibleWithToolVersion(VersionInfo tierVersion, VersionInfo toolVersion)
            => tierVersion.GetMajorVersion().IsEqualToVersion(toolVersion.GetMajorVersion());

        public static bool IsModuleCoreVersionCompatibleWithToolVersion(VersionInfo moduleCoreVersion, VersionInfo toolVersion)
            => moduleCoreVersion.GetMajorVersion().IsEqualToVersion(toolVersion.GetMajorVersion());

        public static bool IsModuleCoreVersionCompatibleWithTierVersion(VersionInfo moduleCoreVersion, VersionInfo tierVersion)
            => moduleCoreVersion.GetMajorVersion().IsEqualToVersion(tierVersion.GetMajorVersion());

        public static VersionInfo GetCoreVersion(VersionInfo version)
            => version.GetMajorVersion();

        public static bool IsValidLambdaSharpAssemblyReferenceForToolVersion(VersionInfo toolVersion, string framework, string lambdaSharpAssemblyVersion) {

            // extract assembly version pattern without wildcard
            VersionWithSuffix libraryVersion;
            if(lambdaSharpAssemblyVersion.EndsWith(".*", StringComparison.Ordinal)) {
                libraryVersion = VersionWithSuffix.Parse(lambdaSharpAssemblyVersion.Substring(0, lambdaSharpAssemblyVersion.Length - 2));
            } else {
                libraryVersion = VersionWithSuffix.Parse(lambdaSharpAssemblyVersion);
            }

            // compare based on selected framework
            switch(framework) {
                case "netcoreapp2.1":

                    // .NET Core 2.1 projects require 0.8.0.*
                    return (libraryVersion.Major == 0)
                        && (libraryVersion.Minor == 8)
                        && (libraryVersion.Build == 0);
                case "netcoreapp3.1":

                    // .NET Core 3.1 projects require 0.8.*
                    return (libraryVersion.Major == 0)
                        && (libraryVersion.Minor >= 8)
                        && (libraryVersion.Build >= 0);
                default:
                    throw new ApplicationException($"unsupported framework: {framework}");
            }
        }

        //--- Fields ---
        public readonly int Major;
        public readonly int? MajorPartial;
        public readonly int Minor;
        public readonly int? Patch;
        public readonly string Suffix;

        //--- Constructors ---
        private VersionInfo(int major, int? majorPartial, int minor, int? patch, string suffix) {

            // parameter validation
            if((major == 0) && !majorPartial.HasValue) {
                throw new ArgumentException($"{nameof(majorPartial)} cannot be null when {nameof(major)} is 0");
            } else if((major != 0) && majorPartial.HasValue) {
                throw new ArgumentException($"{nameof(majorPartial)} must be null when {nameof(major)} is not 0");
            }

            // set fields
            Major = major;
            MajorPartial = majorPartial;
            Minor = minor;
            Patch = patch;
            Suffix = suffix ?? throw new ArgumentNullException(nameof(suffix));
        }

        //--- Properties ---
        public bool IsPreRelease => Suffix.Length > 0;
        public bool HasFloatingConstraints => !Patch.HasValue;

        //--- Methods ---
        public override string ToString() {
            var result = new StringBuilder();
            result.Append(Major);
            if(MajorPartial.HasValue) {
                result.Append('.');
                result.Append(MajorPartial);
            }
            result.Append('.');
            result.Append(Minor);
            if(Patch.HasValue) {
                result.Append('.');
                result.Append(Patch);
            }
            result.Append(Suffix);
            return result.ToString();
        }

        public override int GetHashCode() => (Major << 21) ^ ((MajorPartial ?? 0) << 14) ^ (Minor << 7) ^ (Patch ?? 0) ^ Suffix.GetHashCode();

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
                result = (MajorPartial ?? 0) - (other.MajorPartial ?? 0);
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
                return 0;
            } else {

                // version number dominates other comparisions
                var result = Major - other.Major;
                if(result != 0) {
                    return Sign(result);
                }
                result = (MajorPartial ?? 0) - (other.MajorPartial ?? 0);
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

        public string GetLambdaSharpAssemblyWildcardVersion(string framework) {
            switch(framework) {
                case "netcoreapp2.1":
                    return "0.8.0.*";
                case "netcoreapp3.1":
                    if(IsPreRelease) {

                        // NOTE (2018-12-16, bjorg): for pre-release version, there is no wildcard; the version must match everything
                        return ToString();
                    }
                    if(MajorPartial.HasValue) {
                        return $"{Major}.{MajorPartial}.{Minor}.*";
                    }
                    return $"{Major}.{Minor}.*";
                default:
                    throw new ApplicationException($"unsupported framework: {framework}");
            }
        }

        public VersionWithSuffix GetLambdaSharpAssemblyReferenceVersion() {
            return MajorPartial.HasValue
                ? new VersionWithSuffix(new Version(Major, MajorPartial.Value, Minor, Patch ?? 0), Suffix)
                : new VersionWithSuffix(new Version(Major, Minor, Patch ?? 0, 0), Suffix);
        }

        public bool MatchesConstraint(VersionInfo versionConstraint) {
            return (Major == versionConstraint.Major)
                && (MajorPartial == versionConstraint.MajorPartial)
                && (Minor == versionConstraint.Minor)
                && (Suffix == versionConstraint.Suffix)
                && (!versionConstraint.Patch.HasValue || (Patch == versionConstraint.Patch));
        }

        private VersionInfo GetMajorVersion() => new VersionInfo(Major, MajorPartial, 0, patch: null, Suffix);
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
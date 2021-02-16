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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using LambdaSharp.Modules.Serialization;

namespace LambdaSharp.Modules {

    [JsonConverter(typeof(JsonVersionInfoConverter))]
    [Newtonsoft.Json.JsonConverter(typeof(VersionInfoConverter))]
    public class VersionInfo {

        //--- Class Methods ---
        public static VersionInfo Parse(string text) {

            // check for version suffix (e.g "-rc1")
            var index = text.IndexOf('-');
            string versionText;
            string suffix;
            if(index < 0) {
                versionText = text;
                suffix = "";
            } else {
                versionText = text.Substring(0, index);
                suffix = text.Substring(index).TrimEnd('*');
            }

            // parse parts of the version number
            int major;
            int minor;
            int build;
            int revision;
            if(int.TryParse(versionText, out major)) {

                // version is a single integral major version number
                minor = -1;
                build = -1;
                revision = -1;
            } else {
                var version = Version.Parse(versionText);

                // assign parts of the parsed version information
                major = version.Major;
                minor = version.Minor;
                build = version.Build;
                revision = version.Revision;
            }

            // if major version is 0 and minor is greater 0, we encode the value as a fractional major version number
            if((major == 0) && (minor > 0)) {
                var fractionalMajor = int.MinValue + minor - 1;
                return new VersionInfo(
                    fractionalMajor,
                    (build != -1) ? (int?)build : null,
                    (revision != -1) ? (int?)revision : null,
                    suffix
                );
            }

            // revision is not supported in this format since it requires 4 numbers to be stored
            if(revision != -1) {
                throw new ArgumentException("unsupported format", nameof(text));
            }
            return new VersionInfo(
                major,
                (minor != -1) ? (int?)minor : null,
                (build != -1) ? (int?)build : null,
                suffix
            );
        }

        public static bool TryParse(string text, [NotNullWhen(true)] out VersionInfo? version) {

            // check for version suffix (e.g "-rc1")
            var index = text.IndexOf('-');
            string versionText;
            string suffix;
            if(index < 0) {
                versionText = text;
                suffix = "";
            } else {
                versionText = text.Substring(0, index);
                suffix = text.Substring(index).TrimEnd('*');
            }

            // parse parts of the version number
            int major;
            int minor;
            int build;
            int revision;
            if(int.TryParse(versionText, out major)) {

                // version is a single integral major version number
                minor = -1;
                build = -1;
                revision = -1;
            } else if(Version.TryParse(versionText, out var basicVersion)) {

                // assign parts of the parsed version information
                major = basicVersion.Major;
                minor = basicVersion.Minor;
                build = basicVersion.Build;
                revision = basicVersion.Revision;
            } else {
                version = null;
                return false;
            }

            // if major version is 0 and minor is not 0, we encode the value as a fractional major version number
            if((major == 0) && (minor != 0)) {
                var fractionalMajor = int.MinValue + minor - 1;
                version = new VersionInfo(
                    fractionalMajor,
                    (build != -1) ? (int?)build : null,
                    (revision != -1) ? (int?)revision : null,
                    suffix
                );
                return true;
            }

            // revision is not supported in this format since it requires 4 numbers to be stored
            if(revision != -1) {
                version = null;
                return false;
            }
            version = new VersionInfo(
                major,
                (minor != -1) ? (int?)minor : null,
                (build != -1) ? (int?)build : null,
                suffix
            );
            return true;
        }

        public static VersionInfo From(VersionWithSuffix version, bool strict = true) {
            if((version.Major == 0) && (version.Minor != 0)) {
                return new VersionInfo(
                    int.MinValue + version.Minor - 1,
                    (version.Build != -1) ? version.Build : 0,
                    (version.Revision != -1) ? (int?)version.Revision : null,
                    version.Suffix
                );
            }

            // revision is not supported in this format since it requires 4 numbers to be stored
            if(strict && (version.Revision != -1)) {
                throw new ArgumentException("unsupported format", nameof(version));
            }
            return new VersionInfo(
                version.Major,
                version.Minor,
                (version.Build != -1) ? (int?)version.Build : null,
                version.Suffix
            );
        }

        public static VersionInfo? Max(IEnumerable<VersionInfo> versionInfos, bool strict = false) {
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

        public static VersionInfo? FindLatestMatchingVersion(IEnumerable<VersionInfo> versionInfos, VersionInfo minVersion, Predicate<VersionInfo> validate) {
            var candidates = new List<VersionInfo>(versionInfos);
            while(candidates.Any()) {

                // find latest version
                var candidate = VersionInfo.Max(candidates) ?? throw new InvalidOperationException();
                candidates.Remove(candidate);

                // check if latest version meets minimum version constraint; or if none are provided, the version cannot be a pre-release
                if(
                    ((minVersion != null) && minVersion.IsGreaterThanVersion(candidate))
                    || ((minVersion == null) && (validate == null) && candidate.IsPreRelease())
                 ) {
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

        //--- Constructors ---
        private VersionInfo(int encodedMajor, int? minor, int? patch, string suffix) {

            // parameter validation
            if(minor.HasValue) {
                if(minor.Value < 0) {
                    throw new ArgumentException("value must be greater than or equal to 0", nameof(minor));
                }
                if(patch.GetValueOrDefault() < 0) {
                    throw new ArgumentException("value must be null, greater than, or equal to 0", nameof(patch));
                }
            } else if(patch.HasValue) {
                throw new ArgumentException($"value must be null, when {nameof(minor)} is null", nameof(patch));
            }

            // set fields
            EncodedMajor = encodedMajor;
            Minor = minor;
            Patch = patch;
            Suffix = suffix ?? throw new ArgumentNullException(nameof(suffix));
        }

        //--- Properties ---
        public int IntegralMajor => !IsFractionalMajor ? EncodedMajor : throw new InvalidOperationException();
        public int FractionalMajor => IsFractionalMajor ? (EncodedMajor - int.MinValue + 1) : throw new InvalidOperationException();
        public bool IsFractionalMajor => EncodedMajor < 0;
        public int? Minor { get; }
        public int? Patch { get; }
        public string Suffix { get; }
        private int EncodedMajor { get; }

        //--- Methods ---
        public override string ToString() {
            var result = new StringBuilder();

            // decode fractional major version number
            if(IsFractionalMajor) {
                result.Append("0.");
                result.Append(FractionalMajor);
            } else {
                result.Append(IntegralMajor);
            }

            // optionally append minor version number
            if(Minor.HasValue) {
                result.Append('.');
                result.Append(Minor);

                // optionally append patch version number
                if(Patch.HasValue) {
                    result.Append('.');
                    result.Append(Patch);
                }
            }

            // append version suffix
            result.Append(Suffix);
            return result.ToString();
        }

        public override int GetHashCode() => (EncodedMajor << 20) ^ ((Minor ?? 0) << 10) ^ (Patch ?? 0) ^ Suffix.GetHashCode();

        public int? CompareToVersion(VersionInfo other, bool strict = false) {
            if(object.ReferenceEquals(other, null)) {
                return null;
            }
            if(strict) {

                // suffixes must match to be comparable in strict mode
                if(Suffix != other.Suffix) {
                    return null;
                }
                var result = (long)EncodedMajor - (long)other.EncodedMajor;
                if(result != 0) {
                    return Sign(result);
                }
                result = (Minor ?? 0) - (other.Minor ?? 0);
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
                var result = (long)EncodedMajor - (long)other.EncodedMajor;
                if(result != 0) {
                    return Sign(result);
                }
                result = (Minor ?? 0) - (other.Minor ?? 0);
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
            int Sign(long value) => (value < 0L) ? -1 : ((value > 0L) ? 1 : 0);
        }

        public bool IsPreRelease() => Suffix.Length > 0;
        public bool IsLessThanVersion(VersionInfo info, bool strict = false) => CompareToVersion(info, strict) < 0;
        public bool IsLessOrEqualThanVersion(VersionInfo info, bool strict = false) => CompareToVersion(info, strict) <= 0;
        public bool IsGreaterThanVersion(VersionInfo info, bool strict = false) => CompareToVersion(info, strict) > 0;
        public bool IsGreaterOrEqualThanVersion(VersionInfo info, bool strict = false) => CompareToVersion(info, strict) >= 0;
        public bool IsEqualToVersion(VersionInfo info, bool strict = false) => CompareToVersion(info, strict) == 0;
        public VersionInfo GetMajorOnlyVersion() => new VersionInfo(EncodedMajor, minor: null, patch: null, Suffix);
        public VersionInfo GetMajorMinorVersion() => new VersionInfo(EncodedMajor, Minor, patch: null, Suffix);
        public VersionInfo WithoutSuffix() => new VersionInfo(EncodedMajor, Minor, Patch, suffix: "");

        public bool MatchesConstraint(VersionInfo versionConstraint) {
            return (EncodedMajor == versionConstraint.EncodedMajor)
                && (!versionConstraint.Minor.HasValue || (Minor == versionConstraint.Minor))
                && (!versionConstraint.Patch.HasValue || (Patch == versionConstraint.Patch))
                && (Suffix == versionConstraint.Suffix);
        }
    }
}
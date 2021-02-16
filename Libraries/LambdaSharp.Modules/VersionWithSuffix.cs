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
using System.Text;

namespace LambdaSharp.Modules {

    public class VersionWithSuffix {

        //--- Class Methods ---
        public static void Parse(string text, out int major, out int minor, out int build, out int revision, out string suffix) {

            // check for version suffix (e.g "-rc1")
            var index = text.IndexOf('-');
            Version version;
            if(index < 0) {
                version = Version.Parse(text);
                suffix = "";
            } else {
                version = Version.Parse(text.Substring(0, index));
                suffix = text.Substring(index).TrimEnd('*');
            }

            // assign parts of the parsed version information
            major = version.Major;
            minor = version.Minor;
            build = version.Build;
            revision = version.Revision;
        }

        public static bool TryParse(string text, out int major, out int minor, out int build, out int revision, out string suffix) {
            major = -1;
            minor = -1;
            build = -1;
            revision = -1;
            suffix = "";

            // check for version suffix (e.g "-rc1")
            var index = text.IndexOf('-');
            string prefix;
            if(index < 0) {
                prefix = text;
                suffix = "";
            } else {
                prefix = text.Substring(0, index);
                suffix = text.Substring(index).TrimEnd('*');
            }

            // attempt to parse the rest of the version information
            if(!Version.TryParse(prefix, out var prefixVersion)) {
                return false;
            }

            // assign parts of the parsed version information
            major = prefixVersion.Major;
            minor = prefixVersion.Minor;
            build = prefixVersion.Build;
            revision = prefixVersion.Revision;
            return true;
        }

         public static VersionWithSuffix Parse(string text) {
            VersionWithSuffix.Parse(text, out var major, out var minor, out var build, out var revision, out var suffix);
            if(build == -1) {
                return new VersionWithSuffix(new Version(major, minor), suffix);
            }
            if(revision == -1) {
                return new VersionWithSuffix(new Version(major, minor, build), suffix);
            }
            return new VersionWithSuffix(new Version(major, minor, build, revision), suffix);
        }

        public static bool TryParse(string text, out VersionWithSuffix? version) {
            if(TryParse(text, out var major, out var minor, out var build, out var revision, out var suffix)) {
                if(build == -1) {
                    version = new VersionWithSuffix(new Version(major, minor), suffix);
                } else if(revision == -1) {
                    version = new VersionWithSuffix(new Version(major, minor, build), suffix);
                } else {
                    version = new VersionWithSuffix(new Version(major, minor, build, revision), suffix);
                }
                return true;
            }
            version = null;
            return false;
        }

        //--- Fields ---
        public readonly Version Version;
        public readonly string Suffix;

        //--- Constructors ---
        public VersionWithSuffix(Version version, string suffix) {
            Version = version ?? throw new ArgumentNullException(nameof(version));
            Suffix = suffix ?? throw new ArgumentNullException(nameof(suffix));
        }

        //--- Properties ---
        public int Major => Version.Major;
        public int Minor => Version.Minor;
        public int Build => Version.Build;
        public int Revision => Version.Revision;

        //--- Methods ---
        public int? CompareToVersion(VersionWithSuffix other) {
            if(object.ReferenceEquals(other, null)) {
                return null;
            }

            // version number dominates other comparisions
            var result = Major - other.Major;
            if(result != 0) {
                return Sign(result);
            }
            result = Minor - other.Minor;
            if(result != 0) {
                return Sign(result);
            }
            result = Math.Max(0, Build) - Math.Max(0, other.Build);
            if(result != 0) {
                return Sign(result);
            }
            result = Math.Max(0, Revision) - Math.Max(0, other.Revision);
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

            // versions cannot be compared
            return null;

            // local functions
            int Sign(int value) => (value < 0) ? -1 : ((value > 0) ? 1 : 0);
        }

        public bool IsLessThanVersion(VersionWithSuffix other) => CompareToVersion(other) < 0;
        public bool IsLessOrEqualThanVersion(VersionWithSuffix other) => CompareToVersion(other) <= 0;
        public bool IsGreaterThanVersion(VersionWithSuffix other) => CompareToVersion(other) > 0;
        public bool IsGreaterOrEqualThanVersion(VersionWithSuffix other) => CompareToVersion(other) >= 0;
        public bool IsEqualToVersion(VersionWithSuffix other) => CompareToVersion(other) == 0;
        public override string ToString() => Version.ToString() + Suffix;
    }
}
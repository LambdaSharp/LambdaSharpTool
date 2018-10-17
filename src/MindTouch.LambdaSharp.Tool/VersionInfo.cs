/*
 * MindTouch λ#
 * Copyright (C) 2018 MindTouch, Inc.
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

namespace MindTouch.LambdaSharp.Tool {

    public enum VersionInfoCompare {
        Undefined,
        Older,
        Newer,
        Same
    }

    public class VersionInfo {

        //--- Class Methods ---
        public static VersionInfo Parse(string text) {
            var index = text.IndexOf('-');
            if(index < 0) {
                return new VersionInfo(Version.Parse(text), "");
            } else {
                return new VersionInfo(Version.Parse(text.Substring(0, index)), text.Substring(index));
            }
        }

        public static bool TryParse(string text, out VersionInfo version) {
            try {
                version = Parse(text);
                return true;
            } catch {
                version = null;
                return false;
            }
        }

        //--- Fields ---

        public readonly Version Version;
        public readonly string Suffix;

        //--- Constructors ---
        private VersionInfo(Version version, string suffix) {
            Version = version;
            Suffix = suffix;
        }

        //--- Properties ---
        public int Major => Version.Major;
        public int Minor => Version.Minor;
        public bool IsPreRelease => Suffix.Length > 0;

        //--- Methods ---
        public VersionInfo WithSuffix(string suffix) {
            return new VersionInfo(Version, suffix);
        }

        public VersionInfoCompare CompareTo(VersionInfo other) {
            if(Suffix != other.Suffix) {

                // versions with different suffixes cannot be compared
                return VersionInfoCompare.Undefined;
            }
            if(Version.Major > other.Version.Major) {
                return VersionInfoCompare.Newer;
            }
            if(Version.Major < other.Version.Major) {
                return VersionInfoCompare.Older;
            }
            if(Version.Minor > other.Version.Minor) {
                return VersionInfoCompare.Newer;
            }
            if(Version.Minor < other.Version.Minor) {
                return VersionInfoCompare.Older;
            }
            if(Version.Build > other.Version.Build) {
                if(Version.Revision >= other.Version.Revision) {
                    return VersionInfoCompare.Newer;
                } else {

                    // inconsistent build/revision comparisons yield no result
                    return VersionInfoCompare.Undefined;
                }
            }
            if(Version.Build < other.Version.Build) {
                if(Version.Revision <= other.Version.Revision) {
                    return VersionInfoCompare.Older;
                } else {

                    // inconsistent build/revision comparisons yield no result
                    return VersionInfoCompare.Undefined;
                }
            }
            if(Version.Revision > other.Version.Revision) {
                if(Version.Build >= other.Version.Build) {
                    return VersionInfoCompare.Newer;
                } else {

                    // inconsistent build/revision comparisons yield no result
                    return VersionInfoCompare.Undefined;
                }
            }
            if(Version.Revision < other.Version.Revision) {
                if(Version.Build <= other.Version.Build) {
                    return VersionInfoCompare.Older;
                } else {

                    // inconsistent build/revision comparison yields no result
                    return VersionInfoCompare.Undefined;
                }
            }

            // versions are equal
            return VersionInfoCompare.Same;
        }

        override public string ToString() => Version.ToString() + Suffix;
    }
}
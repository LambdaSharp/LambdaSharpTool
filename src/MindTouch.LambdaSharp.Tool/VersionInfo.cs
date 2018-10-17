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

    public class VersionInfo : IComparable<VersionInfo>, IEquatable<VersionInfo> {

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
            Version = version ?? throw new ArgumentNullException(nameof(version));
            Suffix = suffix ?? throw new ArgumentNullException(nameof(suffix));
        }

        //--- Properties ---
        public int Major => Version.Major;
        public int Minor => Version.Minor;
        public bool IsPreRelease => Suffix.Length > 0;

        //--- Methods ---
        public VersionInfo WithSuffix(string suffix) {
            return new VersionInfo(Version, suffix);
        }

        override public string ToString() => Version.ToString() + Suffix;

        public bool Equals(VersionInfo other) {
            if(other == null) {
                return false;
            }
            if(Suffix != other.Suffix) {
                throw new ArgumentException("version suffix mismatch", nameof(other));
            }
            return Version.CompareTo(other.Version) == 0;
        }

        public override bool Equals(object obj){
            if(ReferenceEquals(null, obj)) {
                return false;
            }
            if(ReferenceEquals(this, obj)) {
                return true;
            }
            if(obj.GetType() != GetType()) {
                return false;
            }
            return Equals(obj as VersionInfo);
	    }

        public override int GetHashCode()
            => Version.GetHashCode() & Suffix.GetHashCode();

        public int CompareTo(VersionInfo other) {
            if(other == null) {
                throw new ArgumentNullException(nameof(other));
            }
            if(Suffix != other.Suffix) {
                throw new ArgumentException("version suffix mismatch", nameof(other));
            }
            return Version.CompareTo(other.Version);
        }
    }
}
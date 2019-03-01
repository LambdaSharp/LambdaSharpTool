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
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace LambdaSharp.Tool.Internal {

    internal static class StringEx {

        //--- Class Fields ---
        private static readonly Regex ModuleKeyPattern = new Regex(@"^(?<ModuleOwner>\w+)\.(?<ModuleName>[\w\.]+)(:(?<Version>\*|[\w\.\-]+))?(@(?<BucketName>\w+))?$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        //--- Extension Methods ---
        public static string ToMD5Hash(this string text) {
            using(var md5 = MD5.Create()) {
                return md5.ComputeHash(Encoding.UTF8.GetBytes(text)).ToHexString();
            }
        }

        public static string ToHexString(this IEnumerable<byte> bytes)
            => string.Concat(bytes.Select(x => x.ToString("X2")));

        public static string PascalCaseToLabel(string name) {
            var builder = new StringBuilder();
            var isUppercase = true;
            foreach(var c in name) {
                if(char.IsDigit(c)) {
                    if(!isUppercase) {
                        builder.Append(' ');
                    }
                    isUppercase = true;
                    builder.Append(c);
                } else if(char.IsLetter(c)) {
                    if(isUppercase) {
                        isUppercase = char.IsUpper(c);
                        builder.Append(c);
                    } else {
                        if(isUppercase = char.IsUpper(c)) {
                            builder.Append(' ');
                        }
                        builder.Append(c);
                    }
                } else {
                    if(!isUppercase) {
                        builder.Append(' ');
                    }
                    isUppercase = true;
                }
            }
            return builder.ToString();
        }

        public static string ToIdentifier(this string text)
            => new string(text.Where(char.IsLetterOrDigit).ToArray());

        public static bool TryParseModuleOwnerName(this string compositeModuleOwnerName, out string moduleOwner, out string moduleName) {
            moduleOwner = "<BAD>";
            moduleName = "<BAD>";
            if(compositeModuleOwnerName == null) {
                return false;
            }
            var moduleOwnerAndName = compositeModuleOwnerName.Split(".", 2);
            if(
                (moduleOwnerAndName.Length != 2)
                || (moduleOwnerAndName[0].Length == 0)
                || (moduleOwnerAndName[1].Length == 0)
            ) {
                return false;
            }
            moduleOwner = moduleOwnerAndName[0];
            moduleName = moduleOwnerAndName[1];
            return true;
        }

        public static bool TryParseModuleDescriptor(
            this string moduleReference,
            out string moduleOwner,
            out string moduleName,
            out VersionInfo moduleVersion,
            out string moduleBucketName
        ) {
            if(moduleReference == null) {
                moduleOwner = "<BAD>";
                moduleName = "<BAD>";
                moduleVersion = VersionInfo.Parse("0.0");
                moduleBucketName = "<BAD>";
                return false;
            }
            if(moduleReference.StartsWith("s3://", StringComparison.Ordinal)) {
                var uri = new Uri(moduleReference);

                // absolute path always starts with '/', which needs to be removed
                var pathSegments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();
                if((pathSegments.Count < 5) || (pathSegments[1] != "Modules") || (pathSegments[3] != "Versions")) {
                    moduleOwner = "<BAD>";
                    moduleName = "<BAD>";
                    moduleVersion = VersionInfo.Parse("0.0");
                    moduleBucketName = "<BAD>";
                    return false;
                }
                moduleOwner = pathSegments[0];
                moduleName = pathSegments[2];
                moduleVersion = VersionInfo.Parse(pathSegments[4]);
                moduleBucketName = uri.Host;
                return true;
            }

            // try parsing module reference
            var match = ModuleKeyPattern.Match(moduleReference);
            if(!match.Success) {
                    moduleOwner = "<BAD>";
                    moduleName = "<BAD>";
                    moduleVersion = VersionInfo.Parse("0.0");
                    moduleBucketName = "<BAD>";
                return false;
            }
            moduleOwner = GetMatchValue("ModuleOwner");
            moduleName = GetMatchValue("ModuleName");
            moduleBucketName = GetMatchValue("BucketName");

            // parse optional version
            var requestedVersionText = GetMatchValue("Version");
            moduleVersion = ((requestedVersionText != null) && (requestedVersionText != "*"))
                ? VersionInfo.Parse(requestedVersionText)
                : null;
            return true;

            // local function
            string GetMatchValue(string groupName) {
                var group = match.Groups[groupName];
                return group.Success ? group.Value : null;
            }
        }

        public static string ComputeHashForFiles(
            this IEnumerable<string> files,
            Func<string, string> normalizeFilePath = null,
            Predicate<string> skipFile = null
        ) {

            // hash file paths and file contents
            using(var md5 = MD5.Create())
            using(var hashStream = new CryptoStream(Stream.Null, md5, CryptoStreamMode.Write)) {
                foreach(var file in files
                    .Where(file => skipFile?.Invoke(file) != true)
                    .OrderBy(file => file)
                ) {

                    // hash file path
                    var filePathBytes = Encoding.UTF8.GetBytes(normalizeFilePath?.Invoke(file) ?? file);
                    hashStream.Write(filePathBytes, 0, filePathBytes.Length);

                    // hash file contents
                    using(var stream = File.OpenRead(file)) {
                        stream.CopyTo(hashStream);
                    }
                }
                hashStream.FlushFinalBlock();
                return md5.Hash.ToHexString();
            }
        }
    }
}
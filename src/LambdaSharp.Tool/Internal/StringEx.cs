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
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace LambdaSharp.Tool.Internal {

    internal static class StringEx {

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

        public static string ToPascalIdentifier(this string text) {
            var identifier = text.ToIdentifier();
            return char.ToUpperInvariant(identifier[0]) + ((identifier.Length > 1) ? identifier.Substring(1) : "");
        }

        public static bool TryParseModuleFullName(this string compositeModuleFullName, out string moduleNamespace, out string moduleName) {
            moduleNamespace = "<BAD>";
            moduleName = "<BAD>";
            if(!ModuleInfo.TryParse(compositeModuleFullName, out var moduleInfo)) {
                return false;
            }
            if((moduleInfo.Version != null) || (moduleInfo.Origin != null)) {
                return false;
            }
            moduleNamespace = moduleInfo.Namespace;
            moduleName = moduleInfo.Name;
            return true;
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

        public static bool TryParseAssemblyClassMethodReference(string reference, out string assemblyName, out string className, out string methodName) {
            var parts = reference.Split("::").Reverse().ToArray();
            if(parts.Length > 3) {
                assemblyName = null;
                className = null;
                methodName = null;
                return false;
            }
            methodName = parts.FirstOrDefault();
            className = parts.Skip(1).FirstOrDefault();
            assemblyName = parts.Skip(2).FirstOrDefault();
            return true;
        }
    }
}
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
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace LambdaSharp.Build.CSharp.Internal {

    internal static class MsBuildFileUtilities {

        // NOTE (2019-12-19): this code was taken from the identically named MSBuild methods to ensure similar processing
        //  of file paths across platforms.
        //  https://github.com/microsoft/msbuild/blob/ab9377f1f20d81c2ef16469cbe4f9cdafe1479a1/src/Shared/FileUtilities.cs#L424

        //--- Properties ---
        private static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        //--- Methods ---

        /// <summary>
        /// If on Unix, convert backslashes to slashes for strings that resemble paths.
        /// The heuristic is if something resembles paths (contains slashes) check if the
        /// first segment exists and is a directory.
        /// Use a native shared method to massage file path. If the file is adjusted,
        /// that qualifies is as a path.
        ///
        /// @baseDirectory is just passed to LooksLikeUnixFilePath, to help with the check
        /// </summary>
        public static string MaybeAdjustFilePath(string baseDirectory, string value) {
            var comparisonType = StringComparison.Ordinal;

            // Don't bother with arrays or properties or network paths, or those that
            // have no slashes.
            if(
                IsWindows
                || string.IsNullOrEmpty(value)
                || value.StartsWith("$(", comparisonType)
                || value.StartsWith("@(", comparisonType)
                || value.StartsWith("\\\\", comparisonType)
            ) {
                return value;
            }

            // For Unix-like systems, we may want to convert backslashes to slashes
            var newValue = ConvertToUnixSlashes(value);

            // Find the part of the name we want to check, that is remove quotes, if present
            var shouldAdjust = (newValue.IndexOf('/') != -1)
                && LooksLikeUnixFilePath(RemoveQuotes(newValue), baseDirectory);
            return shouldAdjust
                ? newValue.ToString()
                : value;
        }

        private static bool LooksLikeUnixFilePath(string value, string baseDirectory = "") {
            if(IsWindows) {
                return false;
            }

            // The first slash will either be at the beginning of the string or after the first directory name
            var directoryLength = value.IndexOf('/', 1) + 1;
            var shouldCheckDirectory = directoryLength != 0;

            // Check for actual files or directories under / that get missed by the above logic
            var shouldCheckFileOrDirectory = !shouldCheckDirectory
                && (value.Length > 0)
                && (value[0] == '/');

            return (
                shouldCheckDirectory
                && Directory.Exists(Path.Combine(baseDirectory, value.Substring(0, directoryLength)))
            ) || (
                shouldCheckFileOrDirectory
                && (
                    File.Exists(value)
                    || Directory.Exists(value)
                )
            );
        }

        private static string RemoveQuotes(string path) {
            var endId = path.Length - 1;
            var singleQuote = '\'';
            var doubleQuote = '\"';
            var hasQuotes = (
                (path.Length > 2)
                && ((path[0] == singleQuote)
                && (path[endId] == singleQuote)
            ) || (
                (path[0] == doubleQuote)
                && path[endId] == doubleQuote)
            );
            return hasQuotes ? path.Substring(1, endId - 1) : path;
        }

        private static string ConvertToUnixSlashes(string path) {
            if(path.IndexOf('\\') == -1) {
                return path;
            }
            var unixPath = new StringBuilder(path.Length);
            CopyAndCollapseSlashes(path, unixPath);
            return unixPath.ToString();
        }

        private static void CopyAndCollapseSlashes(string str, StringBuilder copy) {

            // Performs Regex.Replace(str, @"[\\/]+", "/")
            for(int i = 0; i < str.Length; i++) {
                var isCurSlash = IsAnySlash(str[i]);
                var isPrevSlash = i > 0 && IsAnySlash(str[i - 1]);
                if(!isCurSlash || !isPrevSlash) {
                    copy.Append((str[i] == '\\') ? '/' : str[i]);
                }
            }
        }

        private static bool IsAnySlash(char c) => (c == '/') || (c == '\\');
    }
}
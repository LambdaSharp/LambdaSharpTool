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
using LambdaSharp.Modules.Exceptions;

namespace LambdaSharp.Modules {

    public static class VersionInfoCompatibility {

        //--- Class Methods ---
        public static bool IsTierVersionCompatibleWithToolVersion(VersionInfo tierVersion, VersionInfo toolVersion)
            => toolVersion.IsPreRelease()

                // allow a tool pre-release, next version to be compatible with any previously deployed tier version
                ? tierVersion.GetMajorOnlyVersion().WithoutSuffix().IsEqualToVersion(toolVersion.GetMajorOnlyVersion().WithoutSuffix()) && tierVersion.IsLessOrEqualThanVersion(toolVersion)

                // tool major version must match tier major version
                : tierVersion.GetMajorOnlyVersion().IsEqualToVersion(toolVersion.GetMajorOnlyVersion());

        public static bool IsModuleCoreVersionCompatibleWithToolVersion(VersionInfo moduleCoreVersion, VersionInfo toolVersion)
            => toolVersion.IsPreRelease()
                ? moduleCoreVersion.GetMajorOnlyVersion().WithoutSuffix().IsEqualToVersion(toolVersion.GetMajorOnlyVersion().WithoutSuffix()) && moduleCoreVersion.IsLessOrEqualThanVersion(toolVersion)
                : moduleCoreVersion.GetMajorOnlyVersion().IsEqualToVersion(toolVersion.GetMajorOnlyVersion());

        public static bool IsModuleCoreVersionCompatibleWithTierVersion(VersionInfo moduleCoreVersion, VersionInfo tierVersion)
            => moduleCoreVersion.GetMajorOnlyVersion().IsEqualToVersion(tierVersion.GetMajorOnlyVersion());

        public static int? CompareTierVersionToToolVersion(VersionInfo tierVersion, VersionInfo toolVersion)
            => (tierVersion.IsPreRelease() || toolVersion.IsPreRelease())

                // for pre-releases, we need the entire version information to determine ordering of versions
                ? tierVersion.CompareToVersion(toolVersion)

                // for stable releases, we only need the major version
                : tierVersion.GetMajorOnlyVersion().CompareToVersion(toolVersion.GetMajorOnlyVersion());

        public static VersionInfo GetCoreServicesReferenceVersion(VersionInfo version)
            => version.GetMajorOnlyVersion();

        public static string GetLambdaSharpAssemblyWildcardVersion(VersionInfo toolVersion, string framework) {
            if(toolVersion.IsPreRelease()) {

                // NOTE (2018-12-16, bjorg): for pre-release version, there is no wildcard; the version must match everything
                return toolVersion.ToString();
            }
            return $"{GetLambdaSharpAssemblyReferenceVersion(toolVersion)}.*";
        }

        public static VersionInfo GetLambdaSharpAssemblyReferenceVersion(VersionInfo version) => version.GetMajorMinorVersion();

        public static bool IsNetCore3OrLater(string framework)
            => (
                framework.StartsWith("netcoreapp", StringComparison.Ordinal)
                && (string.Compare(framework, "netcoreapp3.", StringComparison.Ordinal) >= 0)
            ) || IsNet5OrLater(framework);

        public static bool IsNet5OrLater(string framework)
            => framework.StartsWith("net", StringComparison.Ordinal)
                && !framework.StartsWith("netcoreapp", StringComparison.Ordinal)
                && (
                    string.Equals(framework, "net5", StringComparison.Ordinal)
                    && (string.Compare(framework, "net5.", StringComparison.Ordinal) >= 0)
                );

        public static bool IsValidLambdaSharpAssemblyReferenceForToolVersion(VersionInfo toolVersion, string projectFramework, string lambdaSharpAssemblyVersion, out bool outdated) {

            // extract assembly version pattern without wildcard
            VersionWithSuffix libraryVersion;
            if(lambdaSharpAssemblyVersion.EndsWith(".*", StringComparison.Ordinal)) {
                libraryVersion = VersionWithSuffix.Parse(lambdaSharpAssemblyVersion.Substring(0, lambdaSharpAssemblyVersion.Length - 2));
            } else {
                libraryVersion = VersionWithSuffix.Parse(lambdaSharpAssemblyVersion);
            }

            // compare based on selected framework
            bool valid;
            switch(projectFramework) {
            case "netstandard2.1":

                // .NET Standard 2.1 projects (Blazor) require 0.8.1.* or 0.8.2.*
                valid = (libraryVersion.Major == 0)
                    && (libraryVersion.Minor == 8)
                    && (
                        (libraryVersion.Build == 1)
                        || (libraryVersion.Build == 2)
                    );
                break;
            case "netcoreapp2.1":

                // .NET Core 2.1 projects (Lambda) require 0.8.0.* or 0.8.1.*
                valid = (libraryVersion.Major == 0)
                    && (libraryVersion.Minor == 8)
                    && (
                        (libraryVersion.Build == 0)
                        || (libraryVersion.Build == 1)
                    );
                break;
            case "netcoreapp3.1":

                // .NET Core 3.1 projects (Lambda) require 0.8.0.*, 0.8.1.*, or 0.8.2.*
                valid = (libraryVersion.Major == 0)
                    && (libraryVersion.Minor == 8)
                    && (
                        (libraryVersion.Build == 0)
                        || (libraryVersion.Build == 1)
                        || (libraryVersion.Build == 2)
                    );
                break;
            case "net5":
            case "net5.0":

                // .NET 5 projects require 0.8.2.*
                valid = (libraryVersion.Major == 0)
                    && (libraryVersion.Minor == 8)
                    && (libraryVersion.Build == 2);
                break;
            default:
                throw new VersionInfoCompatibilityUnsupportedFrameworkException(projectFramework);
            }
            outdated = valid && VersionInfo.From(libraryVersion, strict: false).IsLessThanVersion(toolVersion.GetMajorMinorVersion());
            return valid;
        }
    }
}
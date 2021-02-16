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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using LambdaSharp.Modules;
using LambdaSharp.Modules.Exceptions;

namespace LambdaSharp.Build.CSharp.Internal {

    internal class CSharpProjectFile {

        //--- Class Methods ---
        public static void DiscoverDependencies(HashSet<string> files, string filePath, Action<string>? analyzingProjectCallback, Action<string, Exception>? errorCallback) {
            try {

                // convert back-slashes in filePath reference if need be
                filePath = MsBuildFileUtilities.MaybeAdjustFilePath("", filePath);

                // skip project if project file doesn't exist or has already been added
                if(!File.Exists(filePath) || files.Contains(filePath)) {
                    return;
                }

                // add project to list of analyzed files
                files.Add(filePath);
                analyzingProjectCallback?.Invoke(filePath);

                // enumerate all files in project folder
                var projectFolder = Path.GetDirectoryName(filePath) ?? throw new InvalidOperationException ($"unable to obtain directory name from file path: {filePath}");
                AddFiles(projectFolder, SearchOption.AllDirectories);
                files.RemoveWhere(file => file.StartsWith(Path.GetFullPath(Path.Combine(projectFolder, "bin"))));
                files.RemoveWhere(file => file.StartsWith(Path.GetFullPath(Path.Combine(projectFolder, "obj"))));

                // analyze project for references
                var csproj = XDocument.Load(filePath, LoadOptions.PreserveWhitespace);

                // TODO (2019-10-22, bjorg): enhance precision for understanding elements in .csrpoj files

                // recurse into referenced projects
                foreach(var projectReference in csproj.Descendants("ProjectReference").Where(node => node.Attribute("Include") != null)) {
                    DiscoverDependencies(files, GetFilePathFromIncludeAttribute(projectReference), analyzingProjectCallback, errorCallback);
                }

                // add compile file references
                foreach(var compile in csproj.Descendants("Compile").Where(node => node.Attribute("Include") != null)) {
                    AddFileReferences(GetFilePathFromIncludeAttribute(compile));
                }

                // add content file references
                foreach(var content in csproj.Descendants("Content").Where(node => node.Attribute("Include") != null)) {
                    AddFileReferences(GetFilePathFromIncludeAttribute(content));
                }

                // added embedded resources
                foreach(var embeddedResource in csproj.Descendants("EmbeddedResource").Where(node => node.Attribute("Include") != null)) {
                    AddFileReferences(GetFilePathFromIncludeAttribute(embeddedResource));
                }

                // local functions
                string GetFilePathFromIncludeAttribute(XElement element)
                    => Path.GetFullPath(Path.Combine(projectFolder, MsBuildFileUtilities.MaybeAdjustFilePath(projectFolder, ResolveFilePath(element.Attribute("Include").Value))));

            } catch(Exception e) {
                errorCallback?.Invoke($"error while analyzing '{filePath}'", e);
            }

            // local function
            void AddFileReferences(string path) {
                var parts = path.Split(new[] { '/', '\\' });
                if(path.Contains("**")) {

                    // NOTE: path contains a recursive wildcard; take part of path up until segment that contains the recursion wildcard '**'
                    var recursionRootPath = Path.Combine(parts.TakeWhile(part => !part.Contains("**")).ToArray());
                    AddFiles(recursionRootPath, SearchOption.AllDirectories);
                } else if(parts.Take(parts.Length - 1).Any(part => part.Contains("*") || part.Contains("?"))) {

                    // NOTE: path contains a wildcard character in a folder portion of the path; enumerate all contents like we do for '**';
                    var recursionRootPath = Path.Combine(parts.TakeWhile(part => !part.Contains("*") && !part.Contains("?")).ToArray());
                    AddFiles(recursionRootPath, SearchOption.AllDirectories);
                } else if(parts.Last().Contains("*")) {

                    // NOTE: last segment in path contains a wildcard for the filename; enumerate the folder contents without recursion

                    // exclude last path segment that contains the wildcard
                    var rootPath = Path.Combine(parts.Take(parts.Length - 1).ToArray());
                    AddFiles(rootPath, SearchOption.TopDirectoryOnly);
                } else if(Directory.Exists(path)) {
                    AddFiles(path, SearchOption.TopDirectoryOnly);
                } else if(File.Exists(path)) {
                    files.Add(path);
                }
            }

            void AddFiles(string folder, SearchOption option) {
                if(Directory.Exists(folder)) {
                    foreach(var file in Directory.GetFiles(folder, "*.*", option)) {
                        files.Add(file);
                    }
                }
            }

            string ResolveFilePath(string path) => Regex.Replace(path, @"\$\((?!\!)[^\)]+\)", match => {
                var matchText = match.ToString();
                var name = matchText.Substring(2, matchText.Length - 3).Trim();
                return Environment.GetEnvironmentVariable(name) ?? matchText;
            });
        }

        //--- Fields ---
        private readonly XDocument _csproj;
        private readonly XElement? _mainPropertyGroup;

        //--- Constructors ---
        public CSharpProjectFile(string filePath) {
            _csproj = XDocument.Load(filePath, LoadOptions.PreserveWhitespace);
            _mainPropertyGroup = _csproj.Element("Project")?.Element("PropertyGroup");
            ProjectName = _mainPropertyGroup?.Element("AssemblyName")?.Value ?? Path.GetFileNameWithoutExtension(filePath);
            TargetFramework = _mainPropertyGroup?.Element("TargetFramework").Value ?? throw new InvalidDataException("missing <TargetFramework>");
            RootNamespace = _mainPropertyGroup?.Element("RootNamespace")?.Value;
            OutputType = _mainPropertyGroup?.Element("OutputType")?.Value;
            AssemblyName = _mainPropertyGroup?.Element("AssemblyName")?.Value;
        }

        //--- Properties ---
        public string ProjectName { get; }
        public string TargetFramework { get; }
        public string? RootNamespace { get; }
        public string? OutputType { get; }
        public string? AssemblyName { get; }
        public IEnumerable<XElement> PackageReferences => _csproj.Element("Project")?.Descendants("PackageReference") ?? Enumerable.Empty<XElement>();

        //--- Methods ---
        public bool RemoveAmazonLambdaToolsReference() {
            var obsoleteNodes = _csproj.Descendants()
                .Where(element =>
                    (element.Name == "DotNetCliToolReference")
                    && ((string)element.Attribute("Include") == "Amazon.Lambda.Tools")
                )
                .ToList();
            if(!obsoleteNodes.Any()) {
                return false;
            }
            foreach(var obsoleteNode in obsoleteNodes) {
                var parent = obsoleteNode.Parent;

                // remove obsolete node
                obsoleteNode.Remove();

                // remove parent if no children are left
                if(!parent.Elements().Any()) {
                    parent.Remove();
                }
            }
            return true;
        }

        public bool ValidateLambdaSharpPackageReferences(VersionInfo toolVersion, Action<string>? logWarn, Action<string>? logError) {
            var success = true;
            var includes = PackageReferences.Where(elem => elem.Attribute("Include")?.Value.StartsWith("LambdaSharp", StringComparison.Ordinal) ?? false);
            foreach(var include in includes) {
                var expectedVersion = VersionInfoCompatibility.GetLambdaSharpAssemblyWildcardVersion(toolVersion, TargetFramework);
                var library = include.Attribute("Include").Value;
                var libraryVersionText = include.Attribute("Version")?.Value;
                if(libraryVersionText == null) {
                    success = false;
                    logError?.Invoke($"csproj file is missing a version attribute in its assembly reference for {library} (expected version: '{expectedVersion}')");
                } else {
                    try {
                        if(!VersionInfoCompatibility.IsValidLambdaSharpAssemblyReferenceForToolVersion(toolVersion, TargetFramework, libraryVersionText, out var outdated)) {

                            // check if we're compiling a conditional package reference in contributor mode
                            if((include.Attribute("Condition")?.Value != null) && (Environment.GetEnvironmentVariable("LAMBDASHARP") != null)) {

                                // show error as warning instead since this package reference will not be used anyway
                                logWarn?.Invoke($"csproj file contains a mismatched assembly reference for {library} (expected version: '{expectedVersion}', found: '{libraryVersionText}')");
                            } else {
                                success = false;
                                logError?.Invoke($"csproj file contains a mismatched assembly reference for {library} (expected version: '{expectedVersion}', found: '{libraryVersionText}')");
                            }
                        } else if(outdated) {

                            // show warning to updated dependency
                            logWarn?.Invoke($"csproj file contains outdated assembly reference for {library} (expected version: '{expectedVersion}', found: '{libraryVersionText}')");
                        }
                    } catch(VersionInfoCompatibilityUnsupportedFrameworkException) {
                        success = false;
                        logError?.Invoke($"csproj file targets unsupported framework '{TargetFramework}'");
                    } catch {
                        success = false;
                        logError?.Invoke($"csproj file contains an invalid wildcard version in its assembly reference for {library} (expected version: '{expectedVersion}', found: '{libraryVersionText}')");
                    }
                }
            }
            return success;
        }

        public void Save(string filePath) => _csproj.Save(filePath);

    }
}
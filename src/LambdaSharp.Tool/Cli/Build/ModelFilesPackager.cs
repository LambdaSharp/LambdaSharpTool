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
using System.IO;
using System.IO.Compression;
using System.Text;
using LambdaSharp.Tool.Internal;
using LambdaSharp.Tool.Model;
using ICSharpCode.SharpZipLib.Zip;

namespace LambdaSharp.Tool.Cli.Build {

    public class ModelFilesPackager : AModelProcessor {

        //--- Constants ---
        private const int READ_AND_WRITE_PERMISSIONS = 0b1_000_000_110_110_110 << 16;
        private const int READ_AND_EXECUTE_PERMISSIONS = 0b1_000_000_101_101_101 << 16;
        private static byte[] ELF_HEADER = new byte[] { 0x7F, 0x45, 0x4C, 0x46 };

        //--- Class Methods ---
        private static bool IsElfExecutable(Stream stream) {
            var fileHeader = new byte[4];
            var isExecutable = (stream.Read(fileHeader, 0, fileHeader.Length) == fileHeader.Length)
                && ELF_HEADER.SequenceEqual(fileHeader);
            stream.Seek(0, SeekOrigin.Begin);
            return isExecutable;
        }

        //--- Types ---
        private class ForwardSlashEncoder : UTF8Encoding {

            //--- Constructors ---
            public ForwardSlashEncoder() : base(true) { }

            //--- Methods ---
            public override byte[] GetBytes(string text)
                => base.GetBytes(text.Replace(@"\", "/"));
        }

        //--- Fields ---
        private ModuleBuilder _builder;
        private HashSet<string> _existingPackages;

        //--- Constructors ---
        public ModelFilesPackager(Settings settings, string sourceFilename) : base(settings, sourceFilename) { }

        //--- Methods ---
        public void Package(ModuleBuilder builder, bool noPackageBuild) {
            _builder = builder;

            // clear output folder when no build is requested
            if(noPackageBuild) {
                if(Directory.Exists(Settings.OutputDirectory)) {
                    foreach(var file in Directory.GetFiles(Settings.OutputDirectory, $"package*.zip")) {
                        try {
                            File.Delete(file);
                        } catch { }
                    }
                }
                return;
            }

            // check if there are any packages items to build
            var packages = builder.Items.OfType<PackageItem>().ToList();
            if(!packages.Any()) {
                return;
            }

            // collect list of previously built packages
            if(!Directory.Exists(Settings.OutputDirectory)) {
                Directory.CreateDirectory(Settings.OutputDirectory);
            }
            _existingPackages = new HashSet<string>(Directory.GetFiles(Settings.OutputDirectory, $"package*.zip"));

            // build each package
            foreach(var item in packages) {
                AtLocation(item.FullName, () => {
                    ProcessParameter(item);
                });
            }

            // delete remaining packages, they are out-of-date
            foreach(var leftoverPackage in _existingPackages) {
                try {
                    File.Delete(leftoverPackage);
                } catch { }
            }
        }

        private void ProcessParameter(PackageItem parameter) {
            AtLocation("Package", () => {
                var containsElfExecutable = false;

                // check if a build command is present
                if(parameter.Build != null) {
                    Console.WriteLine($"=> Building package {Settings.InfoColor}{parameter.Name}{Settings.ResetColor}");
                    var commandAndArguments = parameter.Build.Split(' ', 2);
                    if(!ProcessLauncher.Execute(
                        commandAndArguments[0],
                        commandAndArguments[1],
                        Settings.WorkingDirectory,
                        Settings.VerboseLevel >= VerboseLevel.Detailed,
                        ColorizeOutput
                    )) {
                        LogError("package build command failed");
                        return;
                    }
                }

                // discover files to package
                var files = new List<KeyValuePair<string, string>>();
                string folder;
                string filePattern;
                SearchOption searchOption;
                var packageFiles = Path.Combine(Settings.WorkingDirectory, parameter.Files);
                if((packageFiles.EndsWith("/", StringComparison.Ordinal) || Directory.Exists(packageFiles))) {
                    folder = Path.GetFullPath(packageFiles);
                    filePattern = "*";
                    searchOption = SearchOption.AllDirectories;
                } else {
                    folder = Path.GetDirectoryName(packageFiles);
                    filePattern = Path.GetFileName(packageFiles);
                    searchOption = SearchOption.TopDirectoryOnly;
                }
                if(Directory.Exists(folder)) {
                    foreach(var filePath in Directory.GetFiles(folder, filePattern, searchOption)) {
                        var relativeFilePathName = Path.GetRelativePath(folder, filePath);
                        files.Add(new KeyValuePair<string, string>(relativeFilePathName, filePath));
                    }
                    files = files.OrderBy(file => file.Key).ToList();
                } else {
                    LogError($"cannot find folder '{Path.GetRelativePath(Settings.WorkingDirectory, folder)}'");
                    return;
                }

                // compute MD5 hash for package
                var bytes = new List<byte>();
                foreach(var file in files) {

                    // check if one of the files in the package is an ELF executable
                    using(var stream = File.OpenRead(file.Value)) {
                        if(IsElfExecutable(stream)) {
                            containsElfExecutable = true;
                            break;
                        }
                    }
                }

                // compute hash for all files
                var fileValueToFileKey = files.ToDictionary(kv => kv.Value, kv => kv.Key);
                var hash = files.Select(kv => kv.Value).ComputeHashForFiles(file => fileValueToFileKey[file]);
                var package = Path.Combine(Settings.OutputDirectory, $"package_{_builder.FullName}_{parameter.LogicalId}_{hash}.zip");

                // only build package if it doesn't exist
                if(!_existingPackages.Remove(package)) {

                    // create zip package
                    if(parameter.Build == null) {
                        Console.WriteLine($"=> Building package {Settings.InfoColor}{parameter.Name}{Settings.ResetColor}");
                    }
                    if(containsElfExecutable) {

                        // compress package contents with executable permissions
                        using(var outputStream = File.OpenWrite(package))
                        using(var outputZip = new ZipOutputStream(outputStream)) {
                            foreach(var file in files) {
                                if(Settings.VerboseLevel >= VerboseLevel.Detailed) {
                                    Console.WriteLine($"... zipping: {file.Key}");
                                }
                                using(var entryStream = File.OpenRead(file.Value)) {
                                    var entry = new ZipEntry(file.Key.Replace('\\', '/')) {
                                        HostSystem = (int)HostSystemID.Unix
                                    };
                                    entry.ExternalFileAttributes = IsElfExecutable(entryStream)
                                        ? READ_AND_EXECUTE_PERMISSIONS
                                        : READ_AND_WRITE_PERMISSIONS;

                                    // add entry to zip archive
                                    outputZip.PutNextEntry(entry);
                                    entryStream.CopyTo(outputZip);
                                }
                            }
                        }
                    } else {

                        // package contents with built-in zip library, which is ~6x faster
                        using(var zipArchive = System.IO.Compression.ZipFile.Open(package, ZipArchiveMode.Create, new ForwardSlashEncoder())) {
                            foreach(var file in files) {
                                if(Settings.VerboseLevel >= VerboseLevel.Detailed) {
                                    Console.WriteLine($"... zipping: {file.Key}");
                                }
                                zipArchive.CreateEntryFromFile(file.Value, file.Key);
                            }
                        }
                    }
                }
                _builder.AddArtifact($"{parameter.FullName}::PackageName", package);
            });

            // local functions
            string ColorizeOutput(string line)
                => !Settings.UseAnsiConsole
                    ? line
                    : line.Contains(": error ", StringComparison.Ordinal)
                    ? $"{AnsiTerminal.BrightRed}{line}{AnsiTerminal.Reset}"
                    : line.Contains(": warning ", StringComparison.Ordinal)
                    ? $"{AnsiTerminal.BrightYellow}{line}{AnsiTerminal.Reset}"
                    : line;
        }
    }
}
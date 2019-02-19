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
using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using LambdaSharp.Tool.Internal;
using LambdaSharp.Tool.Model;
using ICSharpCode.SharpZipLib.Zip;

namespace LambdaSharp.Tool.Cli.Build {

    public class ModelFilesPackager : AModelProcessor {

        //--- Constants ---
        private const int READ_ONLY_PERMISSIONS = 0b1_000_000_110_100_100 << 16;
        private const int READ_AND_EXECUTE_PERMISSIONS = 0b1_000_000_111_101_101 << 16;
        private const int ZIP_HOST_OS = 3; // Linux
        private const int ZIP_VERSION = 30; // 3.0
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

            // delete remaining packages
            foreach(var leftoverPackage in _existingPackages) {
                try {
                    File.Delete(leftoverPackage);
                } catch { }
            }
        }

        private void ProcessParameter(PackageItem parameter) {
            AtLocation("Package", () => {
                var containsElfExecutable = false;

                // compute MD5 hash for package
                string package;
                using(var md5 = MD5.Create()) {
                    var bytes = new List<byte>();
                    foreach(var file in parameter.Files) {
                        using(var stream = File.OpenRead(file.Value)) {

                            // check if one of the files in the package is an ELF executable
                            containsElfExecutable = containsElfExecutable || IsElfExecutable(stream);

                            // compute MD5 of filename and file contents
                            bytes.AddRange(Encoding.UTF8.GetBytes(file.Key));
                            var fileHash = md5.ComputeHash(stream);
                            bytes.AddRange(fileHash);
                            if(Settings.VerboseLevel >= VerboseLevel.Detailed) {
                                Console.WriteLine($"... computing md5: {file.Key} => {fileHash.ToHexString()}");
                            }
                        }
                    }
                    package = Path.Combine(Settings.OutputDirectory, $"package_{parameter.FullName.Replace("::", "-")}_{md5.ComputeHash(bytes.ToArray()).ToHexString()}.zip");
                }

                // only build package if it doesn't exist
                if(!_existingPackages.Remove(package)) {

                    // create zip package
                    Console.WriteLine($"=> Building package {parameter.Name}");
                    if(containsElfExecutable) {

                        // compress package contents with executable permissions
                        using(var outputStream = File.OpenWrite(package))
                        using(var outputZip = new ZipOutputStream(outputStream)) {
                            foreach(var file in parameter.Files) {
                                if(Settings.VerboseLevel >= VerboseLevel.Detailed) {
                                    Console.WriteLine($"... zipping: {file.Key}");
                                }
                                using(var entryStream = File.OpenRead(file.Value)) {
                                    var entry = new ZipEntry(file.Key.Replace('\\', '/'), 0, (ZIP_HOST_OS << 8) | ZIP_VERSION, CompressionMethod.Deflated);

                                    // set execution permission for ELF executable
                                    entry.ExternalFileAttributes = IsElfExecutable(entryStream)
                                        ? READ_AND_EXECUTE_PERMISSIONS
                                        : READ_ONLY_PERMISSIONS;

                                    // add entry to zip archive
                                    outputZip.PutNextEntry(entry);
                                    entryStream.CopyTo(outputZip);
                                }
                            }
                        }
                    } else {

                        // package contents with built-in zip library, which is ~6x faster
                        using(var zipArchive = System.IO.Compression.ZipFile.Open(package, ZipArchiveMode.Create, new ForwardSlashEncoder())) {
                            foreach(var file in parameter.Files) {
                                if(Settings.VerboseLevel >= VerboseLevel.Detailed) {
                                    Console.WriteLine($"... zipping: {file.Key}");
                                }
                                zipArchive.CreateEntryFromFile(file.Value, file.Key);
                            }
                        }
                    }
                }
                _builder.AddAsset($"{parameter.FullName}::PackageName", package);
            });
        }
    }
}
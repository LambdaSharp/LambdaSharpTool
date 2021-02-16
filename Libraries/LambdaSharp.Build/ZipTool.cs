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

using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;

namespace LambdaSharp.Build {

    public class ZipTool : ABuildEventsSource {

        //--- Constants ---
        private const int READ_AND_WRITE_PERMISSIONS = 0b1_000_000_110_110_110 << 16;
        private const int READ_AND_EXECUTE_PERMISSIONS = 0b1_000_000_101_101_101 << 16;
        private static byte[] ELF_HEADER = new byte[] { 0x7F, 0x45, 0x4C, 0x46 };

        //--- Types ---
        private class ForwardSlashEncoder : UTF8Encoding {

            //--- Constructors ---
            public ForwardSlashEncoder() : base(true) { }

            //--- Methods ---
            public override byte[] GetBytes(string text)
                => base.GetBytes(text.Replace(@"\", "/"));
        }

        //--- Class Methods ---
        private static bool IsElfExecutable(Stream stream) {
            var fileHeader = new byte[4];
            var isExecutable = (stream.Read(fileHeader, 0, fileHeader.Length) == fileHeader.Length)
                && ELF_HEADER.SequenceEqual(fileHeader);
            stream.Seek(0, SeekOrigin.Begin);
            return isExecutable;
        }

        //--- Constructors ---
        public ZipTool(BuildEventsConfig? buildEventsConfig = null) : base(buildEventsConfig) { }

        //--- Methods ---
        public bool ZipData(string package, string folder, bool showOutput) {
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                using(var zipArchive = System.IO.Compression.ZipFile.Open(package, ZipArchiveMode.Create)) {
                    foreach(var file in Directory.GetFiles(folder, "*", SearchOption.AllDirectories)) {
                        var filename = Path.GetRelativePath(folder, file);
                        zipArchive.CreateEntryFromFile(file, filename);
                    }
                }
            } else {
                var zipTool = ProcessLauncher.ZipExe;
                if(string.IsNullOrEmpty(zipTool)) {
                    LogError("failed to find the \"zip\" utility program in path. This program is required to maintain Linux file permissions in the zip archive.");
                    return false;
                }
                if(!new ProcessLauncher(BuildEventsConfig).Execute(
                    zipTool,
                    new[] { "-r", package, "." },
                    folder,
                    showOutput
                )) {
                    LogError("'zip' command failed");
                    return false;
                }
            }
            return true;
        }

        public void ZipWithExecutable(string package, List<KeyValuePair<string, string>> files) {

            // check if at least one file is an ELF executable
            var containsElfExecutable = false;
            foreach(var file in files) {

                // check if one of the files in the package is an ELF executable
                using(var stream = File.OpenRead(file.Value)) {
                    if(IsElfExecutable(stream)) {
                        containsElfExecutable = true;
                        break;
                    }
                }
            }
            if(containsElfExecutable) {

                // compress package contents with executable permissions
                using(var outputStream = File.OpenWrite(package))
                using(var outputZip = new ZipOutputStream(outputStream)) {
                    foreach(var file in files) {
                        LogInfoVerbose($"... zipping: {file.Key}");
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
                        LogInfoVerbose($"... zipping: {file.Key}");
                        zipArchive.CreateEntryFromFile(file.Value, file.Key);
                    }
                }
            }
        }

        public void ZipFolderWithExecutable(string outputPackagePath, string folder) {
            var files = new List<KeyValuePair<string, string>>();
            foreach(var filePath in Directory.GetFiles(folder, "*", SearchOption.AllDirectories)) {
                var relativeFilePathName = Path.GetRelativePath(folder, filePath);
                files.Add(new KeyValuePair<string, string>(relativeFilePathName, filePath));
            }
            files = files.OrderBy(file => file.Key).ToList();
            new ZipTool(BuildEventsConfig).ZipWithExecutable(outputPackagePath, files);
        }
    }
}
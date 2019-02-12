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

namespace LambdaSharp.Tool.Cli.Build {

    public class ModelFilesPackager : AModelProcessor {

        private class ForwardSlashEncoder : UTF8Encoding {

            //--- Constructors ---
            public ForwardSlashEncoder() : base(true) { }

            //--- Methods ---
            public override byte[] GetBytes(string text)
                => base.GetBytes(text.Replace(@"\", "/"));
        }

        //--- Fields ---
        private ModuleBuilder _builder;

        //--- Constructors ---
        public ModelFilesPackager(Settings settings, string sourceFilename) : base(settings, sourceFilename) { }

        //--- Methods ---
        public void Package(ModuleBuilder builder, bool noPackageBuild) {
            _builder = builder;
            if(Directory.Exists(Settings.OutputDirectory)) {
                foreach(var file in Directory.GetFiles(Settings.OutputDirectory, $"package*.zip")) {
                    try {
                        File.Delete(file);
                    } catch { }
                }
            }
            if(noPackageBuild) {
                return;
            }
            foreach(var item in builder.Items.OfType<PackageItem>()) {
                AtLocation(item.FullName, () => {
                    ProcessParameter(item);
                });
            }
        }

        private void ProcessParameter(PackageItem parameter) {
            AtLocation("Package", () => {

                // compute MD5 hash for package
                string package;
                using(var md5 = MD5.Create()) {
                    var bytes = new List<byte>();
                    foreach(var file in parameter.Files) {
                        using(var stream = File.OpenRead(file.Value)) {
                            bytes.AddRange(Encoding.UTF8.GetBytes(file.Key));
                            var fileHash = md5.ComputeHash(stream);
                            bytes.AddRange(fileHash);
                            if(Settings.VerboseLevel >= VerboseLevel.Detailed) {
                                Console.WriteLine($"... computing md5: {file.Key} => {fileHash.ToHexString()}");
                            }
                        }
                    }
                    package = Path.Combine(Settings.OutputDirectory, $"package_{parameter.Name}_{md5.ComputeHash(bytes.ToArray()).ToHexString()}.zip");
                }

                // create zip package
                Console.WriteLine($"=> Building {parameter.Name} package");
                if(!Directory.Exists(Settings.OutputDirectory)) {
                    Directory.CreateDirectory(Settings.OutputDirectory);
                }
                using(var zipArchive = ZipFile.Open(package, ZipArchiveMode.Create, new ForwardSlashEncoder())) {
                    foreach(var file in parameter.Files) {
                        zipArchive.CreateEntryFromFile(file.Value, file.Key);
                    }
                }
                _builder.AddAsset($"{parameter.FullName}::PackageName", package);
            });
        }
    }
}
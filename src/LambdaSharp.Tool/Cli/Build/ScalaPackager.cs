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
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using LambdaSharp.Tool.Internal;
using LambdaSharp.Tool.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LambdaSharp.Tool.Cli.Build {

    public class ScalaPackager {

        //--- Class Methods ---
        public static void DetermineFunctionProperties(
            string functionName,
            string project,
            ref string language,
            ref string runtime,
            ref string handler
        ) {
            language = "scala";
            runtime = runtime ?? "java8";
            handler = handler ?? throw new ArgumentException("The handler name is required for Scala functions");
        }

        public static string Process(
            FunctionItem function,
            bool skipCompile,
            bool noAssemblyValidation,
            string gitSha,
            string gitBranch,
            string buildConfiguration,
            bool showOutput
        ) {
            function.Language = "scala";
            var projectDirectory = Path.GetDirectoryName(function.Project);

            // check if we need a default handler
            if (function.Function.Handler == null) {
                throw new Exception("The function handler cannot be empty for Scala functions.");
            }

            // compile function and create assembly
            if (!skipCompile) {
                ProcessLauncher.Execute(
                    "sbt",
                    new[] { "assembly" },
                   projectDirectory,
                    showOutput
                );
            }

            // check if we need to set a default runtime
            if(function.Function.Runtime == null) {
                function.Function.Runtime = "java8";
            }

            // check if the project zip file was created
            var scalaOutputJar = Path.Combine(projectDirectory, "target", "scala-2.12", "app.jar");
            return scalaOutputJar;
        }

        public static void ProcessScala(
            FunctionItem function,
            bool skipCompile,
            bool noAssemblyValidation,
            string gitSha,
            string gitBranch,
            string buildConfiguration,
            string outputDirectory,
            HashSet<string> existingPackages,
            string gitInfoFilename,
            ModuleBuilder builder
        ) {
            var showOutput = Settings.VerboseLevel >= VerboseLevel.Detailed;
            var scalaOutputJar = ScalaPackager.Process(function, skipCompile, noAssemblyValidation, gitSha, gitBranch, buildConfiguration, showOutput);

            // compute hash for zip contents
            string hash;
            using(var zipArchive = ZipFile.OpenRead(scalaOutputJar)) {
                using(var md5 = MD5.Create())
                using(var hashStream = new CryptoStream(Stream.Null, md5, CryptoStreamMode.Write)) {
                    foreach(var entry in zipArchive.Entries.OrderBy(e => e.FullName)) {

                        // hash file path
                        var filePathBytes = Encoding.UTF8.GetBytes(entry.FullName.Replace('\\', '/'));
                        hashStream.Write(filePathBytes, 0, filePathBytes.Length);

                        // hash file contents
                        using(var stream = entry.Open()) {
                            stream.CopyTo(hashStream);
                        }
                    }
                    hashStream.FlushFinalBlock();
                    hash = md5.Hash.ToHexString();
                }
            }

            // rename function package with hash
            var package = Path.Combine(outputDirectory, $"function_{builder.FullName}_{function.LogicalId}_{hash}.jar");
            if(!existingPackages.Remove(package)) {
                File.Move(scalaOutputJar, package);

                // add git-info.json file
                using(var zipArchive = ZipFile.Open(package, ZipArchiveMode.Update)) {
                    var entry = zipArchive.CreateEntry(gitInfoFilename);

                    // Set RW-R--R-- permissions attributes on non-Windows operating system
                    if(!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                        entry.ExternalAttributes = 0b1_000_000_110_100_100 << 16;
                    }
                    using(var stream = entry.Open()) {
                        stream.Write(Encoding.UTF8.GetBytes(JObject.FromObject(new ModuleManifestGitInfo {
                            SHA = gitSha,
                            Branch = gitBranch
                        }).ToString(Formatting.None)));
                    }
                }
            } else {
                File.Delete(scalaOutputJar);
            }

            // decompress project zip into temporary folder so we can add the 'GITSHAFILE' files
            builder.AddArtifact($"{function.FullName}::PackageName", package);
        }
    }
}
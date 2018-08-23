/*
 * MindTouch λ#
 * Copyright (C) 2018 MindTouch, Inc.
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
using System.Linq;
using System.IO;
using MindTouch.LambdaSharp.Tool.Model.AST;
using System.Threading.Tasks;
using Amazon.S3.Transfer;
using Amazon.S3.Model;
using MindTouch.LambdaSharp.Tool.Internal;
using YamlDotNet.Serialization;

namespace MindTouch.LambdaSharp.Tool {

    public class ModelUploader : AModelProcessor {

        //--- Fields ---
        private readonly TransferUtility _transferUtility;
        private string _bucket;

        //--- Constructors ---
        public ModelUploader(Settings settings) : base(settings) {
            _transferUtility = new TransferUtility(settings.S3Client);
        }

        //--- Methods ---
        public async Task ProcessAsync(ModuleNode module, string bucket, bool skipUpload) {
            _bucket = bucket;

            // finalize module definition
            ProcessModule(module);

            // upload functions and packages
            if(!skipUpload) {
                Console.WriteLine($"Uploading module assets");

                // upload function packages
                if(module.Functions?.Any() == true) {
                    foreach(var function in module.Functions.Where(f => f.PackagePath != null)) {
                        var s3 = function.S3Location.ToS3Info();
                        if(s3.Bucket == _bucket) {
                            await UploadPackageAsync(
                                s3.Key,
                                function.PackagePath,
                                "Lambda function"
                            );
                        }
                    }
                }

                // upload file packages (NOTE: packages are cannot be nested, so just enumerate the top level parameters)
                if(module.Parameters?.Any() == true) {
                    foreach(var parameter in module.Parameters.Where(p => p.Package?.PackagePath != null)) {
                        var s3 = parameter.Package.S3Location.ToS3Info();
                        if(s3.Bucket == _bucket) {
                            await UploadPackageAsync(
                                s3.Key,
                                parameter.Package.PackagePath,
                                "package"
                            );
                        }
                    }
                }
            }

            // serialize module as YAML file
            var yaml = new SerializerBuilder().Build().Serialize(module);
            await File.WriteAllTextAsync(Path.Combine(Settings.OutputDirectory, "Module.yml"), yaml);

            // upload module definition
            if(!skipUpload) {
                var moduleKey = $"Modules/{module.Name}/{module.Version}/Module.yml";

                // TODO (2018-08-22, bjorg): add flag to force update
                // if(await S3ObjectExistsAsync(moduleKey)) {
                //     AddError($"module {module.Name} (v{module.Version}) already exists at {_bucket}");
                //     return;
                // }
                Console.WriteLine($"=> Uploading module: s3://{_bucket}/{moduleKey}");
                await Settings.S3Client.PutObjectAsync(new PutObjectRequest {
                    BucketName = _bucket,
                    Key = moduleKey,
                    ContentBody = yaml
                });
            }
        }

        private async Task UploadPackageAsync(string key, string package, string description) {

            // only upload files that don't exist
            if(!await S3ObjectExistsAsync(key)) {
                Console.WriteLine($"=> Uploading {description}: s3://{_bucket}/{key}");
                await _transferUtility.UploadAsync(package, _bucket, key);
            }

            // delete the source zip file when there is no failure and the output directory is the working directory
            if(Settings.OutputDirectory == Settings.WorkingDirectory) {
                try {
                    File.Delete(package);
                } catch { }
            }
        }

        private async Task<bool> S3ObjectExistsAsync(string key) {
            var found = false;
            try {
                await Settings.S3Client.GetObjectMetadataAsync(new GetObjectMetadataRequest {
                    BucketName = _bucket,
                    Key = key
                });
                found = true;
            } catch { }
            return found;
        }

        private void ProcessModule(ModuleNode module) {
            ProcessSecrets(module);
            ProcessParameters(module);
            ProcessFunctions(module);
        }

        private void ProcessSecrets(ModuleNode module) {

            // remove empty section
            if(!module.Secrets.Any()) {
                module.Secrets = null;
            }
        }

        private void ProcessParameters(ModuleNode module) {
            foreach(var parameter in module.Parameters.Where(p => p.Package != null)) {
                parameter.Package.S3Location = $"s3://{_bucket}/Modules/{module.Name}/{module.Version}/{Path.GetFileName(parameter.Package.PackagePath)}";

                // files have been packed and uploaded already
                parameter.Package.Files = null;
            }

            // remove empty section
            if(!module.Parameters.Any()) {
                module.Secrets = null;
            }
       }

        private void ProcessFunctions(ModuleNode module) {
            foreach(var function in module.Functions) {
                function.S3Location = $"s3://{_bucket}/Modules/{module.Name}/{module.Version}/{Path.GetFileName(function.PackagePath)}";

                // project has been compiled and uploaded already
                function.Project = null;
            }

            // remove empty section
            if(!module.Functions.Any()) {
                module.Secrets = null;
            }
       }
    }
}
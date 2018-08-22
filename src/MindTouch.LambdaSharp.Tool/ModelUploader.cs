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

namespace MindTouch.LambdaSharp.Tool {

    public class ModelUploader : AModelProcessor {

        //--- Fields ---
        private readonly TransferUtility _transferUtility;

        //--- Constructors ---
        public ModelUploader(Settings settings) : base(settings) {
            _transferUtility = new TransferUtility(settings.S3Client);
        }

        //--- Methods ---
        public async Task Process(ModuleNode module, string bucket) {

            // upload function packages
            foreach(var function in module.Functions.Where(f => f.PackagePath != null)) {
                var key = $"{module.Name}/{Path.GetFileName(function.PackagePath)}";
                function.S3Location = await UploadPackage(
                    bucket,
                    key,
                    function.PackagePath,
                    "Lambda function"
                );
            }

            // upload file packages (NOTE: packages are cannot be nested, so just enumerate the top level parameters)
            foreach(var parameter in module.Parameters.Where(p => p.Package != null)) {
                var key = $"{module.Name}/{Path.GetFileName(parameter.Package.PackagePath)}";
                parameter.Package.S3Location = await UploadPackage(
                    bucket,
                    key,
                    parameter.Package.PackagePath,
                    "package"
                );
            }
        }

        private async Task<string> UploadPackage(string bucket, string key, string package, string description) {

            // check if a matching package file already exists in the bucket
            var found = false;
            try {
                await Settings.S3Client.GetObjectMetadataAsync(new GetObjectMetadataRequest {
                    BucketName = bucket,
                    Key = key
                });
                found = true;
            } catch { }

            // only upload files that don't exist
            if(!found) {
                Console.WriteLine($"=> Uploading {description}: s3://{bucket}/{key} ");
                await _transferUtility.UploadAsync(package, bucket, key);
            }

            // delete the source zip file when there is no failure and the output directory is the working directory
            if(Settings.OutputDirectory == Settings.WorkingDirectory) {
                try {
                    File.Delete(package);
                } catch { }
            }
            return $"s3://{bucket}/{key}";
        }
    }
}
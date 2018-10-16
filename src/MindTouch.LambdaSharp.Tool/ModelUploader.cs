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
using System.Threading.Tasks;
using Amazon.S3.Transfer;
using Amazon.S3.Model;
using MindTouch.LambdaSharp.Tool.Model;

namespace MindTouch.LambdaSharp.Tool {

    public class ModelUploader : AModelProcessor {

        //--- Fields ---
        private readonly TransferUtility _transferUtility;

        //--- Constructors ---
        public ModelUploader(Settings settings) : base(settings) {
            _transferUtility = new TransferUtility(settings.S3Client);
        }

        //--- Methods ---
        public async Task ProcessAsync(ModuleManifest manifest, string assetsPath, bool skipUpload) {

            // upload functions and packages
            if(!skipUpload) {
                Console.WriteLine($"Uploading module assets");

                // upload function packages
                foreach(var asset in manifest.Assets) {
                    await UploadPackageAsync(manifest, Path.Combine(assetsPath, asset.Path), asset.Type);
                }
            }
        }

        private async Task UploadPackageAsync(ModuleManifest manifest, string package, string description) {
            var key = $"{Settings.DeploymentBucketPath}{manifest.Name}/{Path.GetFileName(package)}";

            // only upload files that don't exist
            if(!await S3ObjectExistsAsync(key)) {
                Console.WriteLine($"=> Uploading {description}: s3://{Settings.DeploymentBucketName}/{key}");
                await _transferUtility.UploadAsync(package, Settings.DeploymentBucketName, key);
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
                    BucketName = Settings.DeploymentBucketName,
                    Key = key
                });
                found = true;
            } catch { }
            return found;
        }
    }
}
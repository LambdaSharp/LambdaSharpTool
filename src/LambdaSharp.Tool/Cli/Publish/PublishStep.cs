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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using LambdaSharp.Tool.Internal;
using LambdaSharp.Tool.Model;

namespace LambdaSharp.Tool.Cli.Publish {

    public class PublishStep : AModelProcessor {

        //--- Constructors ---
        public PublishStep(Settings settings, string sourceFilename) : base(settings, sourceFilename) { }

        //--- Methods---
        public async Task<string> DoAsync(string cloudformationFile, bool forcePublish) {

            // make sure there is a deployment bucket
            if(Settings.DeploymentBucketName == null) {
                LogError("missing deployment bucket", new LambdaSharpDeploymentTierSetupException(Settings.Tier));
                return null;
            }

            // load cloudformation template
            if(!File.Exists(cloudformationFile)) {
                LogError("folder does not contain a CloudFormation file for publishing");
                return null;
            }

            // load cloudformation file
            var manifest = await new ModelManifestLoader(Settings, "cloudformation.json").LoadFromFileAsync(cloudformationFile);
            if(manifest == null) {
                return null;
            }
            if(!manifest.Module.TryParseModuleDescriptor(
                out string moduleOwner,
                out string moduleName,
                out VersionInfo moduleVersion,
                out string _
            )) {
                throw new ApplicationException("invalid module info");
            }
            var destinationKey = $"{moduleOwner}/Modules/{moduleName}/Versions/{moduleVersion}/cloudformation.json";

            // check if we want to always publish, regardless of version or detected changes
            if(!forcePublish) {

                // TODO (2019-06-11, bjorg): let's enable this for 0.7, but no for 0.6.0.1; it feels too dramatic a change for just a small release

                // // check if module has a stable version, but is compiled from a dirty git branch
                // if(
                //     !moduleVersion.IsPreRelease
                //     && (manifest.Git.SHA?.StartsWith("DIRTY-") ?? false)
                // ) {
                //     LogError($"attempting to publish an immutable release of {moduleOwner}.{moduleName} (v{moduleVersion}) with uncommitted/untracked changes; use --force-publish to proceed anyway");
                //     return null;
                // }

                // check if a manifest already exists for this version
                var existingManifest = await new ModelManifestLoader(Settings, "cloudformation.json").LoadFromS3Async(Settings.DeploymentBucketName, destinationKey, errorIfMissing: false);
                if(existingManifest != null) {
                    if(!moduleVersion.IsPreRelease) {
                        LogWarn($"{moduleOwner}.{moduleName} (v{moduleVersion}) is already published; use --force-publish to proceed anyway");
                        return null;
                    }
                }
            }

            // publish module
            return await new ModelPublisher(Settings, cloudformationFile).PublishAsync(manifest, destinationKey, forcePublish);
        }
    }
}
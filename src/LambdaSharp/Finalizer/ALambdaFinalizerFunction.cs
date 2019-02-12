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

using System.Threading.Tasks;
using Amazon.Lambda.Core;
using LambdaSharp.CustomResource;

namespace LambdaSharp.Finalizer {

    public class FinalizerRequestProperties {

        //--- Properties ---
        public string DeploymentChecksum { get; set; }
        public string ModuleVersion { get; set; }
    }

    public class FinalizerResponseProperties { }

    public abstract class ALambdaFinalizerFunction : ALambdaCustomResourceFunction<FinalizerRequestProperties, FinalizerResponseProperties> {

        //--- Methods ---
        protected virtual async Task<string> CreateDeployment(FinalizerRequestProperties request) => request.DeploymentChecksum;
        protected virtual async Task<string> UpdateDeployment(FinalizerRequestProperties current, FinalizerRequestProperties previous) => previous.DeploymentChecksum;
        protected virtual async Task DeleteDeployment(FinalizerRequestProperties current) { }

        protected override async Task<Response<FinalizerResponseProperties>> HandleCreateResourceAsync(Request<FinalizerRequestProperties> request) {
            var id = await CreateDeployment(request.ResourceProperties);
            return new Response<FinalizerResponseProperties> {
                PhysicalResourceId = "Finalizer:" + id,
                Properties = new FinalizerResponseProperties { }
            };
        }

        protected override async Task<Response<FinalizerResponseProperties>> HandleDeleteResourceAsync(Request<FinalizerRequestProperties> request) {
            await DeleteDeployment(request.ResourceProperties);
            return new Response<FinalizerResponseProperties>();
        }

        protected override async Task<Response<FinalizerResponseProperties>> HandleUpdateResourceAsync(Request<FinalizerRequestProperties> request) {
            var id = await UpdateDeployment(request.ResourceProperties, request.OldResourceProperties);
            return new Response<FinalizerResponseProperties> {
                PhysicalResourceId = "Finalizer:" + id,
                Properties = new FinalizerResponseProperties { }
            };
        }
    }
}
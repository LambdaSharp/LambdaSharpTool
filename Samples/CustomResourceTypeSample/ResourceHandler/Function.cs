/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2022
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

namespace CustomResourceSample.ResourceHandler;

using LambdaSharp;
using LambdaSharp.CustomResource;

public class ResourceProperties {

    //--- Properties ---

    // TO-DO: add expected custom resource properties
}

public class ResourceAttributes {

    //--- Properties ---

    // TO-DO: add returned custom resource attributes
}

public sealed class Function : ALambdaCustomResourceFunction<ResourceProperties, ResourceAttributes> {

    //--- Constructors ---
    public Function() : base(new LambdaSharp.Serialization.LambdaSystemTextJsonSerializer()) { }

    //--- Methods ---
    public override Task InitializeAsync(LambdaConfig config)
        => Task.CompletedTask;

    public override async Task<Response<ResourceAttributes>> ProcessCreateResourceAsync(Request<ResourceProperties> request, CancellationToken cancellationToken) {

        // TO-DO: create custom resource using resource properties from request

        return new Response<ResourceAttributes> {

            // TO-DO: assign a physical resource ID for custom resource
            PhysicalResourceId = "MyResource:123",

            // TO-DO: set response attributes
            Attributes = new ResourceAttributes { }
        };
    }

    public override async Task<Response<ResourceAttributes>> ProcessDeleteResourceAsync(Request<ResourceProperties> request, CancellationToken cancellationToken) {

        // TO-DO: delete custom resource identified by PhysicalResourceId in request

        return new Response<ResourceAttributes>();
    }

    public override async Task<Response<ResourceAttributes>> ProcessUpdateResourceAsync(Request<ResourceProperties> request, CancellationToken cancellationToken) {

        // TO-DO: update custom resource using resource properties from request

        return new Response<ResourceAttributes> {

            // TO-DO: optionally assign a new physical resource ID to trigger deletion of the previous custom resource
            PhysicalResourceId = "MyResource:123",

            // TO-DO: set response attributes
            Attributes = new ResourceAttributes { }
        };
    }
}

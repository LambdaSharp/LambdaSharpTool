using System;
using System.Threading;
using System.Threading.Tasks;
using LambdaSharp;
using LambdaSharp.CustomResource;

namespace Legacy.ModuleV082.MyCustomResourceFunction {

    public class ResourceProperties {

        //--- Properties ---

        // TO-DO: add request resource properties
    }

    public class ResourceAttributes {

        //--- Properties ---

        // TO-DO: add response resource attributes
    }

    public sealed class Function : ALambdaCustomResourceFunction<ResourceProperties, ResourceAttributes> {

        //--- Constructors ---
        public Function() : base(new LambdaSharp.Serialization.LambdaSystemTextJsonSerializer()) { }

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {

            // TO-DO: add function initialization and reading configuration settings
        }

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
}

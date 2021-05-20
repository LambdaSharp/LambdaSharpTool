using System;
using System.Threading.Tasks;
using LambdaSharp;
using LambdaSharp.ApiGateway;

namespace Legacy.ModuleV082.MyApiGatewayFunction {

    public class ApiGatewayEndpointRequest {

        //--- Properties ---

        // TO-DO: add endpoint request properties
    }

    public class ApiGatewayEndpointResponse {

        //--- Properties ---

        // TO-DO: add endpoint response properties
    }

    public sealed class Function : ALambdaApiGatewayFunction {

        //--- Constructors ---
        public Function() : base(new LambdaSharp.Serialization.LambdaSystemTextJsonSerializer()) { }

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {

            // TO-DO: add function initialization and reading configuration settings
        }

        public async Task<ApiGatewayEndpointResponse> ApiGatewayEndpointAsync(ApiGatewayEndpointRequest request) {

            // TO-DO: add business logic for API Gateway resource endpoint
            return new ApiGatewayEndpointResponse { };
        }
    }
}

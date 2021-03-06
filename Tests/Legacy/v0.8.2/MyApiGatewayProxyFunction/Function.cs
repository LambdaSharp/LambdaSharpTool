using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using LambdaSharp;
using LambdaSharp.ApiGateway;

namespace Legacy.ModuleV082.MyApiGatewayProxyFunction {

    public sealed class Function : ALambdaApiGatewayFunction {

        //--- Constructors ---
        public Function() : base(new LambdaSharp.Serialization.LambdaSystemTextJsonSerializer()) { }

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {

            // TO-DO: add function initialization and reading configuration settings
        }

        public override async Task<APIGatewayProxyResponse> ProcessProxyRequestAsync(APIGatewayProxyRequest request) {

            // TO-DO: add business logic for API Gateway proxy request handling

            return new APIGatewayProxyResponse {
                Body = "Ok",
                Headers = new Dictionary<string, string> {
                    ["Content-Type"] = "text/plain"
                },
                StatusCode = 200
            };
        }
    }
}

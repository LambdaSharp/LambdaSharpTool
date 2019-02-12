using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using LambdaSharp;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace LambdaSharp.Sample.LambdaLayer.MyFunction {

    public class FunctionRequest {

        //--- Properties ---

        // TODO: add request fields
    }

    public class FunctionResponse {

        //--- Properties ---

        // TODO: add response fields
    }

    public class Function : ALambdaFunction<FunctionRequest, FunctionResponse> {

        //--- Methods ---
        public override Task InitializeAsync(LambdaConfig config)
            => Task.CompletedTask;

        public override async Task<FunctionResponse> ProcessMessageAsync(FunctionRequest request, ILambdaContext context) {

            // TODO: add business logic

            return new FunctionResponse();
        }
    }
}

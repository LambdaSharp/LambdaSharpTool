using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;

await LambdaBootstrapBuilder.Create<FunctionRequest, FunctionResponse>(Handler, new DefaultLambdaJsonSerializer())
    .Build()
    .RunAsync();

async Task<FunctionResponse> Handler(FunctionRequest request, ILambdaContext context) {
    Console.WriteLine("*** INFO: Top-Level function invocation");
    return new FunctionResponse {
        Message = "Success!!!"
    };
}

public class FunctionRequest { }

public class FunctionResponse {

    //--- Properties ---
    public string? Message { get; set; }
}

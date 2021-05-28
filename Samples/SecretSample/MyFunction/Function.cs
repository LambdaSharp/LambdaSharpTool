using System;
using System.Threading.Tasks;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using LambdaSharp;

namespace Test.TestModule.MyFunction {

    public class FunctionRequest { }
    public class FunctionResponse { }

    public sealed class Function : ALambdaFunction<FunctionRequest, FunctionResponse> {

        //--- Fields ---
        private string _secretArn;
        private IAmazonSecretsManager _secretManagerClient;

        //--- Constructors ---
        public Function() : base(new LambdaSharp.Serialization.LambdaSystemTextJsonSerializer()) { }

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {

            // read configuration settings
            _secretArn = config.ReadText("CredentialsSecret");

            // create clients
            _secretManagerClient = new AmazonSecretsManagerClient();
        }

        public override async Task<FunctionResponse> ProcessMessageAsync(FunctionRequest request) {
            LogInfo("retrieving secret");
            var secret = await _secretManagerClient.GetSecretValueAsync(new GetSecretValueRequest {
                SecretId = _secretArn
            });
            LogInfo($"Received: {secret.SecretString}");
            return new FunctionResponse();
        }
    }
}

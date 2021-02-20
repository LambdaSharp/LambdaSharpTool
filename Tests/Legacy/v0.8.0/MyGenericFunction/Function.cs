using System;
using System.Threading.Tasks;
using LambdaSharp;

namespace Legacy.ModuleV080.MyGenericFunction {

    public class FunctionRequest {

        //--- Properties ---

        // TO-DO: add request fields
    }

    public class FunctionResponse {

        //--- Properties ---

        // TO-DO: add response fields
    }

    public sealed class Function : ALambdaFunction<FunctionRequest, FunctionResponse> {

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {

            // TO-DO: add function initialization and reading configuration settings
        }

        public override async Task<FunctionResponse> ProcessMessageAsync(FunctionRequest request) {

            // TO-DO: add business logic

            return new FunctionResponse();
        }
    }
}

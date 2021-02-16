using System;
using System.Threading.Tasks;
using LambdaSharp;
using LambdaSharp.SimpleQueueService;

namespace Legacy.ModuleV080.MyQueueFunction {

    public class Message {

        //--- Properties ---

        // TO-DO: add message properties
    }

    public sealed class Function : ALambdaQueueFunction<Message> {

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {

            // TO-DO: add function initialization and reading configuration settings
        }

        public override async Task ProcessMessageAsync(Message message) {

            // TO-DO: add business logic
        }
    }
}

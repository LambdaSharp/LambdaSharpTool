using System;
using System.Threading.Tasks;
using LambdaSharp;
using LambdaSharp.SimpleQueueService;

namespace Legacy.ModuleV082.MyQueueFunction {

    public class Message {

        //--- Properties ---

        // TO-DO: add message properties
    }

    public sealed class Function : ALambdaQueueFunction<Message> {

        //--- Constructors ---
        public Function() : base(new LambdaSharp.Serialization.LambdaSystemTextJsonSerializer()) { }

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {

            // TO-DO: add function initialization and reading configuration settings
        }

        public override async Task ProcessMessageAsync(Message message) {

            // TO-DO: add business logic
        }
    }
}

using System;
using System.Threading.Tasks;
using LambdaSharp;
using LambdaSharp.EventBridge;

namespace Legacy.ModuleV082.MyEventFunction {

    public class EventMessage {

        //--- Properties ---

        // TO-DO: add message properties
    }

    public sealed class Function : ALambdaEventFunction<EventMessage> {

        //--- Constructors ---
        public Function() : base(new LambdaSharp.Serialization.LambdaSystemTextJsonSerializer()) { }

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {

            // TO-DO: add function initialization and reading configuration settings
        }

        public override async Task ProcessEventAsync(EventMessage message) {

            // TO-DO: add business logic
        }
    }
}

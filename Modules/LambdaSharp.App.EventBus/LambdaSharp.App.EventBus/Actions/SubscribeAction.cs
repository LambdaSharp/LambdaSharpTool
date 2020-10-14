namespace LambdaSharp.App.EventBus.Actions {

    public sealed class SubscribeAction : ARuleAction {

        //--- Constructors ---
        public SubscribeAction() => Action = "Subscribe";

        //--- Properties ---
        public string Pattern { get; set; }
    }
}
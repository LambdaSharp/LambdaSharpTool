namespace LambdaSharp.App.EventBus.Actions {

    public sealed class AcknowledgeAction : ARuleAction {

        //--- Constructors ---
        public AcknowledgeAction() => Action = "Ack";

        //--- Properties ---
        public string Status { get; set; }
        public string Message { get; set; }
    }
}
namespace LambdaSharp.App.EventBus.Actions {

    public sealed class UnsubscribeAction : ARuleAction {

        //--- Constructors ---
        public UnsubscribeAction() => Action = "Unsubscribe";
    }
}
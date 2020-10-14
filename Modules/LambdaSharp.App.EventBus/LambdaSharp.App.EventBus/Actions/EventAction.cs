using System.Collections.Generic;

namespace LambdaSharp.App.EventBus.Actions {

    public sealed class EventAction : AnAction {

        //--- Constructors ---
        public EventAction() => Action = "Event";

        //--- Properties ---
        public List<string> Rules { get; set; }
        public string Source { get; set; }
        public string Type { get; set; }
        public string Event { get; set; }
    }
}
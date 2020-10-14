using System.Collections.Generic;
using Newtonsoft.Json;

namespace LambdaSharp.App.EventBus.BroadcastFunction {

    public sealed class CloudWatchEventPayload {

        //--- Properties ---

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("detail-type")]
        public string DetailType { get; set; }

        [JsonProperty("resources")]
        public List<string> Resources { get; set; }
    }
}

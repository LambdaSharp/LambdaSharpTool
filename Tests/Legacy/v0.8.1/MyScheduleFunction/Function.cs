using System;
using System.Threading.Tasks;
using LambdaSharp;
using LambdaSharp.Schedule;

namespace Legacy.ModuleV081.MyScheduleFunction {

    public sealed class Function : ALambdaScheduleFunction {

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {

            // TO-DO: add function initialization and reading configuration settings
        }

        public override async Task ProcessEventAsync(LambdaScheduleEvent schedule) {

            // TO-DO: add business logic
        }
    }
}

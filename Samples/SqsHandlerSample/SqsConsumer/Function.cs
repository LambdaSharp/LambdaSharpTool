using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using LambdaSharp;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace SqsSample.Consumer {

    public class Function: ALambdaSqsFunction<int> {

        //--- Methods ---
        public override Task InitializeAsync(LambdaConfig config) {
            return Task.CompletedTask;
        }

        public override Task HandleSqsMessageAsync(int message, Dictionary<string, SQSEvent.MessageAttribute> messageAttributes) {
            LogInfo(message.ToString());
            if(message % 10 == 0) {
                LogWarn("Retriable Error");
                throw new SqsMessageRetriableException("Retriable Error!");
            }
            if(message % 5 == 0) {
                LogWarn("Non Retriable Error");
                throw new Exception("Non Retriable Error");
            }
            return Task.CompletedTask;
        }
    }
}

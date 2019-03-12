using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.SQS;
using Amazon.SQS.Model;
using LambdaSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace SqsSample.Producer {

    public class Function : ALambdaFunction<int, string> {

        //--- Fields ---
        private string _sqsQueueUrl;
        private IAmazonSQS _sqs;
        
        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {
            _sqsQueueUrl = config.ReadText("QueueUrl");
            _sqs = new AmazonSQSClient();
        }

        public override async Task<string> ProcessMessageAsync(int request, ILambdaContext context) {
            for(var i = 0; i < request; i++) {
                await _sqs.SendMessageAsync(_sqsQueueUrl, i.ToString());
            }
            return "OK";
        }
    }
}

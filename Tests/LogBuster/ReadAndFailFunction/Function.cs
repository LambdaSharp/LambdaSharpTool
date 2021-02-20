using System;
using System.Threading.Tasks;
using Amazon.SQS;
using LambdaSharp;
using LambdaSharp.SimpleQueueService;

namespace LogBuster.ReadAndFailFunction {

    public class Message {

        //--- Properties ---
        public int Counter;
    }

    public sealed class Function : ALambdaQueueFunction<Message> {

        //--- Fields ---
        private string _queueUrl;
        private IAmazonSQS _sqsClient;

        //--- Constructors ---
        public Function() : base(new LambdaSharp.Serialization.LambdaSystemTextJsonSerializer()) { }

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {

            // read configuration settings
            _queueUrl = config.ReadSqsQueueUrl("Queue");

            // initialize clients
            _sqsClient = new AmazonSQSClient();
        }

        public override async Task ProcessMessageAsync(Message message) {
            await Task.WhenAll(
                _sqsClient.SendMessageAsync(_queueUrl, LambdaSerializer.Serialize(new Message {
                    Counter = message.Counter + 1
                })),
                _sqsClient.SendMessageAsync(_queueUrl, LambdaSerializer.Serialize(new Message {
                    Counter = message.Counter + 2
                }))
            );
            throw new Exception("oops!");
        }
    }
}

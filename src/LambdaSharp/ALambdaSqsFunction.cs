/*
 * MindTouch λ#
 * Copyright (C) 2018-2019 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit mindtouch.com;
 * please review the licensing section.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.SQS;
using Amazon.SQS.Model;
using Newtonsoft.Json;
 
namespace LambdaSharp {
    public class SqsMessageRetriableException: ALambdaRetriableException {
        public SqsMessageRetriableException(string message): base(message){}
    }
    
    public static class SqsExtensions {
    
        //--- Extension Methods ---
        public static string QueueUrl(this SQSEvent.SQSMessage message) {
            
            // The EventSourceArn is the SQS queue where the message came from
            // arn:aws:sqs:region:account-id:queue-name
            var arnParts = message.EventSourceArn.Split(':');
            var queueName = arnParts.Last();
            var region = message.AwsRegion;
            var accountId = arnParts[4];
            return $"https://sqs.{region}.amazonaws.com/{accountId}/{queueName}";
        }
        
        public static Task DeleteAsync(this IList<SQSEvent.SQSMessage>messages, IAmazonSQS sqs) {
            var queueUrl = messages.First().QueueUrl();
            var requestEntries = messages.Select(m => 
                new DeleteMessageBatchRequestEntry(m.MessageId, m.ReceiptHandle)
            ).ToList();
            return sqs.DeleteMessageBatchAsync(new DeleteMessageBatchRequest {
                QueueUrl = queueUrl,
                Entries = requestEntries
            });
        }
    } 
    
    public abstract class ALambdaSqsFunction<TMessage>: ALambdaFunction<SQSEvent, string> {
        
        //--- Fields ---
        private readonly IAmazonSQS _sqs;
        
        //--- Constructors ---
        public ALambdaSqsFunction() {
            _sqs = new AmazonSQSClient();
        }

        //--- Methods ---
        public override async Task<string> ProcessMessageAsync(SQSEvent sqsEvent, ILambdaContext context) {
            
            var exceptions = new List<Exception>();
            var messagesToAck = new List<SQSEvent.SQSMessage>();
            foreach (var record in sqsEvent.Records) {
                try {
                    var message = JsonConvert.DeserializeObject<TMessage>(record.Body);
                    await HandleSqsMessageAsync(message, record.MessageAttributes);
                    LogInfo("Message successfully processed: " + record.ReceiptHandle);
                    messagesToAck.Add(record);
                } catch(ALambdaRetriableException e) {
                    
                    // Record retriable errors and continue
                    LogErrorAsWarning(e);
                    exceptions.Add(e);
                } catch(Exception e) {
                
                    // Send straight to the dead letter queue and prevent from re-trying
                    await RecordFailedMessageAsync(LambdaLogLevel.ERROR, record.Body, e);
                    messagesToAck.Add(record);
                }
            }
            
            // Ack messages by deleting from the queue, this will prevent successful
            // messages from showing up in the queue again
            if(messagesToAck.Count > 0) {
                await messagesToAck.DeleteAsync(_sqs);
            }
            
            // If any retriable errors were recorded, throw an exception so they re-appear in the queue
            if(exceptions.Count > 0) {
                throw new Exception($"There were {exceptions.Count} errors.");
            }
            return "OK";
        }
        
        public abstract Task HandleSqsMessageAsync(TMessage message, Dictionary<string, SQSEvent.MessageAttribute> messageAttributes);
        
    }
}
/*
 * MindTouch λ#
 * Copyright (C) 2018 MindTouch, Inc.
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
using System.Threading.Tasks;
using System.Linq;
using Amazon.Lambda.Core;
using MindTouch.LambdaSharp;
using MindTouch.LambdaSharp.Slack;
using MindTouch.LambdaSharpDemo.Common;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace MindTouch.LambdaSharpDemo.SlackCommand {

    public class Function : ALambdaSlackCommandFunction {

        //-- Fields ---
        private MessageTable _table;

        //--- Methods ---
        public override Task InitializeAsync(LambdaConfig config) {
            var tableName = AwsConverters.ConvertDynamoDBArnToName(config.ReadText("MessageTable"));
            _table = new MessageTable(tableName);
            return Task.CompletedTask;
        }

        protected async override Task HandleSlackRequestAsync(SlackRequest request) {

            // parse request into two strings and check if the first one is a recognized command
            var args = request.Text?.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries) ?? new[] { "" };
            switch(args[0].ToLowerInvariant()) {
            case "clear": {
                    var messages = await _table.ListMessagesAsync();

                    // enumerate message from the message table
                    if(messages.Any()) {
                        foreach(var message in messages) {
                            await _table.DeleteMessage(message.MessageId);
                        }
                        Console.WriteLine($"{messages.Count():N0} messages cleared.");
                    } else {
                        Console.WriteLine("There are no messages to clear out.");
                    }
                }
                break;
            case "list": {
                    var messages = await _table.ListMessagesAsync();

                    // enumerate message from the message table
                    if(messages.Any()) {
                        Console.WriteLine($"{messages.Count():N0} messages found.");
                        var count = 0;
                        foreach(var message in messages) {
                            Console.WriteLine($"{++count:N0}: {message.Text} [from {message.Source}]");
                        }
                    } else {
                        Console.WriteLine("There are no messages.");
                    }
                }
                break;
            case "send":

                // add a new message to the message table
                if((args.Length == 1) || string.IsNullOrWhiteSpace(args[1])) {
                    Console.WriteLine("No messages after the `send` command to send.");
                } else {
                    await _table.InsertMessageAsync(new Message {
                        Source = "Slack",
                        Text = args[1]
                    });
                    Console.WriteLine("Message sent.");
                }
                break;
            case "error":
                throw new Exception("Oh no, I haz br0ken!");
            default:
                Console.WriteLine("Sorry, I only understand `send`, `list`, and `clear` commands.");
                break;
            }
        }
    }
}

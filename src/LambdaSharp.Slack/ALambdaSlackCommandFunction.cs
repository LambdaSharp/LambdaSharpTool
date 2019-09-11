/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2019
 * lambdasharp.net
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
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.Json;
using LambdaSharp.ConfigSource;


namespace LambdaSharp.Slack {

    public class SlackVerificationTokenMismatchException : Exception {

        //--- Constructors ---
        public SlackVerificationTokenMismatchException() : base("Slack verification token does not match") {}
    }

    public abstract class ALambdaSlackCommandFunction : ALambdaFunction {

        //--- Fields ---
        private string _slackVerificationToken;

        //--- Abstract Methods ---
        protected abstract Task ProcessSlackRequestAsync(SlackRequest request);

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {

            // check if an alternative name for the slack token was given
            _slackVerificationToken = config.ReadText("SlackToken", defaultValue: null);
            if(_slackVerificationToken == "") {
                _slackVerificationToken = null;
            }
        }

        public override sealed async Task<Stream> ProcessMessageStreamAsync(Stream stream) {

            // sns event deserialization
            LogInfo("reading message stream");
            SlackRequest request;
            try {
                request = DeserializeJson<SlackRequest>(stream);
            } catch(Exception e) {
                LogError(e, "failed during Slack request deserialization");
                return $"ERROR: {e.Message}".ToStream();
            }

            // capture standard output and error output so we can send it to slack instead
            using(var consoleOutWriter = new StringWriter())
            using(var consoleErrorWriter = new StringWriter()) {
                var consoleOutOriginal = Console.Out;
                var consoleErrorOriginal = Console.Error;
                try {

                    // redirect the console output and error streams so we can emit them later to slack
                    Console.SetOut(consoleOutWriter);
                    Console.SetError(consoleErrorWriter);

                    // validate the slack token (assuming one was configured)
                    if(!(_slackVerificationToken?.Equals(request.Token) ?? true)) {
                        throw new SlackVerificationTokenMismatchException();
                    }

                    // handle slack request
                    await ProcessSlackRequestAsync(request);
                } catch(Exception e) {
                    LogError(e);
                    Console.Error.WriteLine(e);
                } finally {
                    Console.SetOut(consoleOutOriginal);
                    Console.SetError(consoleErrorOriginal);
                }

                // send console output to slack as an in_channel response
                var output = consoleOutWriter.ToString();
                if(output.Length > 0) {
                    await RespondInChannel(request, output);
                }

                // send console error to slack as an ephemeral response (only visible to the requesting user)
                var error = consoleErrorWriter.ToString();
                if(error.Length > 0) {
                    await RespondEphemeral(request, error);
                }
            }
            return "Ok".ToStream();
        }

        protected Task<bool> RespondInChannel(SlackRequest request, string text, params SlackResponseAttachment[] attachments)
            => Respond(request, SlackResponse.InChannel(text, attachments));

        protected Task<bool> RespondEphemeral(SlackRequest request, string text, params SlackResponseAttachment[] attachments)
            => Respond(request, SlackResponse.Ephemeral(text, attachments));

        protected async Task<bool> Respond(SlackRequest request, SlackResponse response) {
            var httpResponse = await HttpClient.SendAsync(new HttpRequestMessage {
                RequestUri = new Uri(request.ResponseUrl),
                Method = HttpMethod.Post,
                Content = new StringContent(SerializeJson(response))
            });
            return httpResponse.StatusCode == HttpStatusCode.OK;
        }
    }
}
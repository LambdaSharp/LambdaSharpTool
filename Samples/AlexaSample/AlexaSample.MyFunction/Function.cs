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
using Alexa.NET;
using Alexa.NET.Request;
using Alexa.NET.Request.Type;
using Alexa.NET.Response;
using Amazon.Lambda.Core;
using MindTouch.LambdaSharp;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace AlexaSample.MyFunction {

    public class Function : ALambdaFunction<SkillRequest, SkillResponse> {

        //--- Methods ---
        public override Task InitializeAsync(LambdaConfig config)
            => Task.CompletedTask;

        public override async Task<SkillResponse> ProcessMessageAsync(SkillRequest skill, ILambdaContext context) {
            switch(skill.Request) {
            case LaunchRequest launch:
                LogInfo("Launch");
                break;
            case IntentRequest intent:
                LogInfo("Intent");
                LogInfo($"Intent.Name = {intent.Intent.Name}");
                break;
            case SessionEndedRequest ended:
                LogInfo("Session ended");
                return ResponseBuilder.Empty();
            }
            return ResponseBuilder.Tell(new PlainTextOutputSpeech {
                Text = "Hi!"
            });
        }
    }
}
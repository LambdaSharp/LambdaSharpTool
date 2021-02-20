/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2021
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

using System.Threading.Tasks;
using Alexa.NET;
using Alexa.NET.Request;
using Alexa.NET.Request.Type;
using Alexa.NET.Response;
using LambdaSharp;

namespace AlexaSample.MyFunction {

    public sealed class Function : ALambdaFunction<SkillRequest, SkillResponse> {

        //--- Constructors ---

        // NOTE (2021-01-04, bjorg): Alexa.NET uses Newtonsoft.Json for serialization
        public Function() : base(new LambdaSharp.Serialization.LambdaNewtonsoftJsonSerializer()) { }

        //--- Methods ---
        public override Task InitializeAsync(LambdaConfig config)
            => Task.CompletedTask;

        public override async Task<SkillResponse> ProcessMessageAsync(SkillRequest skill) {
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
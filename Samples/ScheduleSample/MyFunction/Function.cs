/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2020
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
using LambdaSharp;
using LambdaSharp.Schedule;

namespace ScheduleSample.MyFunction {

    public sealed class Function : ALambdaScheduleFunction {

        //--- Methods ---
        public override Task InitializeAsync(LambdaConfig config)
            => Task.CompletedTask;

        public override async Task ProcessEventAsync(LambdaScheduleEvent schedule) {
            LogInfo($"Id = {schedule.Id}");
            LogInfo($"Time = {schedule.Time}");
            LogInfo($"Name = '{schedule.Name}'");
        }
    }
}
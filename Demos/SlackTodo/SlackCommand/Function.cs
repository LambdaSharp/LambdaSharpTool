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
using System.Threading.Tasks;
using System.Linq;
using Amazon.Lambda.Core;
using LambdaSharp;
using LambdaSharp.Slack;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Demo.SlackTodo {

    public class Function : ALambdaSlackCommandFunction {

        //-- Fields ---
        private TaskTable _table;

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {
            _table = new TaskTable(config.ReadDynamoDBTableName("TaskTable"));
        }

        protected async override Task ProcessSlackRequestAsync(SlackRequest request) {

            // parse request into two strings and check if the first one is a recognized command
            var args = request.Text?.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries) ?? new[] { "" };
            switch(args[0].ToLowerInvariant()) {
            case "add":
                if((args.Length == 1) || string.IsNullOrWhiteSpace(args[1])) {
                    Console.WriteLine("Missing task after the 'add' command.");
                } else {
                    await AddTask(request.UserId, args[1]);
                }
                break;
            case "remove":
                if((args.Length == 1) || string.IsNullOrWhiteSpace(args[1]) || !int.TryParse(args[1], out var index)) {
                    Console.WriteLine("Missing or invalid task number after the `remove` command.");
                } else if(!await RemoveTask(request.UserId, index)) {
                    Console.WriteLine("Invalid task number after the `remove` command.");
                }
                break;
            case "list":
                await ListTasks(request.UserId);
                break;
            case "clear":
                await ClearTasks(request.UserId);
                break;
            default:
                Console.WriteLine("Sorry, I only understand `add`, `remove`, `list`, and `clear` commands.");
                break;
            }
        }

        private async Task ClearTasks(string userId) {
            var tasks = await _table.ListTasksAsync(userId);
            if(tasks.Any()) {
                foreach(var task in tasks) {
                    await _table.DeleteTask(task.TaskId);
                }
                Console.WriteLine($"{tasks.Count():N0} tasks cleared.");
            } else {
                Console.WriteLine("There are no tasks to clear.");
            }
        }

        private async Task ListTasks(string userId) {
            var tasks = await _table.ListTasksAsync(userId);
            var now = DateTimeOffset.UtcNow;
            if(tasks.Any()) {
                Console.WriteLine($"{tasks.Count():N0} tasks found.");
                var count = 0;
                foreach(var task in tasks) {
                    Console.WriteLine($"{++count:N0}: {task.Text} [{TaskTimestampDelta(task.Timestamp)}]");
                }
            } else {
                Console.WriteLine("There are no tasks.");
            }

            // local functions
            string TaskTimestampDelta(DateTimeOffset taskTimestamp) {
                var delta = now - taskTimestamp;
                if(delta < TimeSpan.FromSeconds(10)) {
                    return "now";
                }
                if(delta < TimeSpan.FromSeconds(60)) {
                    return $"{delta.TotalSeconds:N0} sec(s) ago";
                }
                if(delta < TimeSpan.FromMinutes(60)) {
                    return $"{delta.TotalMinutes:N0} min(s) ago";
                }
                if(delta < TimeSpan.FromHours(24)) {
                    return $"{delta.TotalHours:N0} hour(s) ago";
                }
                return $"{delta.TotalDays:N0} day(s) ago";
            }
        }

        private async Task AddTask(string userId, string task) {
            await _table.InsertTaskAsync(new TaskRecord {
                UserId = userId,
                Text = task
            });
            Console.WriteLine("Task added.");
        }

        private async Task<bool> RemoveTask(string userId, int index) {
            var tasks = await _table.ListTasksAsync(userId);
            if((index < 0) || (index >= tasks.Count())) {
                return false;
            }
            var task = tasks.ElementAt(index - 1);
            await _table.DeleteTask(task.TaskId);
            Console.WriteLine("Task removed.");
            return true;
        }
    }
}

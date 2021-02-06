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

using System;
using System.Collections.Generic;
using System.Linq;
using LambdaSharp.Tool.Model;

namespace LambdaSharp.Tool.Cli.Build {
    using static ModelFunctions;

    public class ModelAppProcessor : AModelProcessor {

        //--- Fields ---
        private ModuleBuilder _builder;

        //--- Constructors ---
        public ModelAppProcessor(Settings settings, string sourceFilename) : base(settings, sourceFilename) { }

        //--- Methods ---
        public void Process(ModuleBuilder builder) {
            _builder = builder;
            var apps = _builder.Items.OfType<AppItem>().ToList();
            if(apps.Any()) {

                // add functions
                foreach(var app in apps) {
                    AddEventSources(app);
                }
            }
        }

        private void AddEventSources(AppItem app) {

            // add function sources
            for(var sourceIndex = 0; sourceIndex < app.Sources.Count; ++sourceIndex) {
                var source = app.Sources[sourceIndex];
                var sourceSuffix = (sourceIndex + 1).ToString();
                switch(source) {
                case ScheduleSource scheduleSource: {

                        // NOTE (2019-01-30, bjorg): we need the source suffix to support multiple sources
                        //  per app; however, we cannot exceed 64 characters in length for the ID.
                        var id = app.LogicalId;
                        if(id.Length > 61) {
                            id += id.Substring(0, 61) + "-" + sourceSuffix;
                        } else {
                            id += "-" + sourceSuffix;
                        }
                        var schedule = _builder.AddResource(
                            parent: app,
                            name: $"Source{sourceSuffix}ScheduleEvent",
                            description: null,
                            scope: null,
                            resource: new Humidifier.Events.Rule {
                                ScheduleExpression = scheduleSource.Expression,
                                Targets = new[] {
                                    new Humidifier.Events.RuleTypes.Target {
                                        Id = id,
                                        Arn = FnGetAtt($"{app.FullName}::EventBus", "Outputs.EventTopicArn"),
                                        InputTransformer = new Humidifier.Events.RuleTypes.InputTransformer {
                                            InputPathsMap = new Dictionary<string, object> {
                                                ["id"] = "$.id",
                                                ["time"] = "$.time"
                                            },
                                            InputTemplate =
@"{
    ""Id"": <id>,
    ""Time"": <time>,
    ""Name"": """ + scheduleSource.Name + @"""
}"
                                        }
                                    }
                                }.ToList()
                            },
                            resourceExportAttribute: null,
                            dependsOn: null,
                            condition: null,
                            pragmas: null,
                            deletionPolicy: null
                        );
                    }
                    break;
                case CloudWatchEventSource cloudWatchRuleSource: {

                        // NOTE (2019-01-30, bjorg): we need the source suffix to support multiple sources
                        //  per function; however, we cannot exceed 64 characters in length for the ID.
                        var id = app.LogicalId;
                        if(id.Length > 61) {
                            id += id.Substring(0, 61) + "-" + sourceSuffix;
                        } else {
                            id += "-" + sourceSuffix;
                        }
                        var rule = _builder.AddResource(
                            parent: app,
                            name: $"Source{sourceSuffix}Event",
                            description: null,
                            scope: null,
                            resource: new Humidifier.Events.Rule {
                                EventPattern = cloudWatchRuleSource.Pattern,
                                EventBusName = cloudWatchRuleSource.EventBus,
                                Targets = new[] {
                                    new Humidifier.Events.RuleTypes.Target {
                                        Id = id,
                                        Arn = FnGetAtt($"{app.FullName}::EventBus", "Outputs.EventTopicArn")
                                    }
                                }.ToList()
                            },
                            resourceExportAttribute: null,
                            dependsOn: null,
                            condition: null,
                            pragmas: null,
                            deletionPolicy: null
                        );
                    }
                    break;
                default:
                    throw new ApplicationException($"unrecognized app source type '{source?.GetType()}' for source #{sourceSuffix}");
                }
            }
        }
    }
}
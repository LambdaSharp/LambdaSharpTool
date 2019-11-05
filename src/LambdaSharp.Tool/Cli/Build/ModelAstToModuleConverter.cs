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
using System.Collections.Generic;
using System.Linq;
using LambdaSharp.Tool.Internal;
using LambdaSharp.Tool.Model;
using LambdaSharp.Tool.Model.AST;
using Newtonsoft.Json.Linq;

namespace LambdaSharp.Tool.Cli.Build {
    public class ModelAstToModuleConverter : AModelProcessor {

        //--- Constructors ---
        public ModelAstToModuleConverter(Settings settings, string sourceFilename) : base(settings, sourceFilename) { }

        //--- Methods ---
        public ModuleBuilder Convert(ModuleNode module) => throw new NotImplementedException("deleted code");

        private AFunctionSource ConvertFunctionSource(ModuleItemNode function, int index, FunctionSourceNode source) {
            var type = DeterminNodeType("source", index, source, FunctionSourceNode.FieldCombinations, new[] {
                "Api",
                "Schedule",
                "S3",
                "SlackCommand",
                "Topic",
                "Sqs",
                "Alexa",
                "DynamoDB",
                "Kinesis",
                "WebSocket"
            });
            switch(type) {
            case "Api":
                return AtLocation("Api", () => {

                    // extract http method from route
                    var api = source.Api.Trim();
                    var pathSeparatorIndex = api.IndexOfAny(new[] { ':', ' ' });
                    if(pathSeparatorIndex < 0) {
                        LogError("invalid api format");
                        return new RestApiSource {
                            HttpMethod = "ANY",
                            Path = new string[0],
                            Integration = ApiGatewaySourceIntegration.RequestResponse
                        };
                    }
                    var method = api.Substring(0, pathSeparatorIndex).ToUpperInvariant();
                    if(method == "*") {
                        method = "ANY";
                    }
                    var path = api.Substring(pathSeparatorIndex + 1).TrimStart().Split('/', StringSplitOptions.RemoveEmptyEntries);

                    // parse integration into a valid enum
                    var integration = AtLocation("Integration", () => Enum.Parse<ApiGatewaySourceIntegration>(source.Integration ?? "RequestResponse", ignoreCase: true));
                    return new RestApiSource {
                        HttpMethod = method,
                        Path = path,
                        Integration = integration,
                        OperationName = source.OperationName,
                        ApiKeyRequired = source.ApiKeyRequired,
                        AuthorizationType = source.AuthorizationType,
                        AuthorizationScopes =  source.AuthorizationScopes,
                        AuthorizerId = source.AuthorizerId,
                        Invoke = source.Invoke
                    };
                });
            case "Schedule":
                return AtLocation("Schedule", () => new ScheduleSource {
                    Expression = source.Schedule,
                    Name = source.Name
                });
            case "S3":
                return AtLocation("S3", () => new S3Source {
                    Bucket = source.S3,
                    Events = source.Events ?? new List<string> {

                        // default S3 events to listen to
                        "s3:ObjectCreated:*"
                    },
                    Prefix = source.Prefix,
                    Suffix = source.Suffix
                });
            case "SlackCommand":
                return AtLocation("SlackCommand", () => new RestApiSource {
                    HttpMethod = "POST",
                    Path = source.SlackCommand.Split('/', StringSplitOptions.RemoveEmptyEntries),
                    Integration = ApiGatewaySourceIntegration.SlackCommand,
                    OperationName = source.OperationName
                });
            case "Topic":
                return AtLocation("Topic", () => new TopicSource {
                    TopicName = source.Topic,
                    Filters = source.Filters
                });
            case "Sqs":
                return AtLocation("Sqs", () => new SqsSource {
                    Queue = source.Sqs,
                    BatchSize = source.BatchSize ?? 10
                });
            case "Alexa":
                return AtLocation("Alexa", () => new AlexaSource {
                    EventSourceToken = source.Alexa
                });
            case "DynamoDB":
                return AtLocation("DynamoDB", () => new DynamoDBSource {
                    DynamoDB = source.DynamoDB,
                    BatchSize = source.BatchSize ?? 100,
                    StartingPosition = source.StartingPosition ?? "LATEST"
                });
            case "Kinesis":
                return AtLocation("Kinesis", () => new KinesisSource {
                    Kinesis = source.Kinesis,
                    BatchSize = source.BatchSize ?? 100,
                    StartingPosition = source.StartingPosition ?? "LATEST"
                });
            case "WebSocket":
                return AtLocation("WebSocket", () => new WebSocketSource {
                    RouteKey = source.WebSocket.Trim(),
                    OperationName = source.OperationName,
                    ApiKeyRequired = source.ApiKeyRequired,
                    AuthorizationType = source.AuthorizationType,
                    AuthorizationScopes =  source.AuthorizationScopes,
                    AuthorizerId = source.AuthorizerId,
                    Invoke = source.Invoke
                });
            }
            return null;
        }

        private void ValidateFunctionSource(IEnumerable<FunctionSourceNode> sources) {
            var index = 0;
            foreach(var source in sources) {
                ++index;
                AtLocation($"{index}", () => {
                    if(source.Api != null) {

                        // TODO (2018-11-10, bjorg): validate REST API expression
                    } else if(source.Schedule != null) {

                        // TODO (2018-06-27, bjorg): add cron/rate expression validation
                    } else if(source.S3 != null) {

                        // TODO (2018-06-27, bjorg): add events, prefix, suffix validation
                    } else if(source.SlackCommand != null) {

                        // TODO (2018-11-10, bjorg): validate REST API expression
                    } else if(source.Topic != null) {

                        // nothing to validate
                    } else if(source.Sqs != null) {

                        // validate settings
                        AtLocation("BatchSize", () => {
                            if(source.BatchSize is string batchSizeText) {
                                if(!int.TryParse(batchSizeText, out var batchSize) || (batchSize < 1) || (batchSize > 10)) {
                                    LogError($"invalid BatchSize value: {source.BatchSize}");
                                }
                            }
                        });
                    } else if(source.Alexa != null) {

                        // TODO (2018-11-10, bjorg): validate Alexa Skill ID
                    } else if(source.DynamoDB != null) {

                        // validate settings
                        AtLocation("BatchSize", () => {
                            if(source.BatchSize is string batchSizeText) {
                                if(!int.TryParse(batchSizeText, out var batchSize) || (batchSize < 1) || (batchSize > 100)) {
                                    LogError($"invalid BatchSize value: {source.BatchSize}");
                                }
                            }
                        });
                        AtLocation("StartingPosition", () => {
                            if(source.StartingPosition is string) {
                                switch(source.StartingPosition) {
                                case "TRIM_HORIZON":
                                case "LATEST":
                                case null:
                                    break;
                                default:
                                    LogError($"invalid StartingPosition value: {source.StartingPosition}");
                                    break;
                                }
                            }
                        });
                    } else if(source.Kinesis != null) {

                        // validate settings
                        AtLocation("BatchSize", () => {
                            if(source.BatchSize is string batchSizeText) {
                                if(!int.TryParse(batchSizeText, out var batchSize) || (batchSize < 1) || (batchSize > 100)) {
                                    LogError($"invalid BatchSize value: {source.BatchSize}");
                                }
                            }
                        });
                        AtLocation("StartingPosition", () => {
                            if(source.StartingPosition is string) {
                                switch(source.StartingPosition) {
                                case "TRIM_HORIZON":
                                case "LATEST":
                                case null:
                                    break;
                                default:
                                    LogError($"invalid StartingPosition value: {source.StartingPosition}");
                                    break;
                                }
                            }
                        });
                    } else if(source.WebSocket != null) {

                        // TODO (2019-03-13, bjorg): validate WebSocket route expression
                    } else {
                        LogError("unknown source type");
                    }
                });
            }
        }

        private string DeterminNodeType(
            string itemName,
            int index,
            object instance,
            Dictionary<string, IEnumerable<string>> typeChecks,
            IEnumerable<string> expectedTypes
        ) {
            var instanceLookup = JObject.FromObject(instance);
            return AtLocation($"{index}", () => {

                // find all declaration fields with a non-null value; use alphabetical order for consistency
                var matches = typeChecks
                    .OrderBy(kv => kv.Key)
                    .Where(kv => IsFieldSet(kv.Key))
                    .Select(kv => new {
                        ItemType = kv.Key,
                        ValidFields = kv.Value
                    })
                    .ToArray();
                switch(matches.Length) {
                case 0:
                    LogError($"unknown {itemName} type");
                    return null;
                case 1:

                    // good to go
                    break;
                default:
                    LogError($"ambiguous {itemName} type: {string.Join(", ", matches.Select(kv => kv.ItemType))}");
                    return null;
                }

                // validate match
                var match = matches.First();
                var invalidFields = typeChecks

                    // collect all field names
                    .SelectMany(kv => kv.Value)
                    .Distinct()

                    // only keep names that are not defined for the matched type
                    .Where(field => !match.ValidFields.Contains(field))

                    // check if the field is set on the instance
                    .Where(field => IsFieldSet(field))
                    .OrderBy(field => field)
                    .ToArray();
                if(invalidFields.Any()) {
                    LogError($"'{string.Join(", ", invalidFields)}' cannot be used with '{match.ItemType}'");
                }

                // check if the matched item was expected
                if(!expectedTypes.Contains(match.ItemType)) {
                    LogError($"unexpected node type: {match.ItemType}");
                    return null;
                }
                return match.ItemType;
            });

            // local functions
            bool IsFieldSet(string field)
                => instanceLookup.TryGetValue(field, out var token) && (token.Type != JTokenType.Null);
        }
    }
}
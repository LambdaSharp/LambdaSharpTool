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
using LambdaSharp.Tool.Model;

namespace LambdaSharp.Tool.Cli.Build {
    using static ModelFunctions;

    public class ModelPostLinkerValidation : AModelProcessor {

        //--- Fields ---
        private ModuleBuilder _builder;

        //--- Constructors ---
        public ModelPostLinkerValidation(Settings settings, string sourceFilename) : base(settings, sourceFilename) { }

        //--- Methods ---
        public void Validate(ModuleBuilder builder) {
            _builder = builder;
            AtLocation("Items", () => {
                foreach(var item in builder.Items) {
                    AtLocation(item.FullName, () => {
                        switch(item) {
                        case FunctionItem functionItem:
                            ValidateFunction(functionItem);
                            break;
                        case ResourceItem resourceItem:
                            switch(resourceItem.Resource) {
                            case Humidifier.CloudFormation.Macro macro:
                                ValidateFunction((object)macro.FunctionName);
                                break;
                            }
                            break;
                        case ResourceTypeItem resourceTypeItem:
                            AtLocation(resourceTypeItem.CustomResourceType, () => {
                                ValidateHandler(resourceTypeItem.Handler);
                            });
                            break;
                        }
                    });
                }
            });
        }

        public void ValidateFunction(FunctionItem function) {
            var index = 0;
            foreach(var source in function.Sources) {
                AtLocation($"{++index}", () => {
                    switch(source) {
                    case TopicSource topicSource:
                        ValidateSourceParameter(topicSource.TopicName, "AWS::SNS::Topic");
                        break;
                    case ScheduleSource scheduleSource:

                        // no references to validate
                        break;
                    case RestApiSource apiGatewaySource:

                        // no references to validate
                        break;
                    case S3Source s3Source:
                        ValidateSourceParameter(s3Source.Bucket, "AWS::S3::Bucket");
                        break;
                    case SqsSource sqsSource:
                        ValidateSourceParameter(sqsSource.Queue, "AWS::SQS::Queue");
                        break;
                    case AlexaSource alexaSource:
                        break;
                    case DynamoDBSource dynamoDBSource:
                        ValidateSourceParameter(dynamoDBSource.DynamoDB, "AWS::DynamoDB::Table");
                        break;
                    case KinesisSource kinesisSource:
                        ValidateSourceParameter(kinesisSource.Kinesis, "AWS::Kinesis::Stream");
                        break;
                    }
                });
            }
        }

        private void ValidateSourceParameter(object value, string awsType) {
            if(value is string literalValue) {
                ValidateSourceParameter(literalValue);
            } else if(TryGetFnRef(value, out var refKey)) {
                ValidateSourceParameter(refKey);
            } else if(TryGetFnGetAtt(value, out var getAttKey, out var getAttAttribute) && (getAttAttribute == "Arn")) {
                ValidateSourceParameter(getAttKey);
            } else {
                LogWarn($"unable to validate expression has type {awsType}");
            }

            // local functions
            void ValidateSourceParameter(string fullName) {
                if(!_builder.TryGetItem(fullName, out var item)) {
                    LogError($"could not find function source {fullName}");
                    return;
                }
                switch(item) {
                case VariableItem _:
                case ParameterItem _:
                case PackageItem _:
                case ResourceItem _:
                case FunctionItem _:
                    if(awsType != item.Type) {
                        LogError($"function source '{fullName}' must be {awsType}, but was {item.Type}");
                    }
                    break;
                case ConditionItem _:
                    LogError($"function source '{fullName}' cannot be a condition '{item.FullName}'");
                    break;
                case MappingItem _:
                    LogError($"function source '{fullName}' cannot be a mapping '{item.FullName}'");
                    break;
                case ResourceTypeItem _:
                    LogError($"function source '{fullName}' cannot be a custom resource '{item.FullName}'");
                    break;
                default:
                    throw new ApplicationException($"unexpected item type: {item.GetType()}");
                }
            }
        }

        private void ValidateHandler(object handler) {
            if(!(handler is string fullName) && !TryGetFnRef(handler, out fullName)) {
                LogError("invalid expression");
                return;
            }
            if(!_builder.TryGetItem(fullName, out var item)) {
                LogError($"could not find handler item {fullName}");
                return;
            }
            switch(item) {
            case VariableItem _:
            case ParameterItem _:
            case PackageItem _:
            case ResourceItem _:
            case FunctionItem _:
                if((item.Type != "AWS::Lambda::Function") && (item.Type != "AWS::SNS::Topic")) {
                    LogError($"handler reference '{fullName}' must be either be AWS::SNS::Topic or AWS::Lambda::Function, but was {item.Type}");
                }
                break;
            case ConditionItem _:
                LogError($"handler reference '{fullName}' cannot be a condition '{item.FullName}'");
                break;
            case MappingItem _:
                LogError($"handler reference '{fullName}' cannot be a mapping '{item.FullName}'");
                break;
            case ResourceTypeItem _:
                LogError($"handler reference '{fullName}' cannot be a custom resource '{item.FullName}'");
                break;
            default:
                throw new ApplicationException($"unexpected item type: {item.GetType()}");
            }
        }

        private void ValidateFunction(object functionName) {
            if(!(functionName is string fullName) && !TryGetFnRef(functionName, out fullName)) {
                LogError("invalid expression");
                return;
            }
            if(!_builder.TryGetItem(fullName, out var item)) {
                LogError($"could not find function item {fullName}");
                return;
            }
            switch(item) {
            case VariableItem _:
            case ParameterItem _:
            case PackageItem _:
            case ResourceItem _:
            case FunctionItem _:
                if(item.Type != "AWS::Lambda::Function") {
                    LogError($"function reference '{fullName}' must be be AWS::Lambda::Function, but was {item.Type}");
                }
                break;
            case ConditionItem _:
                LogError($"function reference '{fullName}' cannot be a condition '{item.FullName}'");
                break;
            case MappingItem _:
                LogError($"function reference '{fullName}' cannot be a mapping '{item.FullName}'");
                break;
            case ResourceTypeItem _:
                LogError($"function reference '{fullName}' cannot be a custom resource '{item.FullName}'");
                break;
            default:
                throw new ApplicationException($"unexpected item type: {item.GetType()}");
            }
        }
    }
}
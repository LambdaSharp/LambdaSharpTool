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

using System.Collections.Generic;
using System.Text.Json;
using FluentAssertions;
using LambdaSharp.CloudFormation;
using LambdaSharp.CloudFormation.Template;
using Xunit;

namespace Tests.LambdaSharp.CloudFormation {

    public class SerializeTests {

        //--- Methods ---

        [Fact]
        public void SerializeEmptyTemplate() {

            // arrange
            var template = new CloudFormationTemplate();

            // act
            var json = JsonSerializer.Serialize(template, new JsonSerializerOptions {
              IgnoreNullValues = true,
              WriteIndented = true
            });

            // assert
            json.Should().Be(
@"{
  ""AWSTemplateFormatVersion"": ""2010-09-09""
}"
            );
        }


        [Fact]
        public void SerializeEmptyParameter() {

            // arrange
            var template = new CloudFormationTemplate {
                Parameters = {
                    ["Param1"] = new CloudFormationParameter {
                        Type = "String"
                    }
                }
            };

            // act
            var json = JsonSerializer.Serialize(template, new JsonSerializerOptions {
              IgnoreNullValues = true,
              WriteIndented = true
            });

            // assert
            json.Should().Be(
@"{
  ""AWSTemplateFormatVersion"": ""2010-09-09"",
  ""Parameters"": {
    ""Param1"": {
      ""Type"": ""String""
    }
  }
}"
            );
        }

        [Fact]
        public void SerializeParameter() {

            // arrange
            var template = new CloudFormationTemplate {
                Parameters = {
                    ["Param1"] = new CloudFormationParameter {
                        Type = "String",
                        Description = "Hello",
                        AllowedPattern = ".*",
                        AllowedValues = new List<string> {
                            "abc",
                            "def"
                        },
                        ConstraintDescription = "Constraint",
                        Default = "ghi",
                        MinLength = 1,
                        MaxLength = 10,
                        MinValue = 100,
                        MaxValue = 1000,
                        NoEcho = false
                    }
                }
            };

            // act
            var json = JsonSerializer.Serialize(template, new JsonSerializerOptions {
              IgnoreNullValues = true,
              WriteIndented = true
            });

            // assert
            json.Should().Be(
@"{
  ""AWSTemplateFormatVersion"": ""2010-09-09"",
  ""Parameters"": {
    ""Param1"": {
      ""Type"": ""String"",
      ""Description"": ""Hello"",
      ""AllowedPattern"": "".*"",
      ""AllowedValues"": [
        ""abc"",
        ""def""
      ],
      ""ConstraintDescription"": ""Constraint"",
      ""Default"": ""ghi"",
      ""MinLength"": 1,
      ""MaxLength"": 10,
      ""MinValue"": 100,
      ""MaxValue"": 1000,
      ""NoEcho"": false
    }
  }
}"
            );
        }


        [Fact]
        public void SerializeEmptyResource() {

            // arrange
            var template = new CloudFormationTemplate {
                Resources = {
                    ["Resource1"] = new CloudFormationResource("AWS::SNS::Topic") {
                        ["TopicName"] = new CloudFormationLiteral("topic-name")
                    }
                }
            };

            // act
            var json = JsonSerializer.Serialize(template, new JsonSerializerOptions {
              IgnoreNullValues = true,
              WriteIndented = true
            });

            // assert
            json.Should().Be(
@"{
  ""AWSTemplateFormatVersion"": ""2010-09-09"",
  ""Resources"": {
    ""Resource1"": {
      ""Type"": ""AWS::SNS::Topic"",
      ""Properties"": {
        ""TopicName"": ""topic-name""
      }
    }
  }
}"
            );
        }

        [Fact]
        public void SerializeResource() {

            // arrange
            var template = new CloudFormationTemplate {
                Resources = {
                    ["Resource1"] = new CloudFormationResource("AWS::SNS::Topic") {
                        Properties = {
                            ["TopicName"] = new CloudFormationLiteral("topic-name")
                        },
                        DependsOn = {
                            "Resource1"
                        },
                        Metadata = {
                            ["meta-data"] = new CloudFormationObject {
                                ["OriginalName"] = new CloudFormationLiteral("name")
                            }
                        },
                        Condition = "Condition1",
                        DeletionPolicy = "Retain"
                    }
                }
            };

            // act
            var json = JsonSerializer.Serialize(template, new JsonSerializerOptions {
              IgnoreNullValues = true,
              WriteIndented = true
            });

            // assert
            json.Should().Be(
@"{
  ""AWSTemplateFormatVersion"": ""2010-09-09"",
  ""Resources"": {
    ""Resource1"": {
      ""Type"": ""AWS::SNS::Topic"",
      ""Properties"": {
        ""TopicName"": ""topic-name""
      },
      ""DependsOn"": [
        ""Resource1""
      ],
      ""Metadata"": {
        ""meta-data"": {
          ""OriginalName"": ""name""
        }
      },
      ""Condition"": ""Condition1"",
      ""DeletionPolicy"": ""Retain""
    }
  }
}"
            );
        }
    }
}
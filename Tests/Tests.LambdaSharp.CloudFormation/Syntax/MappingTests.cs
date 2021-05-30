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

using LambdaSharp.CloudFormation.Syntax;
using LambdaSharp.CloudFormation.Syntax.Declarations;
using LambdaSharp.CloudFormation.Syntax.Expressions;
using LambdaSharp.CloudFormation.Syntax.Validators;
using Xunit;
using Xunit.Abstractions;

namespace Tests.LambdaSharp.CloudFormation.Syntax {

    public class MappingTests {

        //--- Constructors ---
        public MappingTests(ITestOutputHelper output) => Output = output;

        //--- Properties ---
        protected ITestOutputHelper Output { get; }

        //--- Methods ---

        [Fact]
        public void Mapping_level_2_key_is_missing() {

            // arrange
            var report = new Report(Output);
            var template = new CloudFormationSyntaxTemplate {
                Mappings = new CloudFormationSyntaxList<CloudFormationSyntaxMapping> {
                    new CloudFormationSyntaxMapping(new CloudFormationSyntaxLiteral("MyMapping")) {
                        Value = new CloudFormationSyntaxMap {
                            [new CloudFormationSyntaxLiteral("Level1Value1")] = new CloudFormationSyntaxMap {
                                [new CloudFormationSyntaxLiteral("Level2Value1")] = new CloudFormationSyntaxLiteral("1-1"),
                                [new CloudFormationSyntaxLiteral("Level2Value2")] = new CloudFormationSyntaxLiteral("1-2")
                            },
                            [new CloudFormationSyntaxLiteral("Level1Value2")] = new CloudFormationSyntaxMap {
                                [new CloudFormationSyntaxLiteral("Level2Value1")] = new CloudFormationSyntaxLiteral("2-1")
                            },
                            [new CloudFormationSyntaxLiteral("Level1Value3")] = new CloudFormationSyntaxMap {
                                [new CloudFormationSyntaxLiteral("Level2Value2")] = new CloudFormationSyntaxLiteral("3-2")
                            }
                        }
                    }
                },
                Resources = new CloudFormationSyntaxList<CloudFormationSyntaxResource> {
                    new CloudFormationSyntaxResource(new CloudFormationSyntaxLiteral("MyResource")) {
                        Type = new CloudFormationSyntaxLiteral("AWS::SNS::Topic")
                    }
                }
            };

            // act
            new CloudFormationSyntaxTemplateValidator(report).Validate(template);

            // assert
            Assert.Collection(
                report.Entries,
                entry => Assert.Equal("level 2 key 'Level2Value2' is missing", entry.Message),
                entry => Assert.Equal("level 2 key 'Level2Value1' is missing", entry.Message)
            );
        }
    }
}
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

    public class TemplateTests {

        //--- Constructors ---
        public TemplateTests(ITestOutputHelper output) => Output = output;

        //--- Properties ---
        protected ITestOutputHelper Output { get; }

        //--- Methods ---

        [Fact]
        public void Template_resources_missing() {

            // arrange
            var report = new Report(Output);
            var template = new CloudFormationSyntaxTemplate();

            // act
            new CloudFormationSyntaxTemplateValidator(report).Validate(template);

            // assert
            Assert.Collection(
                report.Entries,
                entry => Assert.Equal("template is missing the resources section", entry.Message)
            );
        }

        [Fact]
        public void Template_version_is_wrong() {

            // arrange
            var report = new Report(Output);
            var template = new CloudFormationSyntaxTemplate {
                AWSTemplateFormatVersion = new CloudFormationSyntaxLiteral("2010-09-119"),
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
                entry => Assert.Equal("template version is not valid (expected: 2010-09-09)", entry.Message)
            );
        }

        [Fact]
        public void Minimal_valid_template() {

            // arrange
            var report = new Report(Output);
            var template = new CloudFormationSyntaxTemplate {
                Resources = new CloudFormationSyntaxList<CloudFormationSyntaxResource> {
                    new CloudFormationSyntaxResource(new CloudFormationSyntaxLiteral("MyResource")) {
                        Type = new CloudFormationSyntaxLiteral("AWS::SNS::Topic")
                    }
                }
            };

            // act
            new CloudFormationSyntaxTemplateValidator(report).Validate(template);

            // assert
            Assert.Empty(report.Entries);
        }
    }
}
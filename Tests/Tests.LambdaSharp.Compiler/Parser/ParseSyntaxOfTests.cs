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

using FluentAssertions;
using LambdaSharp.Compiler.Syntax;
using LambdaSharp.Compiler.Syntax.Expressions;
using Xunit;
using Xunit.Abstractions;

namespace Tests.LambdaSharp.Compiler.Parser {

    // TODO: add CloudWatch event source
    // TODO: add tests to recover from badly formed YAML

    public class ParseSyntaxOfTests : _Init {

        //--- Constructors ---
        public ParseSyntaxOfTests(ITestOutputHelper output) : base(output) { }

        //--- Methods ---

        [Fact]
        public void ParseAllFields() {

            // arrange
            var parser = NewParser(
@"Module: My.Module
Version: 1.2.3.4-DEV
Description: description
CloudFormation:
  Version: 1.2.3
  Region: us-east-1
Pragmas:
    - no-module-registration
    - Overrides:
        Module::WebSocket.RouteSelectionExpression: $request.body.Action
Secrets:
    - key-alias
    - arn:aws:kms:eu-west-1:xxxxxxxx:key/xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
Using:
    - Module: My.OtherModule
      Description: Importing some features
Items:
    - Parameter: MyParameter
      Description: MyDescription
      Section: MySection
      Label: MyLabel
      Type: String
      Scope: MyLambda
      NoEcho: true
      Default: ~
      ConstraintDescription: It needs to fit the constraint
      AllowedPattern: .*
      AllowedValues:
        - one
        - two
        - three
      MaxLength: 123
      MaxValue: 500
      MinLength: 1
      MinValue: 0
      Allow: ReadWrite
      Properties:
        FirstProperty: Scalar
        SecondProperty: [ One, Two, Three ]
        ThirdProperty:
            Key1: One
            Key2: Two
            Key4: Three
      EncryptionContext:
        Foo: Bar
      Pragmas:
        - no-type-validation

    - Import: MyImport
      Description: description
      Type: String
      Scope:
        - MyFirstLambda
        - MySecondLambda
      Allow:
        - Read
        - Write
      Module: My.OtherModule::MessageTitle
      EncryptionContext:
        Foo: Bar

    - Variable: MyVariable
      Description: description
      Type: String
      Scope: MyFirstLambda, MySecondLambda
      Value: 123456789
      EncryptionContext:
        Foo: Bar

    - Group: MyGroup
      Description: description
      Items:

        - Variable: MyNestedVariable
          Type: String
          Scope: MyLambda
          Value: 987654321

    - Condition: MyCondition
      Description: description
      Value: !Equals [ !Ref Abc, ""Foo"" ]

    - Resource: MyResource
      Description: description
      Type: AWS::SNS::Topic
      Scope: MyLambda
      Allow: ReadWrite
      Value: arn::s3:::my-bucket
      DependsOn: MyOtherResource
      Properties:
        Property: value
      DefaultAttribute: default
      Pragmas:
        - no-type-validation

    - Nested: MyNestedModule
      Description: description
      Module: Acme.MyOtherModule:1.0@my-bucket
      DependsOn:
        - MyResource
        - MyOtherResource
      Parameters:
        FirstParameter: Scalar
        SecondParameter: [ One, Two, Three ]
        ThirdParameter:
            Key1: One
            Key2: Two
            Key4: Three

    - Package: MyPackage
      Description: description
      Scope: MyLambda
      Files: webroot/*

    - Function: MyFunction
      Description: description
      Scope: MyLambda
      If: MyCondition
      Memory: 128
      Timeout: 60
      Project: folder/my-function.csproj
      Runtime: dotnetcore2.1
      Language: csharp
      Handler: MyModule.MyNameSpace.MyFunction::EntryPoint
      Vpc:
        SecurityGroupIds: !Split [ "","", !Ref SecurityGroupIds ]
        SubnetIds: !Split [ "","", !Ref SubnetIds ]
      Environment:
        FirstEnvironmentVariable: value
      Properties:
        Property: value
      Pragmas:
        - no-assembly-validation
      Sources:
        - Api: POST:/path
          Integration: AWS
          OperationName: PostToPath
          ApiKeyRequired: false
          AuthorizationType: NONE
          AuthorizationScopes:
            - Group1
            - Group2
          AuthorizerId: !Ref MyAuthorizer
          Invoke: MyApiMethod

        - Schedule: rate(2 hrs)
          Name: MyScheduleEvent

        - S3: !Ref MyBucket
          Events:
            - ""s3:ObjectCreated:*""
            - ""s3:ObjectRemoved:*""
          Prefix: images/
          Suffix: .png

        - SlackCommand: /api/path/to/command

        - Topic: !Ref MyTopic
          Filters:
            source:
                - shopping-cart

        - Sqs: !Ref MyQueue
          BatchSize: 10

        - Alexa: !Ref AlexaSkill

        - DynamoDB: !Ref MyTable
          BatchSize: 100
          StartingPosition: LATEST
          MaximumBatchingWindowInSeconds: 5

        - Kinesis: !Ref MyKinesisStream
          BatchSize: 100
          StartingPosition: LATEST
          MaximumBatchingWindowInSeconds: 5

        - WebSocket: $connect
          OperationName: Connect
          ApiKeyRequired: true
          AuthorizationType: NONE
          AuthorizationScopes: Group1, Group2
          AuthorizerId: !Ref MyAuthorizer
          Invoke: MyWebSocketMethod
");

            // act
            var module = parser.ParseModule();

            // assert
            ExpectNoMessages();
            module.Should().NotBeNull();
            module!.Visit(new SyntaxHierarchyValidationAnalyzer());
        }


        [Fact]
        public void ParseDecryptSecretFunction() {

            // arrange
            var parser = NewParser("LambdaSharp.Compiler.dll", "DecryptSecretFunction.js");

            // act
            var expression = parser.ParseExpression();

            // assert
            ExpectNoMessages();
            expression.Should().NotBeNull();
            expression.Should().BeOfType<LiteralExpression>()
              .Which.Value.Should().StartWith("const AWS = require('aws-sdk');");
        }

        [Fact]
        public void ParseStandardModule() {

            // arrange
            var parser = NewParser("LambdaSharp.Compiler.dll", "Standard-Module.yml");

            // act
            var module = parser.ParseModule();

            // assert
            ExpectNoMessages();
            module.Should().NotBeNull();
        }

        [Fact]
        public void ParseLambdaSharpModule() {

            // arrange
            var parser = NewParser("LambdaSharp.Compiler.dll", "LambdaSharp-Module.yml");

            // act
            var module = parser.ParseModule();

            // assert
            ExpectNoMessages();
            module.Should().NotBeNull();
        }
    }
}
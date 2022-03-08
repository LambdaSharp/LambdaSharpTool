/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2022
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
using System.Linq.Expressions;
using FluentAssertions;
using LambdaSharp.DynamoDB.Native;
using LambdaSharp.DynamoDB.Native.Internal;
using Test.LambdaSharp.DynamoDB.Internal.DynamoRequestConverterTests.Model;
using Xunit;
using Xunit.Abstractions;

namespace Test.LambdaSharp.DynamoDB.Internal.DynamoRequestConverterTests {

    public class TryParseIfNotExistsSetFunction {

        //--- Class Methods ---
        private static Expression LambdaBody<T>(Expression<Func<MyRecord, T>> expression)
            => expression.Body;

        //--- Constructors ---
        public TryParseIfNotExistsSetFunction(ITestOutputHelper output) => Output = output;

        //--- Properties ---
        private ITestOutputHelper Output { get; }

        //--- Methods ---

        [Fact]
        public void Test() {

            // arrange
            var converter = new DynamoRequestConverter(new(), new(), new());

            // act
            var expression = LambdaBody(record => DynamoUpdate.IfNotExists(record.List, new List<string>()));
            var success = converter.TryParseIfNotExistsSetFunction(expression, out var output);

            // assert
            success.Should().BeTrue();
            output.Should().Be($"if_not_exists(#a_1, :v_1)");
            converter.ExpressionAttributes.ContainsKey("#a_1").Should().BeTrue();
            converter.ExpressionAttributes["#a_1"].Should().Be("List");
            converter.ExpressionValues.ContainsKey(":v_1").Should().BeTrue();
            converter.ExpressionValues[":v_1"].IsLSet.Should().BeTrue();
            converter.ExpressionValues[":v_1"].L.Count.Should().Be(0);
        }
    }
}

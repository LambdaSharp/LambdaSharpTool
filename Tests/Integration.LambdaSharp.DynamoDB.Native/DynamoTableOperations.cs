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

using Xunit;
using Xunit.Abstractions;
using System.Threading.Tasks;
using Sample.DynamoDBNative.DataAccess.Models;
using Sample.DynamoDBNative.DataAccess;
using FluentAssertions;

namespace Integration.LambdaSharp.DynamoDB.Native {

    [Collection("DynamoDB")]
    public class DynamoTableOperations : _Init {

        // TODO: add `BatchWriteItems()`

        //--- Constructors ---
        public DynamoTableOperations(DynamoDbFixture dynamoDbFixture, ITestOutputHelper output) : base(dynamoDbFixture, output) {
            DataAccessClient = new ThriftBooksDataAccessClient(dynamoDbFixture.TableName, dynamoDbFixture.DynamoClient);
        }

        //--- Properties ---
        private IThriftBooksDataAccess DataAccessClient { get; }

        //--- Methods ---

        [Fact]
        public async Task TransactGetItems() {

            // arrange
            var customer1 = NewCustomerRecord();
            await DataAccessClient.CreateCustomerAsync(customer1);
            var customer2 = NewCustomerRecord();
            await DataAccessClient.CreateCustomerAsync(customer2);

            // act
            var result = await base.Table.TransactGetItems(new[] {
                new CustomerRecord.PrimaryKey(customer1),
                new CustomerRecord.PrimaryKey(customer2) }
            ).TryExecuteAsync();

            // assert
            result.Success.Should().BeTrue();
            result.Items.Should().HaveCount(2);
            result.Items.Should().ContainEquivalentOf(customer1);
            result.Items.Should().ContainEquivalentOf(customer2);
        }

        [Fact]
        public async Task BatchGetItems() {

            // arrange
            var customer1 = NewCustomerRecord();
            await DataAccessClient.CreateCustomerAsync(customer1);
            var customer2 = NewCustomerRecord();
            await DataAccessClient.CreateCustomerAsync(customer2);

            // act
            var result = await base.Table.BatchGetItems(new[] {
                new CustomerRecord.PrimaryKey(customer1),
                new CustomerRecord.PrimaryKey(customer2) }
            ).ExecuteAsync();

            // assert
            result.Should().HaveCount(2);
            result.Should().ContainEquivalentOf(customer1);
            result.Should().ContainEquivalentOf(customer2);
        }
    }
}

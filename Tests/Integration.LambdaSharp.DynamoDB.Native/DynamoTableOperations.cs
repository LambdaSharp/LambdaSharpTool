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
using System.Linq;
using LambdaSharp.DynamoDB.Native;

namespace Integration.LambdaSharp.DynamoDB.Native {

    [Collection("DynamoDB")]
    public class DynamoTableOperations : _Init {

        // TODO: add `BatchWriteItems()`
        // TODO: add `TransactGetItemsMixed()`

        //--- Types ---
        private class MyRecord {

            //--- Types ---
            public class PrimaryKey : DynamoPrimaryKey<MyRecord> {

                //--- Constants ---
                public const string PK_PATTERN = "MY-RECORD-ID#{0}";
                public const string SK_PATTERN = "SUB-ID#{1}";

                //--- Constructors ---
                public PrimaryKey(MyRecord record) : this(record.Id, record.SubId) { }
                public PrimaryKey(string id, string subId) : base(PK_PATTERN, SK_PATTERN, id, subId) { }

            }

            //--- Properties ---
            public string Id { get; set; }
            public string SubId { get; set; }
            public string Value { get; set; }
        }

        private class MyOtherRecord {

            //--- Types ---
            public class PrimaryKey : DynamoPrimaryKey<MyOtherRecord> {

                //--- Constants ---
                public const string PK_PATTERN = "MY-RECORD-ID#{0}";
                public const string SK_PATTERN = "OTHER-SUB-ID#{1}";

                //--- Constructors ---
                public PrimaryKey(MyOtherRecord record) : this(record.Id, record.SubId) { }
                public PrimaryKey(string id, string subId) : base(PK_PATTERN, SK_PATTERN, id, subId) { }

            }

            //--- Properties ---
            public string Id { get; set; }
            public string SubId { get; set; }
            public string Name { get; set; }
        }

        //--- Constructors ---
        public DynamoTableOperations(DynamoDbFixture dynamoDbFixture, ITestOutputHelper output) : base(dynamoDbFixture, output) {
            DataAccessClient = new ThriftBooksDataAccessClient(dynamoDbFixture.TableName, dynamoDbFixture.DynamoClient);
            Table = new DynamoTable(TableName, DynamoClient, ThriftBooksDataAccessClient.DynamoOptions);
        }

        //--- Properties ---
        private IThriftBooksDataAccess DataAccessClient { get; }
        private IDynamoTable Table { get; }

        //--- Methods ---

        [Fact]
        public async Task GetItem_when_it_does_not_exist() {

            // arrange

            // act
            var result = await Table.GetItemAsync(new CustomerRecord.PrimaryKey("123456789"));

            // assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task PutItem_with_condition_success() {

            // arrange
            var customer = NewCustomer();
            await DataAccessClient.CreateCustomerAsync(customer);

            // act
            var result = await Table.PutItem(customer, new CustomerRecord.PrimaryKey(customer))
                .WithCondition(record => DynamoCondition.Exists(record))
                .ExecuteAsync();

            // assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task PutItem_with_condition_failed() {

            // arrange
            var customer = NewCustomer();
            await DataAccessClient.CreateCustomerAsync(customer);

            // act
            var result = await Table.PutItem(customer, new CustomerRecord.PrimaryKey(customer))
                .WithCondition(record => DynamoCondition.DoesNotExist(record))
                .ExecuteAsync();

            // assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateItem_with_condition_success() {

            // arrange
            var customer = NewCustomer();
            var (order, items) = NewOrder(customer.Username);
            await DataAccessClient.CreateCustomerAsync(customer);
            await DataAccessClient.SaveOrderAsync(order, items);

            // act
            var result = await Table.UpdateItem(new OrderRecord.PrimaryKey(order))
                .WithCondition(record => record.Status == OrderStatus.Pending)
                .Set(record => record.Status, OrderStatus.Shipped)
                .ExecuteAsync();

            // assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteItem_with_condition_success() {

            // arrange
            var customer = NewCustomer();
            await DataAccessClient.CreateCustomerAsync(customer);

            // act
            var result = await Table.DeleteItem(new CustomerRecord.PrimaryKey(customer))
                .WithCondition(record => record.Name == customer.Name)
                .ExecuteAsync();

            // assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteItem_with_condition_failed() {

            // arrange
            var customer = NewCustomer();
            await DataAccessClient.CreateCustomerAsync(customer);

            // act
            var result = await Table.DeleteItem(new CustomerRecord.PrimaryKey(customer))
                .WithCondition(record => record.Name == "Bob")
                .ExecuteAsync();

            // assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateItem_with_condition_failed() {

            // arrange
            var customer = NewCustomer();
            var (order, items) = NewOrder(customer.Username);
            await DataAccessClient.CreateCustomerAsync(customer);
            await DataAccessClient.SaveOrderAsync(order, items);

            // act
            var result = await Table.UpdateItem(new OrderRecord.PrimaryKey(order))
                .WithCondition(record => record.Status == OrderStatus.Shipped)
                .Set(record => record.Status, OrderStatus.Delivered)
                .ExecuteAsync();

            // assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task Query_main_index() {

            // arrange
            var id = GetRandomString(10);
            var record1 = new MyRecord {
                Id = id,
                SubId = GetRandomString(10),
                Value = "Hello"
            };
            await Table.PutItemAsync(record1, new MyRecord.PrimaryKey(record1));
            var record2 = new MyRecord {
                Id = id,
                SubId = GetRandomString(10),
                Value = "World"
            };
            await Table.PutItemAsync(record2, new MyRecord.PrimaryKey(record2));

            // act
            var result = await Table.Query(new MyRecord.PrimaryKey(record1), consistentRead: true)
                .WhereSKBeginsWith(string.Format(MyRecord.PrimaryKey.SK_PATTERN, "", ""))
                .ExecuteAsync();

            // assert
            result.Should().HaveCount(2);
            result.Should().ContainEquivalentOf(record1);
            result.Should().ContainEquivalentOf(record2);
        }

        [Fact]
        public async Task QueryMixed_main_index() {

            // arrange
            var id = GetRandomString(10);
            var record1 = new MyRecord {
                Id = id,
                SubId = GetRandomString(10),
                Value = "Hello"
            };
            await Table.PutItemAsync(record1, new MyRecord.PrimaryKey(record1));
            var record2 = new MyOtherRecord {
                Id = id,
                SubId = GetRandomString(10),
                Name = "Bob"
            };
            await Table.PutItemAsync(record2, new MyOtherRecord.PrimaryKey(record2));

            // act
            var result = await Table.QueryMixed(new MyRecord.PrimaryKey(record1), consistentRead: true)
                .WhereSKMatchesAny()
                .WithTypeFilter<MyRecord>()
                .WithTypeFilter<MyOtherRecord>()
                .ExecuteAsync();

            // assert
            result.Should().HaveCount(2);
            result.Should().ContainEquivalentOf(record1);
            result.Should().ContainEquivalentOf(record2);
        }

        [Fact]
        public async Task TransactGetItems() {

            // arrange
            var customer1 = NewCustomer();
            await DataAccessClient.CreateCustomerAsync(customer1);
            var customer2 = NewCustomer();
            await DataAccessClient.CreateCustomerAsync(customer2);

            // act
            var result = await Table.TransactGetItems(new[] {
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
            var customer1 = NewCustomer();
            await DataAccessClient.CreateCustomerAsync(customer1);
            var customer2 = NewCustomer();
            await DataAccessClient.CreateCustomerAsync(customer2);

            // act
            var result = await Table.BatchGetItems(new[] {
                new CustomerRecord.PrimaryKey(customer1),
                new CustomerRecord.PrimaryKey(customer2) }
            ).ExecuteAsync();

            // assert
            result.Should().HaveCount(2);
            result.Should().ContainEquivalentOf(customer1);
            result.Should().ContainEquivalentOf(customer2);
        }

        [Fact]
        public async Task BatchGetItemsMixed() {

            // arrange
            var customer = NewCustomer();
            await DataAccessClient.CreateCustomerAsync(customer);
            var (order, items) = NewOrder(customer.Username);
            await DataAccessClient.SaveOrderAsync(order, items);

            // act
            var result = await Table.BatchGetItemsMixed()
                .GetItem(new CustomerRecord.PrimaryKey(customer))
                .GetItem(new OrderRecord.PrimaryKey(order))
                .ExecuteAsync();

            // assert
            result.Should().HaveCount(2);
            result.Should().ContainEquivalentOf(customer);
            result.Should().ContainEquivalentOf(order);
        }

        [Fact]
        public async Task BatchGetItemsMixedPartial() {

            // arrange
            var customer = NewCustomer();
            await DataAccessClient.CreateCustomerAsync(customer);
            var (order, items) = NewOrder(customer.Username);
            await DataAccessClient.SaveOrderAsync(order, items);

            // act
            var result = await Table.BatchGetItemsMixed()
                .StartGetItem(new CustomerRecord.PrimaryKey(customer))
                    .Get(record => record.Username)
                .End()
                .StartGetItem(new OrderRecord.PrimaryKey(order))
                    .Get(record => record.OrderId)
                .End()
                .ExecuteAsync();

            // assert
            result.Should().HaveCount(2);

            // verify fetched customer record
            var customerRecords = result.OfType<CustomerRecord>().ToList();
            customerRecords.Should().HaveCount(1);
            var fetchedCustomer =  customerRecords.First();
            fetchedCustomer.Should().NotBeNull();
            fetchedCustomer.Username.Should().Be(customer.Username);
            fetchedCustomer.Name.Should().BeNull();

            // verify fetched customer record
            var orderRecords = result.OfType<OrderRecord>().ToList();
            orderRecords.Should().HaveCount(1);
            var fetchedOrder =  orderRecords.First();
            fetchedOrder.Should().NotBeNull();
            fetchedOrder.OrderId.Should().Be(order.OrderId);
            fetchedOrder.CustomerUsername.Should().BeNull();
        }
    }
}

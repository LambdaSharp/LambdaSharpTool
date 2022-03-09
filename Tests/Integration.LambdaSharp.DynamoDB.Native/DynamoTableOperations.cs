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

        // TODO (2021-07-14, bjorg): add tests for these DynamoDB operations
        // * BatchWriteItems()
        // * TransactGetItemsMixed()

        //--- Types ---
        private class MyRecord {

            //--- Properties ---
            public string Id { get; set; }
            public string SubId { get; set; }
            public string Value { get; set; }
        }

        private class MyOtherRecord {

            //--- Properties ---
            public string Id { get; set; }
            public string SubId { get; set; }
            public string Name { get; set; }
        }

        private static class MyDataModel {

            //--- Constants ---
            public const string MY_RECORD_PK_PATTERN = "MY-RECORD-ID={0}";
            public const string MY_RECORD_SK_PATTERN = "SUB-ID={1}";
            public const string MY_OTHER_RECORD_PK_PATTERN = "MY-RECORD-ID={0}";
            public const string MY_OTHER_RECORD_SK_PATTERN = "OTHER-SUB-ID={1}";

            //--- Class Methods ---
            public static DynamoPrimaryKey<MyRecord> GetPrimaryKey(MyRecord record) => MyRecordPrimaryKey(record.Id, record.SubId);
            public static DynamoPrimaryKey<MyRecord> MyRecordPrimaryKey(string id, string subId) => new DynamoPrimaryKey<MyRecord>(MY_RECORD_PK_PATTERN, MY_RECORD_SK_PATTERN, id, subId);
            public static DynamoPrimaryKey<MyOtherRecord> GetPrimaryKey(MyOtherRecord record) => MyOtherRecordPrimaryKey(record.Id, record.SubId);
            public static DynamoPrimaryKey<MyOtherRecord> MyOtherRecordPrimaryKey(string id, string subId) => new DynamoPrimaryKey<MyOtherRecord>(MY_OTHER_RECORD_PK_PATTERN, MY_OTHER_RECORD_SK_PATTERN, id, subId);
            public static IDynamoQueryClause<MyRecord> SelectMyRecords(string recordId)
                => DynamoQuery.SelectPKFormat<MyRecord>(MY_RECORD_PK_PATTERN, recordId)
                    .WhereSKBeginsWith(string.Format(MY_RECORD_SK_PATTERN, "", ""));
            public static IDynamoQueryClause SelectMyRecordsAndMyOtherRecords(string recordId)
                => DynamoQuery.SelectPKFormat(MY_RECORD_PK_PATTERN, recordId)
                    .WhereSKMatchesAny()
                    .WithTypeFilter<MyRecord>()
                    .WithTypeFilter<MyOtherRecord>();
        }

        //--- Constructors ---
        public DynamoTableOperations(DynamoDbFixture dynamoDbFixture, ITestOutputHelper output) : base(dynamoDbFixture, output) {
            DataAccessClient = new ThriftBooksDataAccessClient(dynamoDbFixture.TableName, dynamoDbFixture.DynamoClient);
            Table = new DynamoTable(TableName, DynamoClient, ThriftBooksDataAccessClient.TableOptions);
        }

        //--- Properties ---
        private IThriftBooksDataAccess DataAccessClient { get; }
        private IDynamoTable Table { get; }

        //--- Methods ---

        [Fact]
        public async Task GetItemAsync_when_it_does_not_exist() {

            // arrange

            // act
            var result = await Table.GetItemAsync(DataModel.CustomerRecordPrimaryKey("123456789"));

            // assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetItem_when_it_does_not_exist() {

            // arrange

            // act
            var result = await Table.GetItem(DataModel.CustomerRecordPrimaryKey("123456789"))
                .ExecuteAsync();

            // assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task PutItem_with_condition_success() {

            // arrange
            var customer = NewCustomer();
            await DataAccessClient.CreateCustomerAsync(customer);

            // act
            var result = await Table.PutItem(customer.GetPrimaryKey(), customer)
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
            var result = await Table.PutItem(customer.GetPrimaryKey(), customer)
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
            var result = await Table.UpdateItem(order.GetPrimaryKey())
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
            var result = await Table.DeleteItem(customer.GetPrimaryKey())
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
            var result = await Table.DeleteItem(customer.GetPrimaryKey())
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
            var result = await Table.UpdateItem(order.GetPrimaryKey())
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
            await Table.PutItemAsync(MyDataModel.GetPrimaryKey(record1), record1);
            var record2 = new MyRecord {
                Id = id,
                SubId = GetRandomString(10),
                Value = "World"
            };
            await Table.PutItemAsync(MyDataModel.GetPrimaryKey(record2), record2);

            // act
            var result = await Table.Query(MyDataModel.SelectMyRecords(record1.Id), consistentRead: true).ExecuteAsync();

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
            await Table.PutItemAsync(MyDataModel.GetPrimaryKey(record1), record1);
            var record2 = new MyOtherRecord {
                Id = id,
                SubId = GetRandomString(10),
                Name = "Bob"
            };
            await Table.PutItemAsync(MyDataModel.GetPrimaryKey(record2), record2);

            // act
            var result = await Table.Query(MyDataModel.SelectMyRecordsAndMyOtherRecords(record1.Id), consistentRead: true)
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
                customer1.GetPrimaryKey(),
                customer2.GetPrimaryKey()
            }).TryExecuteAsync();

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
                customer1.GetPrimaryKey(),
                customer2.GetPrimaryKey()
            }).ExecuteAsync();

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
            var result = await Table.BatchGetItems()
                .GetItem(customer.GetPrimaryKey())
                .GetItem(order.GetPrimaryKey())
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
            var result = await Table.BatchGetItems()
                .BeginGetItem(customer.GetPrimaryKey())
                    .Get(record => record.Username)
                .End()
                .BeginGetItem(order.GetPrimaryKey())
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

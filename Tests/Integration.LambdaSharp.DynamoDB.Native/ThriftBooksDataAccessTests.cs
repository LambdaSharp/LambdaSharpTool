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
using System.Collections.Generic;
using System;
using System.Linq;
using FluentAssertions;
using LambdaSharp.DynamoDB.Native;
using Sample.DynamoDBNative.DataAccess;
using Sample.DynamoDBNative.DataAccess.Models;

namespace Integration.LambdaSharp.DynamoDB.Native {

    [Collection("DynamoDB")]
    public class ThriftBooksDataAccessTests : _Init {

        //--- Class Methods ---
        public static CustomerRecord NewCustomerRecord() {
            var username = "user_" + GetRandomString(10);
            return new CustomerRecord {
                Username = username,
                Name = "John Doe",
                EmailAddress = $"{username}@example.org",
                Addresses = new()
            };
        }

        //--- Constructors ---
        public ThriftBooksDataAccessTests(DynamoDbFixture dynamoDbFixture, ITestOutputHelper output) : base(dynamoDbFixture, output) {
            DataAccessClient = new ThriftBooksDataAccessClient(dynamoDbFixture.TableName, dynamoDbFixture.DynamoClient);
        }

        //--- Properties ---
        private IThriftBooksDataAccess DataAccessClient { get; }

        //--- Methods ---

        [Fact]
        public async Task Create_customer_record() {

            // arrange
            var customer = NewCustomerRecord();

            // act
            await DataAccessClient.CreateCustomerAsync(customer);
            var result = await Table.GetItemAsync(new CustomerRecord.PrimaryKey(customer), consistentRead: true);

            // assert
            result.Should().BeEquivalentTo(customer);
        }

        [Fact]
        public async Task Update_address() {

            // arrange
            var customer = NewCustomerRecord();

            // act
            await DataAccessClient.CreateCustomerAsync(customer);
            var address = new AddressRecord {
                Label = "Work",
                Street = "101 W. Broadway",
                City = "San Diego",
                State = "CA"
            };
            await DataAccessClient.AddOrUpdateAddressAsync(customer.Username, address);
            var result = await Table.GetItemAsync(new CustomerRecord.PrimaryKey(customer), consistentRead: true);

            // assert
            result.Username.Should().Be(customer.Username);
            result.Name.Should().Be(customer.Name);
            result.EmailAddress.Should().Be(customer.EmailAddress);
            result.Addresses.Should().NotBeEmpty();
            result.Addresses.Should().ContainKey("Work");
            result.Addresses["Work"].Should().BeEquivalentTo(address);
        }

        [Fact]
        public async Task Add_order() {

            // arrange
            var customer = NewCustomerRecord();

            // act
            await DataAccessClient.CreateCustomerAsync(customer);
            var orderId = "order_" + GetRandomString(10);
            var orderItems = new List<OrderItemRecord> {
                new() {
                    OrderId = orderId,
                    Description = "Toothbrush",
                    ItemId = "123",
                    Price = 1.23m,
                    Quantity = 1
                }
            };
            var order = new OrderRecord {
                Amount = 100,
                CreateAt = DateTimeOffset.FromUnixTimeSeconds(1624671954),
                CustomerUsername = customer.Username,
                NumberOfItems = orderItems.Count,
                OrderId = orderId,
                Status = OrderStatus.Pending
            };
            await DataAccessClient.SaveOrderAsync(order, orderItems);
            var result = await DataAccessClient.GetCustomerWithMostRecentOrdersAsync(customer.Username, limit: 10);

            // assert
            result.Customer.Username.Should().Be(customer.Username);
            result.Customer.Name.Should().Be(customer.Name);
            result.Customer.EmailAddress.Should().Be(customer.EmailAddress);
            result.Orders.Count().Should().Be(1);
            result.Orders.First().Should().BeEquivalentTo(order);
        }
    }
}

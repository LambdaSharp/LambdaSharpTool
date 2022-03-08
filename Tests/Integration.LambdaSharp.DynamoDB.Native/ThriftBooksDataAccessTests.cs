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

using System.Threading.Tasks;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using FluentAssertions;
using LambdaSharp.DynamoDB.Native;
using Sample.DynamoDBNative.DataAccess;
using Sample.DynamoDBNative.DataAccess.Models;

namespace Integration.LambdaSharp.DynamoDB.Native {


    [Collection("DynamoDB")]
    public class ThriftBooksDataAccessTests : _Init {

        //--- Constructors ---
        public ThriftBooksDataAccessTests(DynamoDbFixture dynamoDbFixture, ITestOutputHelper output) : base(dynamoDbFixture, output) {
            DataAccessClient = new ThriftBooksDataAccessClient(dynamoDbFixture.TableName, dynamoDbFixture.DynamoClient);
            Table = new DynamoTable(TableName, DynamoClient, ThriftBooksDataAccessClient.TableOptions);
        }

        //--- Properties ---
        private IThriftBooksDataAccess DataAccessClient { get; }
        private IDynamoTable Table { get; }

        //--- Methods ---

        [Fact]
        public async Task Create_customer_record() {

            // arrange
            var customer = NewCustomer();

            // act
            await DataAccessClient.CreateCustomerAsync(customer);
            var result = await Table.GetItemAsync(customer.GetPrimaryKey(), consistentRead: true);

            // assert
            result.Should().BeEquivalentTo(customer);
        }

        [Fact]
        public async Task Update_address() {

            // arrange
            var customer = NewCustomer();

            // act
            await DataAccessClient.CreateCustomerAsync(customer);
            var address = new AddressRecord {
                Label = "Work",
                Street = "101 W. Broadway",
                City = "San Diego",
                State = "CA"
            };
            await DataAccessClient.AddOrUpdateAddressAsync(customer.Username, address);
            var result = await Table.GetItemAsync(customer.GetPrimaryKey(), consistentRead: true);

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
            var customer = NewCustomer();
            var (order, items) = NewOrder(customer.Username);

            // act
            await DataAccessClient.CreateCustomerAsync(customer);
            await DataAccessClient.SaveOrderAsync(order, items);
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

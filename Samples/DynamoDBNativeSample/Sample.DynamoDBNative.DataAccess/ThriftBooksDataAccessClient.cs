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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using LambdaSharp.DynamoDB.Native;
using Sample.DynamoDBNative.DataAccess.Models;

namespace Sample.DynamoDBNative.DataAccess {

    public class ThriftBooksDataAccessClient : IThriftBooksDataAccess {

        //--- Class Methods ---
        private static async Task<T> GetSingleItem<T>(IAsyncEnumerable<T> asyncEnumerable) {
            var result = new List<T>();
            await foreach(var item in asyncEnumerable) {
                result.Add(item);
                break;
            }
            return result.Single();
        }

        //--- Constructors ---
        public ThriftBooksDataAccessClient(string tableName, IAmazonDynamoDB dynamoClient = null)
            => Table = new DynamoTable(tableName, dynamoClient);

        //--- Properties ---
        protected IDynamoTable Table { get; }

        //--- Methods ---
        public async Task CreateCustomerAsync(CustomerRecord customer, CancellationToken cancellationToken) {
            if(customer.Addresses is null) {

                // initialize an empty dictionary for addresses so we can add to it easily later
                customer.Addresses = new Dictionary<string, AddressRecord>();
            }

            // TODO: wrap in a transaction
            await Table.PutItem(customer, new CustomerRecord.PrimaryKey(customer))
                .WithCondition(record => DynamoCondition.DoesNotExist(record))
                .ExecuteAsync(cancellationToken);

            var customerEmail = new CustomerEmailRecord {
                Username = customer.Username,
                EmailAddress = customer.EmailAddress
            };
            await Table.PutItem(customerEmail, new CustomerEmailRecord.PrimaryKey(customerEmail))
                .WithCondition(record => DynamoCondition.DoesNotExist(record))
                .ExecuteAsync(cancellationToken);
        }

        public Task AddOrUpdateAddressAsync(string customerUsername, AddressRecord address, CancellationToken cancellationToken) {
            return Table.UpdateItem(new CustomerRecord.PrimaryKey(customerUsername))
                .Set(record => record.Addresses[address.Label], address)
                .ExecuteAsync(cancellationToken);
        }

        public async Task<(CustomerRecord Customer, IEnumerable<OrderRecord> Orders)> GetCustomerWithMostRecentOrdersAsync(string customerUsername, int limit, CancellationToken cancellationToken) {

            // query all records under the customer name, which include the order records as well
            var records = await Table.QueryMixed(new CustomerRecord.PrimaryKey(customerUsername), limit: 11, scanIndexForward: false)
                .WhereSKMatchesAny()
                .WithTypeFilter<CustomerRecord>()
                .WithTypeFilter<OrderRecord>()
                .ExecuteAsync(cancellationToken: cancellationToken);

            // split returned records by type
            return (Customer: records.OfType<CustomerRecord>().Single(), Orders: records.OfType<OrderRecord>().ToList());
        }

        public async Task SaveOrderAsync(OrderRecord order, IEnumerable<OrderItemRecord> orderItems, CancellationToken cancellationToken) {

            // store order
            var success = await Table.PutItem(order, new OrderRecord.PrimaryKey(order), new OrderRecord.GSI1Key(order))
                .WithCondition(record => DynamoCondition.DoesNotExist(record))
                .ExecuteAsync(cancellationToken);
            if(!success) {
                throw new Exception("unable to store order");
            }

            // store all order items using batch operations
            while(orderItems.Any()) {
                var batch = Table.BatchWriteItem();

                // BatchWriteItem can take up to 25 operations
                foreach(var orderItem in orderItems.Take(25)) {
                    batch.PutItem(orderItem, new OrderItemRecord.PrimaryKey(orderItem));
                }
                await batch.ExecuteAsync();

                // skip the oder items that we stored
                orderItems = orderItems.Skip(25);
            }
        }

        public async Task UpdateOrderAsync(string orderId, OrderStatus orderStatus, CancellationToken cancellationToken) {

            // resolve order ID to primary key by querying the global secondary index
            var gsi1Key = new OrderRecord.GSI1Key(orderId);
            var order = await GetSingleItem(
                Table.Query(gsi1Key)
                    .WhereSKEquals(gsi1Key.SortKeyValue)
                    .Get(record => record.CustomerUsername)
                    .ExecuteAsyncEnumerable(cancellationToken)
            );

            // update order with state
            await Table.UpdateItem(new OrderRecord.PrimaryKey(order.CustomerUsername, order.OrderId))
                .Set(record => record.Status, orderStatus)
                .ExecuteAsync(cancellationToken);
        }

        public async Task<(OrderRecord Order, IEnumerable<OrderItemRecord> Items)> GetOrderWithOrderItemsAsync(string orderId, CancellationToken cancellationToken) {

            // query all records under the order ID, which include the order item records as well
            var records = await Table.QueryMixed(new OrderRecord.GSI1Key(orderId))
                .WhereSKMatchesAny()
                .WithTypeFilter<OrderRecord>()
                .WithTypeFilter<OrderItemRecord>()
                .ExecuteAsync(cancellationToken);

            // split returned records by type
            return (Order: records.OfType<OrderRecord>().Single(), Items: records.OfType<OrderItemRecord>().ToList());
        }
    }
}

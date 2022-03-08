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

using Xunit.Abstractions;
using Amazon.DynamoDBv2;
using System.Text.Json;
using System;
using System.Linq;
using LambdaSharp.DynamoDB.Native.Utility;
using Sample.DynamoDBNative.DataAccess.Models;
using System.Collections.Generic;
using LambdaSharp.DynamoDB.Serialization.Utility;

namespace Integration.LambdaSharp.DynamoDB.Native {

    public abstract class _Init {

        //--- Constants ---
        private const string VALID_SYMBOLS = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

        //--- Class Fields ---
        protected static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters = {
                new DynamoAttributeValueConverter()
            }
        };

        private readonly static Random _random = new Random();

        //--- Class Methods ---
        protected static string GetRandomString(int length)
            => new string(Enumerable.Repeat(VALID_SYMBOLS, length).Select(chars => chars[_random.Next(chars.Length)]).ToArray());

        protected static CustomerRecord NewCustomer() {
            var username = "user_" + GetRandomString(10);
            return new CustomerRecord {
                Username = username,
                Name = "John Doe",
                EmailAddress = username + "@example.org",
                Addresses = new()
            };
        }

        protected static (OrderRecord, IEnumerable<OrderItemRecord>) NewOrder(string customerUsername) {
            var order = new OrderRecord {
                OrderId = GetRandomString(10),
                Status = OrderStatus.Pending,
                Amount = 9m,
                CreateAt = DateTimeOffset.FromUnixTimeMilliseconds(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()),
                CustomerUsername = customerUsername,
                NumberOfItems = 3
            };
            var orderItems = new List<OrderItemRecord>();
            for(var i = 0; i < order.NumberOfItems; ++i) {
                orderItems.Add(new OrderItemRecord {
                    Description = $"Order item {i + 1}",
                    ItemId = GetRandomString(10),
                    OrderId = order.OrderId,
                    Price = 3m,
                    Quantity = 1
                });
            }
            return (order, orderItems);
        }

        //--- Constructors ---
        protected _Init(DynamoDbFixture dynamoDbFixture, ITestOutputHelper output) {
            Output = output;
            DynamoClient = new InspectDynamoDbClient(new AmazonDynamoDBClient(), item => Output.WriteLine(JsonSerializer.Serialize(item, JsonOptions)));
            TableName = dynamoDbFixture.TableName;
        }

        //--- Properties ---
        protected ITestOutputHelper Output { get; }
        protected IAmazonDynamoDB DynamoClient { get; }
        protected string TableName { get; }
    }
}

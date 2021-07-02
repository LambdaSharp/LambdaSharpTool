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

using Xunit.Abstractions;
using Amazon.DynamoDBv2;
using System.Text.Json;
using System;
using System.Linq;
using LambdaSharp.DynamoDB.Serialization;
using LambdaSharp.DynamoDB.Native;
using LambdaSharp.DynamoDB.Native.Logger;

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

        //--- Constructors ---
        protected _Init(DynamoDbFixture dynamoDbFixture, ITestOutputHelper output) {
            Output = output;
            var dynamoClient = new LoggingDynamoDbClient(new AmazonDynamoDBClient(), item => Output.WriteLine(JsonSerializer.Serialize(item, JsonOptions)));
            Table = new DynamoTable(dynamoDbFixture.TableName, dynamoClient);
        }

        //--- Properties ---
        protected ITestOutputHelper Output { get; }
        protected IDynamoTable Table { get; }
    }
}

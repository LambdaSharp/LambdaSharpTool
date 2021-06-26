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
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using LambdaSharp.DynamoDB.Native.Internal;
using LambdaSharp.DynamoDB.Native.Operations;
using LambdaSharp.DynamoDB.Serialization;

namespace LambdaSharp.DynamoDB.Native {

    public class DynamoTable : IDynamoTable {

        //--- Fields ---
        private readonly IAmazonDynamoDB _dynamoClient;
        private readonly string _tableName;

        //--- Properties ---
        public IAmazonDynamoDB DynamoClient => _dynamoClient;

        //--- Constructors ---
        public DynamoTable(string tableName, IAmazonDynamoDB? dynamoClient = null, DynamoSerializerOptions? serializerOptions = null) {
            _tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
            _dynamoClient = dynamoClient ?? new AmazonDynamoDBClient();
            SerializerOptions = serializerOptions ?? new DynamoSerializerOptions();
        }

        //--- Properties ---
        public DynamoSerializerOptions SerializerOptions { get; set; }

        //--- Methods ---
        public IDynamoTableDeleteItem<TRecord> DeleteItem<TRecord>(DynamoPrimaryKey<TRecord> primaryKey)
            where TRecord : class
            => new DynamoTableDeleteItem<TRecord>(this, new DeleteItemRequest {
                Key = new Dictionary<string, AttributeValue> {
                    [primaryKey.PartitionKeyName] = new AttributeValue(primaryKey.PartitionKeyValue),
                    [primaryKey.SortKeyName] = new AttributeValue(primaryKey.SortKeyValue)
                },
                TableName = _tableName
            });

        public IDynamoTableGetItem<TRecord> GetItem<TRecord>(DynamoPrimaryKey<TRecord> primaryKey, bool consistenRead = false)
            where TRecord : class
            => new DynamoTableGetItem<TRecord>(this, new GetItemRequest {
                ConsistentRead = consistenRead,
                Key = new Dictionary<string, AttributeValue> {
                    [primaryKey.PartitionKeyName] = new AttributeValue(primaryKey.PartitionKeyValue),
                    [primaryKey.SortKeyName] = new AttributeValue(primaryKey.SortKeyValue)
                },
                TableName = _tableName
            });

        public IDynamoTablePutItem<TRecord> PutItem<TRecord>(TRecord record, DynamoPrimaryKey<TRecord> primaryKey, params ADynamoSecondaryKey[] secondaryKeys)
            where TRecord : class
            => new DynamoTablePutItem<TRecord>(this, new PutItemRequest {
                Item = SerializeItem(record, primaryKey, secondaryKeys),
                TableName = _tableName
            });

        public IDynamoTableUpdateItem<TRecord> UpdateItem<TRecord>(DynamoPrimaryKey<TRecord> primaryKey)
            where TRecord : class
            => new DynamoTableUpdateItem<TRecord>(this, new UpdateItemRequest {
                Key = new Dictionary<string, AttributeValue> {
                    [primaryKey.PartitionKeyName] = new AttributeValue(primaryKey.PartitionKeyValue),
                    [primaryKey.SortKeyName] = new AttributeValue(primaryKey.SortKeyValue)
                },
                TableName = _tableName
            });

        public IDynamoTableQuerySortKeyCondition<TRecord> Query<TRecord>(DynamoPrimaryKey<TRecord> partitionKey, int limit = int.MaxValue, bool scanIndexForward = true, bool consistenRead = false)
            where TRecord : class
            => new DynamoTableQuery<TRecord>(this, new QueryRequest {
                ConsistentRead = consistenRead,
                Limit = limit,
                ScanIndexForward = scanIndexForward,
                TableName = _tableName
            }, partitionKey.PartitionKeyName, partitionKey.SortKeyName, partitionKey.PartitionKeyValue);

        public IDynamoTableQuerySortKeyCondition<TRecord> Query<TRecord>(DynamoLocalIndexKey<TRecord> partitionKey, int limit, bool scanIndexForward, bool consistenRead)
            where TRecord : class
            => new DynamoTableQuery<TRecord>(this, new QueryRequest {
                ConsistentRead = consistenRead,
                IndexName = partitionKey.IndexName,
                Limit = limit,
                ScanIndexForward = scanIndexForward,
                TableName = _tableName
            }, partitionKey.PartitionKeyName, partitionKey.SortKeyName, partitionKey.PartitionKeyValue);

        public IDynamoTableQuerySortKeyCondition<TRecord> Query<TRecord>(DynamoGlobalIndexKey<TRecord> partitionKey, int limit, bool scanIndexForward, bool consistenRead)
            where TRecord : class
            => new DynamoTableQuery<TRecord>(this, new QueryRequest {
                ConsistentRead = consistenRead,
                IndexName = partitionKey.IndexName,
                Limit = limit,
                ScanIndexForward = scanIndexForward,
                TableName = _tableName
            }, partitionKey.PartitionKeyName, partitionKey.SortKeyName, partitionKey.PartitionKeyValue);

        public IDynamoTableQuerySortKeyCondition QueryUntyped(DynamoPrimaryKey partitionKey, int limit = int.MaxValue, bool scanIndexForward = true, bool consistenRead = false)
            => new DynamoTableQuery(this, new QueryRequest {
                ConsistentRead = consistenRead,
                Limit = limit,
                ScanIndexForward = scanIndexForward,
                TableName = _tableName
            }, partitionKey.PartitionKeyName, partitionKey.SortKeyName, partitionKey.PartitionKeyValue);

        public IDynamoTableQuerySortKeyCondition QueryUntyped(ADynamoSecondaryKey partitionKey, int limit, bool scanIndexForward, bool consistenRead)
            => new DynamoTableQuery(this, new QueryRequest {
                ConsistentRead = consistenRead,
                IndexName = partitionKey.IndexName,
                Limit = limit,
                ScanIndexForward = scanIndexForward,
                TableName = _tableName
            }, partitionKey.PartitionKeyName, partitionKey.SortKeyName, partitionKey.PartitionKeyValue);

        public TRecord? DeserializeItem<TRecord>(Dictionary<string, AttributeValue> attributes)
            where TRecord : class
            => DynamoSerializer.Deserialize<TRecord>(attributes, SerializerOptions);

        public object? DeserializeItem(Dictionary<string, AttributeValue> attributes, Type? targetType)
            => DynamoSerializer.Deserialize(attributes, targetType, SerializerOptions);

        public Dictionary<string, AttributeValue> SerializeItem<TRecord>(TRecord record, DynamoPrimaryKey<TRecord> primaryKey, ADynamoSecondaryKey[] secondaryKeys)
            where TRecord : class
        {
            var attributes = DynamoSerializer.Serialize(record, SerializerOptions).M;

            // add primary key details
            attributes[primaryKey.PartitionKeyName] = new AttributeValue(primaryKey.PartitionKeyValue);
            attributes[primaryKey.SortKeyName] = new AttributeValue(primaryKey.SortKeyValue);

            // add secondary key details
            foreach(var secondaryKey in secondaryKeys) {
                switch(secondaryKey) {
                case DynamoLocalIndexKey localIndexKey:

                    // primary key is the same and should not be set again
                    attributes[secondaryKey.SortKeyName] = new AttributeValue(secondaryKey.SortKeyValue);
                    break;
                case DynamoGlobalIndexKey globalIndexKey:
                    attributes[secondaryKey.PartitionKeyName] = new AttributeValue(secondaryKey.PartitionKeyValue);
                    attributes[secondaryKey.SortKeyName] = new AttributeValue(secondaryKey.SortKeyValue);
                    break;
                default:
                    throw new ArgumentException($"unrecognized key type: '{secondaryKey?.GetType().FullName ?? "<null>"}'");
                }
            }
            return attributes;
        }

    }
}

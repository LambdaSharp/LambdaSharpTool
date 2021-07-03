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

        //--- Properties ---
        public IAmazonDynamoDB DynamoClient => _dynamoClient;

        //--- Constructors ---
        public DynamoTable(string tableName, IAmazonDynamoDB? dynamoClient = null, DynamoSerializerOptions? serializerOptions = null) {
            TableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
            _dynamoClient = dynamoClient ?? new AmazonDynamoDBClient();
            SerializerOptions = serializerOptions ?? new DynamoSerializerOptions();
        }

        //--- Properties ---
        public DynamoSerializerOptions SerializerOptions { get; set; }
        public string TableName { get; }

        //--- Methods ---
        public IDynamoTableDeleteItem<TRecord> DeleteItem<TRecord>(DynamoPrimaryKey<TRecord> primaryKey)
            where TRecord : class
            => new DynamoTableDeleteItem<TRecord>(this, new DeleteItemRequest {
                Key = new Dictionary<string, AttributeValue> {
                    [primaryKey.PartitionKeyName] = new AttributeValue(primaryKey.PartitionKeyValue),
                    [primaryKey.SortKeyName] = new AttributeValue(primaryKey.SortKeyValue)
                },
                TableName = TableName
            });

        public IDynamoTableGetItem<TRecord> GetItem<TRecord>(DynamoPrimaryKey<TRecord> primaryKey, bool consistentRead = false)
            where TRecord : class
            => new DynamoTableGetItem<TRecord>(this, new GetItemRequest {
                ConsistentRead = consistentRead,
                Key = new Dictionary<string, AttributeValue> {
                    [primaryKey.PartitionKeyName] = new AttributeValue(primaryKey.PartitionKeyValue),
                    [primaryKey.SortKeyName] = new AttributeValue(primaryKey.SortKeyValue)
                },
                TableName = TableName
            });

        public IDynamoTablePutItem<TRecord> PutItem<TRecord>(TRecord record, DynamoPrimaryKey<TRecord> primaryKey, params ADynamoSecondaryKey[] secondaryKeys)
            where TRecord : class
            => new DynamoTablePutItem<TRecord>(this, new PutItemRequest {
                Item = SerializeItem(record, primaryKey, secondaryKeys),
                TableName = TableName
            });

        public IDynamoTableUpdateItem<TRecord> UpdateItem<TRecord>(DynamoPrimaryKey<TRecord> primaryKey)
            where TRecord : class
            => new DynamoTableUpdateItem<TRecord>(this, new UpdateItemRequest {
                Key = new Dictionary<string, AttributeValue> {
                    [primaryKey.PartitionKeyName] = new AttributeValue(primaryKey.PartitionKeyValue),
                    [primaryKey.SortKeyName] = new AttributeValue(primaryKey.SortKeyValue)
                },
                TableName = TableName
            });

        public IDynamoTableQuerySortKeyCondition<TRecord> Query<TRecord>(DynamoPrimaryKey<TRecord> partitionKey, int limit = int.MaxValue, bool scanIndexForward = true, bool consistentRead = false)
            where TRecord : class
            => new DynamoTableQuery<TRecord>(this, new QueryRequest {
                ConsistentRead = consistentRead,
                Limit = limit,
                ScanIndexForward = scanIndexForward,
                TableName = TableName
            }, partitionKey.PartitionKeyName, partitionKey.SortKeyName, partitionKey.PartitionKeyValue);

        public IDynamoTableQuerySortKeyCondition<TRecord> Query<TRecord>(DynamoLocalIndexKey<TRecord> partitionKey, int limit, bool scanIndexForward, bool consistentRead)
            where TRecord : class
            => new DynamoTableQuery<TRecord>(this, new QueryRequest {
                ConsistentRead = consistentRead,
                IndexName = partitionKey.IndexName,
                Limit = limit,
                ScanIndexForward = scanIndexForward,
                TableName = TableName
            }, partitionKey.PartitionKeyName, partitionKey.SortKeyName, partitionKey.PartitionKeyValue);

        public IDynamoTableQuerySortKeyCondition<TRecord> Query<TRecord>(DynamoGlobalIndexKey<TRecord> partitionKey, int limit, bool scanIndexForward, bool consistentRead)
            where TRecord : class
            => new DynamoTableQuery<TRecord>(this, new QueryRequest {
                ConsistentRead = consistentRead,
                IndexName = partitionKey.IndexName,
                Limit = limit,
                ScanIndexForward = scanIndexForward,
                TableName = TableName
            }, partitionKey.PartitionKeyName, partitionKey.SortKeyName, partitionKey.PartitionKeyValue);

        public IDynamoTableQuerySortKeyCondition QueryMixed(DynamoPrimaryKey partitionKey, int limit, bool scanIndexForward = true, bool consistentRead = false)
            => new DynamoTableQuery(this, new QueryRequest {
                ConsistentRead = consistentRead,
                Limit = limit,
                ScanIndexForward = scanIndexForward,
                TableName = TableName
            }, partitionKey.PartitionKeyName, partitionKey.SortKeyName, partitionKey.PartitionKeyValue);

        public IDynamoTableQuerySortKeyCondition QueryMixed(ADynamoSecondaryKey partitionKey, int limit, bool scanIndexForward, bool consistentRead)
            => new DynamoTableQuery(this, new QueryRequest {
                ConsistentRead = consistentRead,
                IndexName = partitionKey.IndexName,
                Limit = limit,
                ScanIndexForward = scanIndexForward,
                TableName = TableName
            }, partitionKey.PartitionKeyName, partitionKey.SortKeyName, partitionKey.PartitionKeyValue);

        public TRecord? DeserializeItem<TRecord>(Dictionary<string, AttributeValue> attributes)
            where TRecord : class
            => DynamoSerializer.Deserialize<TRecord>(attributes, SerializerOptions);

        public object? DeserializeItem(Dictionary<string, AttributeValue> attributes, Type? targetType)
            => DynamoSerializer.Deserialize(attributes, targetType, SerializerOptions);

        public Dictionary<string, AttributeValue> SerializeItem<TRecord>(TRecord record, DynamoPrimaryKey<TRecord> primaryKey, ADynamoSecondaryKey[] secondaryKeys)
            where TRecord : class
        {
            var attributes = DynamoSerializer.Serialize(record, SerializerOptions)?.M;
            if(attributes is null) {
                throw new ArgumentException("cannot serialize null record", nameof(record));
            }

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

        public IDynamoTableBatchGetItem<TRecord> BatchGetItem<TRecord>(IEnumerable<DynamoPrimaryKey<TRecord>> primaryKeys, bool consistentRead)
            where TRecord : class
        {
            if(!primaryKeys.Any()) {
                throw new ArgumentException("primary keys cannot be empty", nameof(primaryKeys));
            }
            if(primaryKeys.Count() > 100) {
                throw new ArgumentException("too many primary keys", nameof(primaryKeys));
            }
            var request = new BatchGetItemRequest {
                RequestItems = {
                    [TableName] = new KeysAndAttributes {
                        ConsistentRead = consistentRead,
                        Keys = primaryKeys.Select(primaryKey => new Dictionary<string, AttributeValue> {
                            [primaryKey.PartitionKeyName] = new AttributeValue(primaryKey.PartitionKeyValue),
                            [primaryKey.SortKeyName] = new AttributeValue(primaryKey.SortKeyValue)
                        }).ToList()
                    }
                }
            };
            return new DynamoTableBatchGetItem<TRecord>(this, request);
        }

        public IDynamoTableBatchGetItem BatchGetItemMixed(bool consistentRead)
            => new DynamoTableBatchGetItem(this, new BatchGetItemRequest {
                RequestItems = {
                    [TableName] = new KeysAndAttributes {
                        ConsistentRead = consistentRead
                    }
                }
            });

        public IDynamoTableBatchWriteItem BatchWriteItem( )
            => new DynamoTableBatchWriteItem(this, new BatchWriteItemRequest {
                RequestItems = {
                    [TableName] = new List<WriteRequest>()
                }
            });

        public IDynamoTableTransactGetItems<TRecord> TransactGetItemsMixed<TRecord>(IEnumerable<DynamoPrimaryKey<TRecord>> primaryKeys) where TRecord : class
            => new DynamoTableTransactGetItems<TRecord>(this, new TransactGetItemsRequest {
                TransactItems = primaryKeys.Select(primaryKey => new TransactGetItem {
                    Get = new Get {
                        TableName = TableName,
                        Key = new Dictionary<string, AttributeValue> {
                            [primaryKey.PartitionKeyName] = new AttributeValue(primaryKey.PartitionKeyValue),
                            [primaryKey.SortKeyName] = new AttributeValue(primaryKey.SortKeyValue)
                        }
                    }
                }).ToList()
            });

        public IDynamoTableTransactGetItems TransactGetItemsMixed()
            => new DynamoTableTransactGetItems(this, new TransactGetItemsRequest());
    }
}

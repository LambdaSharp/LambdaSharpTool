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
using System.Globalization;
using System.Linq;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using LambdaSharp.DynamoDB.Native.Internal;
using LambdaSharp.DynamoDB.Native.Operations;
using LambdaSharp.DynamoDB.Serialization;

namespace LambdaSharp.DynamoDB.Native {

    public class DynamoTable : IDynamoTable {

        //--- Constructors ---
        public DynamoTable(string tableName, IAmazonDynamoDB? dynamoClient = null, DynamoTableOptions? options = null) {
            TableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
            DynamoClient = dynamoClient ?? new AmazonDynamoDBClient();
            Options = options ?? new DynamoTableOptions();
        }

        //--- Properties ---
        public IAmazonDynamoDB DynamoClient { get; }
        public DynamoTableOptions Options { get; set; }
        public string TableName { get; }
        internal DynamoSerializerOptions SerializerOptions => Options.SerializerOptions;

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

        public IDynamoTableQuery<TRecord> Query<TRecord>(IDynamoQuerySelect<TRecord> querySelect, int limit, bool scanIndexForward, bool consistentRead)
            where TRecord : class
            => new DynamoTableQuery<TRecord>(this, new QueryRequest {
                ConsistentRead = consistentRead,
                Limit = limit,
                ScanIndexForward = scanIndexForward,
                TableName = TableName
            }, (ADynamoQuerySelect<TRecord>)querySelect);

        public IDynamoTableBatchGetItems<TRecord> BatchGetItems<TRecord>(IEnumerable<DynamoPrimaryKey<TRecord>> primaryKeys, bool consistentRead)
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
            return new DynamoTableBatchGetItems<TRecord>(this, request);
        }

        public IDynamoTableBatchGetItems BatchGetItemsMixed(bool consistentRead)
            => new DynamoTableBatchGetItems(this, new BatchGetItemRequest {
                RequestItems = {
                    [TableName] = new KeysAndAttributes {
                        ConsistentRead = consistentRead
                    }
                }
            });

        public IDynamoTableBatchWriteItems BatchWriteItems( )
            => new DynamoTableBatchWriteItems(this, new BatchWriteItemRequest {
                RequestItems = {
                    [TableName] = new List<WriteRequest>()
                }
            });

        public IDynamoTableTransactGetItems<TRecord> TransactGetItems<TRecord>(IEnumerable<DynamoPrimaryKey<TRecord>> primaryKeys) where TRecord : class
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

        internal Dictionary<string, AttributeValue> SerializeItem<TRecord>(TRecord record, DynamoPrimaryKey<TRecord> primaryKey, ADynamoSecondaryKey[] secondaryKeys)
            where TRecord : class
        {
            var attributes = DynamoSerializer.Serialize(record, SerializerOptions)?.M;
            if(attributes is null) {
                throw new ArgumentException("cannot serialize null record", nameof(record));
            }

            // add type details
            attributes["_t"] = new AttributeValue(Options.GetRecordTypeName(typeof(TRecord)));

            // add modified details
            attributes["_m"] = new AttributeValue {
                N = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture)
            };

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

        internal TRecord? DeserializeItem<TRecord>(Dictionary<string, AttributeValue> item)
            where TRecord : class
            => DynamoSerializer.Deserialize<TRecord>(item, SerializerOptions);

        internal object? DeserializeItem(Dictionary<string, AttributeValue> item, Type? type)
            => DynamoSerializer.Deserialize(item, type, SerializerOptions);

        internal object? DeserializeItemUsingRecordType(Dictionary<string, AttributeValue> item, Type expectedRecordType) {
            Type? type = null;

            // determine deserialization type by inspecting record meta-data
            if(
                item.TryGetValue("_t", out var itemTypeAttribute)
                && !(itemTypeAttribute.S is null)
            ) {
                type = Options.GetRecordType(itemTypeAttribute.S);
            }

            // fallback to expected record type
            return DeserializeItem(item, type ?? expectedRecordType);
        }
    }
}

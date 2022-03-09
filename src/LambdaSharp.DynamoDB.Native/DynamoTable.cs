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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using LambdaSharp.DynamoDB.Native.Operations;
using LambdaSharp.DynamoDB.Native.Operations.Internal;
using LambdaSharp.DynamoDB.Native.Query.Internal;
using LambdaSharp.DynamoDB.Serialization;

namespace LambdaSharp.DynamoDB.Native {

    /// <summary>
    /// Implementation for accessing DynamoDB operations in a type-safe mannter using LINQ expressions.
    /// </summary>
    public class DynamoTable : IDynamoTable {

        //--- Constructors ---

        /// <summary>
        /// Create a new <see cref="DynamoTable"/> instance.
        /// </summary>
        /// <param name="tableName">The name of the table.</param>
        /// <param name="dynamoClient">The <c>IAmazonDynamoDB</c> client to use.</param>
        /// <param name="options">The table access options.</param>
        public DynamoTable(string tableName, IAmazonDynamoDB? dynamoClient = null, DynamoTableOptions? options = null) {
            TableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
            DynamoClient = dynamoClient ?? new AmazonDynamoDBClient();
            Options = options ?? new DynamoTableOptions();
        }

        //--- Properties ---

        /// <summary>
        /// Get the <c>IAmazonDynamoDB</c> client.
        /// </summary>
        public IAmazonDynamoDB DynamoClient { get; }

        /// <summary>
        /// Get/set the table access options.
        /// </summary>
        public DynamoTableOptions Options { get; set; }

        /// <summary>
        /// Get the DynamoDB table name.
        /// </summary>
        public string TableName { get; }

        internal DynamoSerializerOptions SerializerOptions => Options.SerializerOptions;

        //--- Methods ---

        /// <summary>
        /// Specify the GetItem operation for the given primary key.
        /// </summary>
        /// <param name="primaryKey">Primary key of the item to retrieve.</param>
        /// <param name="consistentRead">Boolean indicating if the read operation should be performed against the main partition (2x cost compared to eventual consistent read).</param>
        /// <typeparam name="TRecord">The record type.</typeparam>
        public IDynamoTableGetItem<TRecord> GetItem<TRecord>(DynamoPrimaryKey<TRecord> primaryKey, bool consistentRead = false)
            where TRecord : class
            => new DynamoTableGetItem<TRecord>(this, new GetItemRequest {
                ConsistentRead = consistentRead,
                Key = new Dictionary<string, AttributeValue> {
                    [primaryKey.PKName] = new AttributeValue(primaryKey.PKValue),
                    [primaryKey.SKName] = new AttributeValue(primaryKey.SKValue)
                },
                TableName = TableName
            });

        /// <summary>
        /// Specify the PutItem operation for the given primary key and record. When successful, this operation creates a new row or replaces all attributes of the matching row.
        /// </summary>
        /// <param name="primaryKey">Primary key of the item to write.</param>
        /// <param name="record">The record to write</param>
        /// <typeparam name="TRecord">The record type.</typeparam>
        public IDynamoTablePutItem<TRecord> PutItem<TRecord>(DynamoPrimaryKey<TRecord> primaryKey, TRecord record)
            where TRecord : class
            => new DynamoTablePutItem<TRecord>(this, new PutItemRequest {
                Item = SerializeItem(record, primaryKey),
                TableName = TableName
            });

        /// <summary>
        /// Specify the UpdateItem operation for the given primary key.
        /// </summary>
        /// <param name="primaryKey">Primary key of the item to update.</param>
        /// <typeparam name="TRecord">The record type.</typeparam>
        public IDynamoTableUpdateItem<TRecord> UpdateItem<TRecord>(DynamoPrimaryKey<TRecord> primaryKey)
            where TRecord : class
            => new DynamoTableUpdateItem<TRecord>(this, new UpdateItemRequest {
                Key = new Dictionary<string, AttributeValue> {
                    [primaryKey.PKName] = new AttributeValue(primaryKey.PKValue),
                    [primaryKey.SKName] = new AttributeValue(primaryKey.SKValue)
                },
                TableName = TableName
            });

        /// <summary>
        /// /// Specify the DeleteItem operation for the given primary key.
        /// </summary>
        /// <param name="primaryKey">Primary key of the item to delete.</param>
        /// <typeparam name="TRecord">The record type.</typeparam>
        public IDynamoTableDeleteItem<TRecord> DeleteItem<TRecord>(DynamoPrimaryKey<TRecord> primaryKey)
            where TRecord : class
            => new DynamoTableDeleteItem<TRecord>(this, new DeleteItemRequest {
                Key = new Dictionary<string, AttributeValue> {
                    [primaryKey.PKName] = new AttributeValue(primaryKey.PKValue),
                    [primaryKey.SKName] = new AttributeValue(primaryKey.SKValue)
                },
                TableName = TableName
            });

        /// <summary>
        /// Specify the BatchGetItems operation for a list of primary keys that all share the same record type.
        /// </summary>
        /// <param name="primaryKeys">List of primary keys to retrieve.</param>
        /// <param name="consistentRead">Boolean indicating if the read operations should be performed against the main partition (2x cost compared to eventual consistent read).</param>
        /// <typeparam name="TRecord">The record type.</typeparam>
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
                            [primaryKey.PKName] = new AttributeValue(primaryKey.PKValue),
                            [primaryKey.SKName] = new AttributeValue(primaryKey.SKValue)
                        }).ToList()
                    }
                }
            };
            return new DynamoTableBatchGetItems<TRecord>(this, request);
        }

        /// <summary>
        /// Specify the BatchGetItems operation with mixed record types.
        /// </summary>
        /// <param name="consistentRead">Boolean indicating if the read operations should be performed against the main partition (2x cost compared to eventual consistent read).</param>
        public IDynamoTableBatchGetItems BatchGetItems(bool consistentRead)
            => new DynamoTableBatchGetItems(this, new BatchGetItemRequest {
                RequestItems = {
                    [TableName] = new KeysAndAttributes {
                        ConsistentRead = consistentRead
                    }
                }
            });

        /// <summary>
        /// Specify the BatchWriteItems operation to write or delete rows.
        /// </summary>
        public IDynamoTableBatchWriteItems BatchWriteItems( )
            => new DynamoTableBatchWriteItems(this, new BatchWriteItemRequest {
                RequestItems = {
                    [TableName] = new List<WriteRequest>()
                }
            });

        /// <summary>
        /// Specify the TransactGetItems operation for a list of primary keys that all share the same record type.
        /// </summary>
        /// <param name="primaryKeys">List of primary keys to retrieve.</param>
        /// <typeparam name="TRecord">The record type.</typeparam>
        public IDynamoTableTransactGetItems<TRecord> TransactGetItems<TRecord>(IEnumerable<DynamoPrimaryKey<TRecord>> primaryKeys) where TRecord : class
            => new DynamoTableTransactGetItems<TRecord>(this, new TransactGetItemsRequest {
                TransactItems = primaryKeys.Select(primaryKey => new TransactGetItem {
                    Get = new Get {
                        TableName = TableName,
                        Key = new Dictionary<string, AttributeValue> {
                            [primaryKey.PKName] = new AttributeValue(primaryKey.PKValue),
                            [primaryKey.SKName] = new AttributeValue(primaryKey.SKValue)
                        }
                    }
                }).ToList()
            });

        /// <summary>
        /// /// Specify the TransactGetItems operation with mixed record types.
        /// </summary>
        public IDynamoTableTransactGetItems TransactGetItems()
            => new DynamoTableTransactGetItems(this, new TransactGetItemsRequest());

        /// <summary>
        /// Specify the TransactWriteItems to created, put, update, or delete records in a transaction.
        /// </summary>
        public IDynamoTableTransactWriteItems TransactWriteItems( )
            => new DynamoTableTransactWriteItems(this, new TransactWriteItemsRequest());

        /// <summary>
        /// Specify the Query operation to fetch a list of records of the same type.
        /// </summary>
        /// <param name="queryClause">The query clause that specifies the index and sort-key constraints.</param>
        /// <param name="limit">The maximum number of records to read.</param>
        /// <param name="scanIndexForward">The direction of index scan.</param>
        /// <param name="consistentRead">Boolean indicating if the read operations should be performed against the main partition (2x cost compared to eventual consistent read).</param>
        /// <typeparam name="TRecord">The record type.</typeparam>
        public IDynamoTableQuery<TRecord> Query<TRecord>(IDynamoQueryClause<TRecord> queryClause, int limit, bool scanIndexForward, bool consistentRead)
            where TRecord : class
            => new DynamoTableQuery<TRecord>(this, new QueryRequest {
                ConsistentRead = consistentRead,
                Limit = limit,
                ScanIndexForward = scanIndexForward,
                TableName = TableName
            }, (ADynamoQueryClause<TRecord>)queryClause);

        /// <summary>
        /// Specify the Query operation to fetch a list of mixed records type.
        /// </summary>
        /// <param name="queryClause">The query clause that specifies the index and sort-key constraints.</param>
        /// <param name="limit">The maximum number of records to read.</param>
        /// <param name="scanIndexForward">The direction of index scan.</param>
        /// <param name="consistentRead">Boolean indicating if the read operations should be performed against the main partition (2x cost compared to eventual consistent read).</param>
        public IDynamoTableQuery Query(IDynamoQueryClause queryClause, int limit, bool scanIndexForward, bool consistentRead)
            => new DynamoTableQuery<object>(this, new QueryRequest {
                ConsistentRead = consistentRead,
                Limit = limit,
                ScanIndexForward = scanIndexForward,
                TableName = TableName
            }, (ADynamoQueryClause<object>)queryClause);

        internal Dictionary<string, AttributeValue> SerializeItem<TRecord>(TRecord record, DynamoPrimaryKey<TRecord> primaryKey)
            where TRecord : class
        {
            var attributes = DynamoSerializer.Serialize(record, SerializerOptions)?.M;
            if(attributes is null) {
                throw new ArgumentException("cannot serialize null record", nameof(record));
            }

            // add type details
            attributes["_t"] = new AttributeValue(Options.GetShortTypeName(typeof(TRecord)));

            // add modified details
            attributes["_m"] = new AttributeValue {
                N = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture)
            };

            // add primary key details
            attributes[primaryKey.PKName] = new AttributeValue(primaryKey.PKValue);
            attributes[primaryKey.SKName] = new AttributeValue(primaryKey.SKValue);
            return attributes;
        }

        internal TRecord? DeserializeItem<TRecord>(Dictionary<string, AttributeValue> item)
            where TRecord : class
            => DynamoSerializer.Deserialize<TRecord>(item, SerializerOptions);

        internal object? DeserializeItemUsingRecordType(Dictionary<string, AttributeValue> item, Type expectedRecordType, Dictionary<string, Type>? requestExpectedTypes) {
            Type? type = null;

            // determine deserialization type by inspecting record meta-data
            if(
                item.TryGetValue("_t", out var itemTypeAttribute)
                && !(itemTypeAttribute.S is null)
            ) {

                // resolve stored type name to an actual type
                var fullTypeName = Options.GetFullTypeName(itemTypeAttribute.S);
                if(
                    (requestExpectedTypes is null)
                    || !requestExpectedTypes.TryGetValue(fullTypeName, out type)
                ) {
                    type = Options.GetRecordType(fullTypeName);
                }
            }

            // fallback to expected record type
            return DeserializeItem(item, type ?? expectedRecordType);
        }

        private object? DeserializeItem(Dictionary<string, AttributeValue> item, Type? type)
            => DynamoSerializer.Deserialize(item, type, SerializerOptions);
    }
}

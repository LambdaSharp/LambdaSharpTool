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
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;

namespace LambdaSharp.DynamoDB.Native.Logger {

    public class LoggingDynamoDbClient : IAmazonDynamoDB {

        //--- Fields ---
        private readonly IAmazonDynamoDB _dynamoDBClient;
        private readonly Action<object> _log;

        //--- Constructors ---
        public LoggingDynamoDbClient(IAmazonDynamoDB dynamoDBClient, Action<object> log) {
            _dynamoDBClient = dynamoDBClient ?? throw new ArgumentNullException(nameof(dynamoDBClient));
            _log = log;
        }

        //--- Methods ---
        public async Task<GetItemResponse> GetItemAsync(GetItemRequest request, CancellationToken cancellationToken = default) {
            try {
                var response = await _dynamoDBClient.GetItemAsync(request, cancellationToken);
                _log?.Invoke(new {
                    Action = nameof(GetItemAsync),
                    Request = request,
                    Response = response
                });
                return response;
            } catch(Exception e) {
                _log?.Invoke(new {
                    Action = nameof(GetItemAsync),
                    Request = request,
                    Exception = new {
                        Type = e.GetType().FullName,
                        Message = e.Message
                    }
                });
                throw;
            }
        }

        public async Task<DeleteItemResponse> DeleteItemAsync(DeleteItemRequest request, CancellationToken cancellationToken = default) {
            try {
                var response = await _dynamoDBClient.DeleteItemAsync(request, cancellationToken);
                _log?.Invoke(new {
                    Action = nameof(DeleteItemAsync),
                    Request = request,
                    Response = response
                });
                return response;
            } catch(Exception e) {
                _log?.Invoke(new {
                    Action = nameof(DeleteItemAsync),
                    Request = request,
                    Exception = new {
                        Type = e.GetType().FullName,
                        Message = e.Message
                    }
                });
                throw;
            }
        }

        public async Task<PutItemResponse> PutItemAsync(PutItemRequest request, CancellationToken cancellationToken = default) {
            try {
                var response = await _dynamoDBClient.PutItemAsync(request, cancellationToken);
                _log?.Invoke(new {
                    Action = nameof(PutItemAsync),
                    Request = request,
                    Response = response
                });
                return response;
            } catch(Exception e) {
                _log?.Invoke(new {
                    Action = nameof(PutItemAsync),
                    Request = request,
                    Exception = new {
                        Type = e.GetType().FullName,
                        Message = e.Message
                    }
                });
                throw;
            }
        }

        public async Task<UpdateItemResponse> UpdateItemAsync(UpdateItemRequest request, CancellationToken cancellationToken = default) {
            try {
                var response = await _dynamoDBClient.UpdateItemAsync(request, cancellationToken);
                _log?.Invoke(new {
                    Action = nameof(UpdateItemAsync),
                    Request = request,
                    Response = response
                });
                return response;
            } catch(Exception e) {
                _log?.Invoke(new {
                    Action = nameof(UpdateItemAsync),
                    Request = request,
                    Exception = new {
                        Type = e.GetType().FullName,
                        Message = e.Message
                    }
                });
                throw;
            }
        }

        public async Task<QueryResponse> QueryAsync(QueryRequest request, CancellationToken cancellationToken = default) {
            try {
                var response = await _dynamoDBClient.QueryAsync(request, cancellationToken);
                _log?.Invoke(new {
                    Action = nameof(QueryAsync),
                    Request = request,
                    Response = response
                });
                return response;
            } catch(Exception e) {
                _log?.Invoke(new {
                    Action = nameof(QueryAsync),
                    Request = request,
                    Exception = new {
                        Type = e.GetType().FullName,
                        Message = e.Message
                    }
                });
                throw;
            }
        }

        public async Task<BatchGetItemResponse> BatchGetItemAsync(BatchGetItemRequest request, CancellationToken cancellationToken = default) {
            try {
                var response = await _dynamoDBClient.BatchGetItemAsync(request, cancellationToken);
                _log?.Invoke(new {
                    Action = nameof(BatchGetItemAsync),
                    Request = request,
                    Response = response
                });
                return response;
            } catch(Exception e) {
                _log?.Invoke(new {
                    Action = nameof(BatchGetItemAsync),
                    Request = request,
                    Exception = new {
                        Type = e.GetType().FullName,
                        Message = e.Message
                    }
                });
                throw;
            }
        }

        public async Task<BatchWriteItemResponse> BatchWriteItemAsync(BatchWriteItemRequest request, CancellationToken cancellationToken = default) {
            try {
                var response = await _dynamoDBClient.BatchWriteItemAsync(request, cancellationToken);
                _log?.Invoke(new {
                    Action = nameof(BatchWriteItemAsync),
                    Request = request,
                    Response = response
                });
                return response;
            } catch(Exception e) {
                _log?.Invoke(new {
                    Action = nameof(BatchWriteItemAsync),
                    Request = request,
                    Exception = new {
                        Type = e.GetType().FullName,
                        Message = e.Message
                    }
                });
                throw;
            }
        }

        public async Task<TransactGetItemsResponse> TransactGetItemsAsync(TransactGetItemsRequest request, CancellationToken cancellationToken = default) {
            try {
                var response = await _dynamoDBClient.TransactGetItemsAsync(request, cancellationToken);
                _log?.Invoke(new {
                    Action = nameof(TransactGetItemsAsync),
                    Request = request,
                    Response = response
                });
                return response;
            } catch(Exception e) {
                _log?.Invoke(new {
                    Action = nameof(TransactGetItemsAsync),
                    Request = request,
                    Exception = new {
                        Type = e.GetType().FullName,
                        Message = e.Message
                    }
                });
                throw;
            }
        }

        public async Task<TransactWriteItemsResponse> TransactWriteItemsAsync(TransactWriteItemsRequest request, CancellationToken cancellationToken = default) {
            try {
                var response = await _dynamoDBClient.TransactWriteItemsAsync(request, cancellationToken);
                _log?.Invoke(new {
                    Action = nameof(TransactWriteItemsAsync),
                    Request = request,
                    Response = response
                });
                return response;
            } catch(Exception e) {
                _log?.Invoke(new {
                    Action = nameof(TransactWriteItemsAsync),
                    Request = request,
                    Exception = new {
                        Type = e.GetType().FullName,
                        Message = e.Message
                    }
                });
                throw;
            }
        }

        #region *** Not Supported ***

        //--- Properties ---
        public IDynamoDBv2PaginatorFactory Paginators => throw new NotSupportedException();
        public IClientConfig Config => throw new NotSupportedException();

        //--- Methods ---
        public Task<BatchExecuteStatementResponse> BatchExecuteStatementAsync(BatchExecuteStatementRequest request, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<BatchGetItemResponse> BatchGetItemAsync(Dictionary<string, KeysAndAttributes> requestItems, ReturnConsumedCapacity returnConsumedCapacity, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<BatchGetItemResponse> BatchGetItemAsync(Dictionary<string, KeysAndAttributes> requestItems, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<BatchWriteItemResponse> BatchWriteItemAsync(Dictionary<string, List<WriteRequest>> requestItems, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<CreateBackupResponse> CreateBackupAsync(CreateBackupRequest request, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<CreateGlobalTableResponse> CreateGlobalTableAsync(CreateGlobalTableRequest request, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<CreateTableResponse> CreateTableAsync(string tableName, List<KeySchemaElement> keySchema, List<AttributeDefinition> attributeDefinitions, ProvisionedThroughput provisionedThroughput, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<CreateTableResponse> CreateTableAsync(CreateTableRequest request, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<DeleteBackupResponse> DeleteBackupAsync(DeleteBackupRequest request, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<DeleteItemResponse> DeleteItemAsync(string tableName, Dictionary<string, AttributeValue> key, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<DeleteItemResponse> DeleteItemAsync(string tableName, Dictionary<string, AttributeValue> key, ReturnValue returnValues, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<DeleteTableResponse> DeleteTableAsync(string tableName, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<DeleteTableResponse> DeleteTableAsync(DeleteTableRequest request, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<DescribeBackupResponse> DescribeBackupAsync(DescribeBackupRequest request, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<DescribeContinuousBackupsResponse> DescribeContinuousBackupsAsync(DescribeContinuousBackupsRequest request, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<DescribeContributorInsightsResponse> DescribeContributorInsightsAsync(DescribeContributorInsightsRequest request, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<DescribeEndpointsResponse> DescribeEndpointsAsync(DescribeEndpointsRequest request, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<DescribeExportResponse> DescribeExportAsync(DescribeExportRequest request, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<DescribeGlobalTableResponse> DescribeGlobalTableAsync(DescribeGlobalTableRequest request, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<DescribeGlobalTableSettingsResponse> DescribeGlobalTableSettingsAsync(DescribeGlobalTableSettingsRequest request, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<DescribeKinesisStreamingDestinationResponse> DescribeKinesisStreamingDestinationAsync(DescribeKinesisStreamingDestinationRequest request, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<DescribeLimitsResponse> DescribeLimitsAsync(DescribeLimitsRequest request, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<DescribeTableResponse> DescribeTableAsync(string tableName, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<DescribeTableResponse> DescribeTableAsync(DescribeTableRequest request, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<DescribeTableReplicaAutoScalingResponse> DescribeTableReplicaAutoScalingAsync(DescribeTableReplicaAutoScalingRequest request, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<DescribeTimeToLiveResponse> DescribeTimeToLiveAsync(string tableName, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<DescribeTimeToLiveResponse> DescribeTimeToLiveAsync(DescribeTimeToLiveRequest request, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<DisableKinesisStreamingDestinationResponse> DisableKinesisStreamingDestinationAsync(DisableKinesisStreamingDestinationRequest request, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public void Dispose() {
            throw new NotSupportedException();
        }

        public Task<EnableKinesisStreamingDestinationResponse> EnableKinesisStreamingDestinationAsync(EnableKinesisStreamingDestinationRequest request, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<ExecuteStatementResponse> ExecuteStatementAsync(ExecuteStatementRequest request, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<ExecuteTransactionResponse> ExecuteTransactionAsync(ExecuteTransactionRequest request, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<ExportTableToPointInTimeResponse> ExportTableToPointInTimeAsync(ExportTableToPointInTimeRequest request, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<GetItemResponse> GetItemAsync(string tableName, Dictionary<string, AttributeValue> key, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<GetItemResponse> GetItemAsync(string tableName, Dictionary<string, AttributeValue> key, bool consistentRead, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<ListBackupsResponse> ListBackupsAsync(ListBackupsRequest request, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<ListContributorInsightsResponse> ListContributorInsightsAsync(ListContributorInsightsRequest request, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<ListExportsResponse> ListExportsAsync(ListExportsRequest request, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<ListGlobalTablesResponse> ListGlobalTablesAsync(ListGlobalTablesRequest request, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<ListTablesResponse> ListTablesAsync(CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<ListTablesResponse> ListTablesAsync(string exclusiveStartTableName, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<ListTablesResponse> ListTablesAsync(string exclusiveStartTableName, int limit, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<ListTablesResponse> ListTablesAsync(int limit, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<ListTablesResponse> ListTablesAsync(ListTablesRequest request, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<ListTagsOfResourceResponse> ListTagsOfResourceAsync(ListTagsOfResourceRequest request, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<PutItemResponse> PutItemAsync(string tableName, Dictionary<string, AttributeValue> item, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<PutItemResponse> PutItemAsync(string tableName, Dictionary<string, AttributeValue> item, ReturnValue returnValues, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<RestoreTableFromBackupResponse> RestoreTableFromBackupAsync(RestoreTableFromBackupRequest request, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<RestoreTableToPointInTimeResponse> RestoreTableToPointInTimeAsync(RestoreTableToPointInTimeRequest request, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<ScanResponse> ScanAsync(string tableName, List<string> attributesToGet, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<ScanResponse> ScanAsync(string tableName, Dictionary<string, Condition> scanFilter, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<ScanResponse> ScanAsync(string tableName, List<string> attributesToGet, Dictionary<string, Condition> scanFilter, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<ScanResponse> ScanAsync(ScanRequest request, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<TagResourceResponse> TagResourceAsync(TagResourceRequest request, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<UntagResourceResponse> UntagResourceAsync(UntagResourceRequest request, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<UpdateContinuousBackupsResponse> UpdateContinuousBackupsAsync(UpdateContinuousBackupsRequest request, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<UpdateContributorInsightsResponse> UpdateContributorInsightsAsync(UpdateContributorInsightsRequest request, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<UpdateGlobalTableResponse> UpdateGlobalTableAsync(UpdateGlobalTableRequest request, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<UpdateGlobalTableSettingsResponse> UpdateGlobalTableSettingsAsync(UpdateGlobalTableSettingsRequest request, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<UpdateItemResponse> UpdateItemAsync(string tableName, Dictionary<string, AttributeValue> key, Dictionary<string, AttributeValueUpdate> attributeUpdates, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<UpdateItemResponse> UpdateItemAsync(string tableName, Dictionary<string, AttributeValue> key, Dictionary<string, AttributeValueUpdate> attributeUpdates, ReturnValue returnValues, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<UpdateTableResponse> UpdateTableAsync(string tableName, ProvisionedThroughput provisionedThroughput, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<UpdateTableResponse> UpdateTableAsync(UpdateTableRequest request, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<UpdateTableReplicaAutoScalingResponse> UpdateTableReplicaAutoScalingAsync(UpdateTableReplicaAutoScalingRequest request, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<UpdateTimeToLiveResponse> UpdateTimeToLiveAsync(UpdateTimeToLiveRequest request, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }
        #endregion
    }
}

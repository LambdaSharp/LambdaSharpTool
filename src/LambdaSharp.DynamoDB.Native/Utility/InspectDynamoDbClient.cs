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
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;

namespace LambdaSharp.DynamoDB.Native.Utility {

    /// <summary>
    /// The <see cref="InspectDynamoDbClient"/> class inserts a callback for IAmazonDynamoDB operations that
    /// captures the request and respose (or exception) of the operation. This class is useful for inspecting
    /// or logging what information actually goes across the wire when calling the DynamoDB API.
    /// </summary>
    public class InspectDynamoDbClient : IAmazonDynamoDB {

        //--- Fields ---
        private readonly IAmazonDynamoDB _dynamoDBClient;
        private readonly Action<object> _callback;

        //--- Constructors ---

        /// <summary>
        /// Create new instance of <see cref="InspectDynamoDbClient"/>.
        /// </summary>
        /// <param name="dynamoDBClient">The DynamoDB client to wrap.</param>
        /// <param name="callback">The inspection callback.</param>
        public InspectDynamoDbClient(IAmazonDynamoDB dynamoDBClient, Action<object> callback) {
            _dynamoDBClient = dynamoDBClient ?? throw new ArgumentNullException(nameof(dynamoDBClient));
            _callback = callback;
        }

        //--- IDisposable Members ---
        void IDisposable.Dispose( ) => _dynamoDBClient.Dispose();

        //--- IAmazonService Members ---
        IClientConfig IAmazonService.Config => _dynamoDBClient.Config;

        //--- IAmazonDynamoDB Members ---
        IDynamoDBv2PaginatorFactory IAmazonDynamoDB.Paginators => _dynamoDBClient.Paginators;

        async Task<GetItemResponse> IAmazonDynamoDB.GetItemAsync(GetItemRequest request, CancellationToken cancellationToken) {
            try {
                var response = await _dynamoDBClient.GetItemAsync(request, cancellationToken);
                _callback?.Invoke(new {
                    Action = nameof(IAmazonDynamoDB.GetItemAsync),
                    Request = request,
                    Response = response
                });
                return response;
            } catch(Exception e) {
                _callback?.Invoke(new {
                    Action = nameof(IAmazonDynamoDB.GetItemAsync),
                    Request = request,
                    Exception = new {
                        Type = e.GetType().FullName,
                        Message = e.Message
                    }
                });
                throw;
            }
        }

        async Task<DeleteItemResponse> IAmazonDynamoDB.DeleteItemAsync(DeleteItemRequest request, CancellationToken cancellationToken) {
            try {
                var response = await _dynamoDBClient.DeleteItemAsync(request, cancellationToken);
                _callback?.Invoke(new {
                    Action = nameof(IAmazonDynamoDB.DeleteItemAsync),
                    Request = request,
                    Response = response
                });
                return response;
            } catch(Exception e) {
                _callback?.Invoke(new {
                    Action = nameof(IAmazonDynamoDB.DeleteItemAsync),
                    Request = request,
                    Exception = new {
                        Type = e.GetType().FullName,
                        Message = e.Message
                    }
                });
                throw;
            }
        }

        async Task<PutItemResponse> IAmazonDynamoDB.PutItemAsync(PutItemRequest request, CancellationToken cancellationToken) {
            try {
                var response = await _dynamoDBClient.PutItemAsync(request, cancellationToken);
                _callback?.Invoke(new {
                    Action = nameof(IAmazonDynamoDB.PutItemAsync),
                    Request = request,
                    Response = response
                });
                return response;
            } catch(Exception e) {
                _callback?.Invoke(new {
                    Action = nameof(IAmazonDynamoDB.PutItemAsync),
                    Request = request,
                    Exception = new {
                        Type = e.GetType().FullName,
                        Message = e.Message
                    }
                });
                throw;
            }
        }

        async Task<UpdateItemResponse> IAmazonDynamoDB.UpdateItemAsync(UpdateItemRequest request, CancellationToken cancellationToken) {
            try {
                var response = await _dynamoDBClient.UpdateItemAsync(request, cancellationToken);
                _callback?.Invoke(new {
                    Action = nameof(IAmazonDynamoDB.UpdateItemAsync),
                    Request = request,
                    Response = response
                });
                return response;
            } catch(Exception e) {
                _callback?.Invoke(new {
                    Action = nameof(IAmazonDynamoDB.UpdateItemAsync),
                    Request = request,
                    Exception = new {
                        Type = e.GetType().FullName,
                        Message = e.Message
                    }
                });
                throw;
            }
        }

        async Task<QueryResponse> IAmazonDynamoDB.QueryAsync(QueryRequest request, CancellationToken cancellationToken) {
            try {
                var response = await _dynamoDBClient.QueryAsync(request, cancellationToken);
                _callback?.Invoke(new {
                    Action = nameof(IAmazonDynamoDB.QueryAsync),
                    Request = request,
                    Response = response
                });
                return response;
            } catch(Exception e) {
                _callback?.Invoke(new {
                    Action = nameof(IAmazonDynamoDB.QueryAsync),
                    Request = request,
                    Exception = new {
                        Type = e.GetType().FullName,
                        Message = e.Message
                    }
                });
                throw;
            }
        }

        async Task<BatchGetItemResponse> IAmazonDynamoDB.BatchGetItemAsync(BatchGetItemRequest request, CancellationToken cancellationToken) {
            try {
                var response = await _dynamoDBClient.BatchGetItemAsync(request, cancellationToken);
                _callback?.Invoke(new {
                    Action = nameof(IAmazonDynamoDB.BatchGetItemAsync),
                    Request = request,
                    Response = response
                });
                return response;
            } catch(Exception e) {
                _callback?.Invoke(new {
                    Action = nameof(IAmazonDynamoDB.BatchGetItemAsync),
                    Request = request,
                    Exception = new {
                        Type = e.GetType().FullName,
                        Message = e.Message
                    }
                });
                throw;
            }
        }

        async Task<BatchWriteItemResponse> IAmazonDynamoDB.BatchWriteItemAsync(BatchWriteItemRequest request, CancellationToken cancellationToken) {
            try {
                var response = await _dynamoDBClient.BatchWriteItemAsync(request, cancellationToken);
                _callback?.Invoke(new {
                    Action = nameof(IAmazonDynamoDB.BatchWriteItemAsync),
                    Request = request,
                    Response = response
                });
                return response;
            } catch(Exception e) {
                _callback?.Invoke(new {
                    Action = nameof(IAmazonDynamoDB.BatchWriteItemAsync),
                    Request = request,
                    Exception = new {
                        Type = e.GetType().FullName,
                        Message = e.Message
                    }
                });
                throw;
            }
        }

        async Task<TransactGetItemsResponse> IAmazonDynamoDB.TransactGetItemsAsync(TransactGetItemsRequest request, CancellationToken cancellationToken) {
            try {
                var response = await _dynamoDBClient.TransactGetItemsAsync(request, cancellationToken);
                _callback?.Invoke(new {
                    Action = nameof(IAmazonDynamoDB.TransactGetItemsAsync),
                    Request = request,
                    Response = response
                });
                return response;
            } catch(Exception e) {
                _callback?.Invoke(new {
                    Action = nameof(IAmazonDynamoDB.TransactGetItemsAsync),
                    Request = request,
                    Exception = new {
                        Type = e.GetType().FullName,
                        Message = e.Message
                    }
                });
                throw;
            }
        }

        async Task<TransactWriteItemsResponse> IAmazonDynamoDB.TransactWriteItemsAsync(TransactWriteItemsRequest request, CancellationToken cancellationToken) {
            try {
                var response = await _dynamoDBClient.TransactWriteItemsAsync(request, cancellationToken);
                _callback?.Invoke(new {
                    Action = nameof(IAmazonDynamoDB.TransactWriteItemsAsync),
                    Request = request,
                    Response = response
                });
                return response;
            } catch(Exception e) {
                _callback?.Invoke(new {
                    Action = nameof(IAmazonDynamoDB.TransactWriteItemsAsync),
                    Request = request,
                    Exception = new {
                        Type = e.GetType().FullName,
                        Message = e.Message
                    }
                });
                throw;
            }
        }

        async Task<ScanResponse> IAmazonDynamoDB.ScanAsync(ScanRequest request, CancellationToken cancellationToken) {
            try {
                var response = await _dynamoDBClient.ScanAsync(request, cancellationToken);
                _callback?.Invoke(new {
                    Action = nameof(IAmazonDynamoDB.ScanAsync),
                    Request = request,
                    Response = response
                });
                return response;
            } catch(Exception e) {
                _callback?.Invoke(new {
                    Action = nameof(IAmazonDynamoDB.ScanAsync),
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
        Task<BatchExecuteStatementResponse> IAmazonDynamoDB.BatchExecuteStatementAsync(BatchExecuteStatementRequest request, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<BatchGetItemResponse> IAmazonDynamoDB.BatchGetItemAsync(Dictionary<string, KeysAndAttributes> requestItems, ReturnConsumedCapacity returnConsumedCapacity, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<BatchGetItemResponse> IAmazonDynamoDB.BatchGetItemAsync(Dictionary<string, KeysAndAttributes> requestItems, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<BatchWriteItemResponse> IAmazonDynamoDB.BatchWriteItemAsync(Dictionary<string, List<WriteRequest>> requestItems, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<CreateBackupResponse> IAmazonDynamoDB.CreateBackupAsync(CreateBackupRequest request, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<CreateGlobalTableResponse> IAmazonDynamoDB.CreateGlobalTableAsync(CreateGlobalTableRequest request, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<CreateTableResponse> IAmazonDynamoDB.CreateTableAsync(string tableName, List<KeySchemaElement> keySchema, List<AttributeDefinition> attributeDefinitions, ProvisionedThroughput provisionedThroughput, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<CreateTableResponse> IAmazonDynamoDB.CreateTableAsync(CreateTableRequest request, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<DeleteBackupResponse> IAmazonDynamoDB.DeleteBackupAsync(DeleteBackupRequest request, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<DeleteItemResponse> IAmazonDynamoDB.DeleteItemAsync(string tableName, Dictionary<string, AttributeValue> key, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<DeleteItemResponse> IAmazonDynamoDB.DeleteItemAsync(string tableName, Dictionary<string, AttributeValue> key, ReturnValue returnValues, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<DeleteTableResponse> IAmazonDynamoDB.DeleteTableAsync(string tableName, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<DeleteTableResponse> IAmazonDynamoDB.DeleteTableAsync(DeleteTableRequest request, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<DescribeBackupResponse> IAmazonDynamoDB.DescribeBackupAsync(DescribeBackupRequest request, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<DescribeContinuousBackupsResponse> IAmazonDynamoDB.DescribeContinuousBackupsAsync(DescribeContinuousBackupsRequest request, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<DescribeContributorInsightsResponse> IAmazonDynamoDB.DescribeContributorInsightsAsync(DescribeContributorInsightsRequest request, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<DescribeEndpointsResponse> IAmazonDynamoDB.DescribeEndpointsAsync(DescribeEndpointsRequest request, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<DescribeExportResponse> IAmazonDynamoDB.DescribeExportAsync(DescribeExportRequest request, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<DescribeGlobalTableResponse> IAmazonDynamoDB.DescribeGlobalTableAsync(DescribeGlobalTableRequest request, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<DescribeGlobalTableSettingsResponse> IAmazonDynamoDB.DescribeGlobalTableSettingsAsync(DescribeGlobalTableSettingsRequest request, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<DescribeKinesisStreamingDestinationResponse> IAmazonDynamoDB.DescribeKinesisStreamingDestinationAsync(DescribeKinesisStreamingDestinationRequest request, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<DescribeLimitsResponse> IAmazonDynamoDB.DescribeLimitsAsync(DescribeLimitsRequest request, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<DescribeTableResponse> IAmazonDynamoDB.DescribeTableAsync(string tableName, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<DescribeTableResponse> IAmazonDynamoDB.DescribeTableAsync(DescribeTableRequest request, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<DescribeTableReplicaAutoScalingResponse> IAmazonDynamoDB.DescribeTableReplicaAutoScalingAsync(DescribeTableReplicaAutoScalingRequest request, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<DescribeTimeToLiveResponse> IAmazonDynamoDB.DescribeTimeToLiveAsync(string tableName, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<DescribeTimeToLiveResponse> IAmazonDynamoDB.DescribeTimeToLiveAsync(DescribeTimeToLiveRequest request, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<DisableKinesisStreamingDestinationResponse> IAmazonDynamoDB.DisableKinesisStreamingDestinationAsync(DisableKinesisStreamingDestinationRequest request, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<EnableKinesisStreamingDestinationResponse> IAmazonDynamoDB.EnableKinesisStreamingDestinationAsync(EnableKinesisStreamingDestinationRequest request, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<ExecuteStatementResponse> IAmazonDynamoDB.ExecuteStatementAsync(ExecuteStatementRequest request, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<ExecuteTransactionResponse> IAmazonDynamoDB.ExecuteTransactionAsync(ExecuteTransactionRequest request, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<ExportTableToPointInTimeResponse> IAmazonDynamoDB.ExportTableToPointInTimeAsync(ExportTableToPointInTimeRequest request, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<GetItemResponse> IAmazonDynamoDB.GetItemAsync(string tableName, Dictionary<string, AttributeValue> key, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<GetItemResponse> IAmazonDynamoDB.GetItemAsync(string tableName, Dictionary<string, AttributeValue> key, bool consistentRead, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<ListBackupsResponse> IAmazonDynamoDB.ListBackupsAsync(ListBackupsRequest request, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<ListContributorInsightsResponse> IAmazonDynamoDB.ListContributorInsightsAsync(ListContributorInsightsRequest request, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<ListExportsResponse> IAmazonDynamoDB.ListExportsAsync(ListExportsRequest request, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<ListGlobalTablesResponse> IAmazonDynamoDB.ListGlobalTablesAsync(ListGlobalTablesRequest request, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<ListTablesResponse> IAmazonDynamoDB.ListTablesAsync(CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<ListTablesResponse> IAmazonDynamoDB.ListTablesAsync(string exclusiveStartTableName, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<ListTablesResponse> IAmazonDynamoDB.ListTablesAsync(string exclusiveStartTableName, int limit, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<ListTablesResponse> IAmazonDynamoDB.ListTablesAsync(int limit, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<ListTablesResponse> IAmazonDynamoDB.ListTablesAsync(ListTablesRequest request, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<ListTagsOfResourceResponse> IAmazonDynamoDB.ListTagsOfResourceAsync(ListTagsOfResourceRequest request, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<PutItemResponse> IAmazonDynamoDB.PutItemAsync(string tableName, Dictionary<string, AttributeValue> item, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<PutItemResponse> IAmazonDynamoDB.PutItemAsync(string tableName, Dictionary<string, AttributeValue> item, ReturnValue returnValues, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<RestoreTableFromBackupResponse> IAmazonDynamoDB.RestoreTableFromBackupAsync(RestoreTableFromBackupRequest request, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<RestoreTableToPointInTimeResponse> IAmazonDynamoDB.RestoreTableToPointInTimeAsync(RestoreTableToPointInTimeRequest request, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<ScanResponse> IAmazonDynamoDB.ScanAsync(string tableName, List<string> attributesToGet, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<ScanResponse> IAmazonDynamoDB.ScanAsync(string tableName, Dictionary<string, Condition> scanFilter, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<ScanResponse> IAmazonDynamoDB.ScanAsync(string tableName, List<string> attributesToGet, Dictionary<string, Condition> scanFilter, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<TagResourceResponse> IAmazonDynamoDB.TagResourceAsync(TagResourceRequest request, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<UntagResourceResponse> IAmazonDynamoDB.UntagResourceAsync(UntagResourceRequest request, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<UpdateContinuousBackupsResponse> IAmazonDynamoDB.UpdateContinuousBackupsAsync(UpdateContinuousBackupsRequest request, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<UpdateContributorInsightsResponse> IAmazonDynamoDB.UpdateContributorInsightsAsync(UpdateContributorInsightsRequest request, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<UpdateGlobalTableResponse> IAmazonDynamoDB.UpdateGlobalTableAsync(UpdateGlobalTableRequest request, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<UpdateGlobalTableSettingsResponse> IAmazonDynamoDB.UpdateGlobalTableSettingsAsync(UpdateGlobalTableSettingsRequest request, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<UpdateItemResponse> IAmazonDynamoDB.UpdateItemAsync(string tableName, Dictionary<string, AttributeValue> key, Dictionary<string, AttributeValueUpdate> attributeUpdates, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<UpdateItemResponse> IAmazonDynamoDB.UpdateItemAsync(string tableName, Dictionary<string, AttributeValue> key, Dictionary<string, AttributeValueUpdate> attributeUpdates, ReturnValue returnValues, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<UpdateTableResponse> IAmazonDynamoDB.UpdateTableAsync(string tableName, ProvisionedThroughput provisionedThroughput, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<UpdateTableResponse> IAmazonDynamoDB.UpdateTableAsync(UpdateTableRequest request, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<UpdateTableReplicaAutoScalingResponse> IAmazonDynamoDB.UpdateTableReplicaAutoScalingAsync(UpdateTableReplicaAutoScalingRequest request, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        Task<UpdateTimeToLiveResponse> IAmazonDynamoDB.UpdateTimeToLiveAsync(UpdateTimeToLiveRequest request, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }
        #endregion
    }
}

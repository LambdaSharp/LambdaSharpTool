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

namespace LambdaSharp.App.EventBus;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using LambdaSharp.App.EventBus.Records;

public sealed class DataTable : ADataTable {

    //--- Constants ---

    // TODO (2021-07-13, bjorg): convert to using DynamoTable!
    private const string VALID_SYMBOLS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private const string CONNECTION_PREFIX = "WS#";
    private const string TOUCH_PREFIX = "TOUCH#";
    private const string RULE_PREFIX = "RULE#";
    private const string INFO = "INFO";

    //--- Class Fields ---
    private readonly static Random _random = new Random();

    //--- Class Methods ---
    public static string GetRandomString(int length)
        => new string(Enumerable.Repeat(VALID_SYMBOLS, length).Select(chars => chars[_random.Next(chars.Length)]).ToArray());

    //--- Constructors ---
    public DataTable(string tableName, IAmazonDynamoDB? dynamoDbClient = null) : base(tableName, dynamoDbClient) { }

    //--- Methods ---

    #region Connection Record
    public async Task<ConnectionRecord> GetConnectionRecordAsync(string connectionId, CancellationToken cancellationToken = default)
        => Deserialize<ConnectionRecord>(await GetItemAsync(CONNECTION_PREFIX + connectionId, INFO, cancellationToken));

    public Task CreateConnectionRecordAsync(ConnectionRecord record, CancellationToken cancellationToken = default)
        => CreateItemAsync(
            record,
            pk: CONNECTION_PREFIX + record.ConnectionId,
            sk: INFO,
            cancellationToken
        );

    public Task UpdateConnectionRecordAsync(ConnectionRecord record, CancellationToken cancellationToken = default)
        => UpdateItemAsync(
            record,
            pk: CONNECTION_PREFIX + record.ConnectionId,
            sk: INFO,
            cancellationToken
        );

    public async Task<bool> SetConnectionRecordStateAsync(ConnectionRecord record, ConnectionState state, CancellationToken cancellationToken = default) {
        try {
            await DynamoDbClient.UpdateItemAsync(new UpdateItemRequest {
                TableName = TableName,
                Key = new Dictionary<string, AttributeValue> {
                    ["PK"] = new AttributeValue(CONNECTION_PREFIX + record.ConnectionId),
                    ["SK"] = new AttributeValue(INFO)
                },
                UpdateExpression = "SET #State = :state, #Modified = :modified",
                ExpressionAttributeNames = {
                    ["#State"] = nameof(record.State),
                    ["#Modified"] = "_Modified"
                },
                ExpressionAttributeValues = {
                    [":state"] = new AttributeValue(state.ToString()),
                    [":modified"] = new AttributeValue {
                        N = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()
                    }
                }
            });
            record.State = state;
            return true;
        } catch(ConditionalCheckFailedException) {
            return false;
        }
    }

    public async Task<bool> UpdateConnectionRecordStateAsync(ConnectionRecord record, ConnectionState state, CancellationToken cancellationToken = default) {
        try {
            await DynamoDbClient.UpdateItemAsync(new UpdateItemRequest {
                TableName = TableName,
                Key = new Dictionary<string, AttributeValue> {
                    ["PK"] = new AttributeValue(CONNECTION_PREFIX + record.ConnectionId),
                    ["SK"] = new AttributeValue(INFO)
                },
                ConditionExpression = "#State = :expectedState",
                UpdateExpression = "SET #State = :state, #Modified = :modified",
                ExpressionAttributeNames = {
                    ["#State"] = nameof(record.State),
                    ["#Modified"] = "_Modified"
                },
                ExpressionAttributeValues = {
                    [":expectedState"] = new AttributeValue(record.State.ToString()),
                    [":state"] = new AttributeValue(state.ToString()),
                    [":modified"] = new AttributeValue {
                        N = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()
                    }
                }
            });
            record.State = state;
            return true;
        } catch(ConditionalCheckFailedException) {
            return false;
        }
    }

    public async Task<bool> UpdateConnectionRecordStateAndSubscriptionAsync(
        ConnectionRecord record,
        ConnectionState state,
        string subscriptionArn,
        CancellationToken cancellationToken = default
    ) {
        try {
            await DynamoDbClient.UpdateItemAsync(new UpdateItemRequest {
                TableName = TableName,
                Key = new Dictionary<string, AttributeValue> {
                    ["PK"] = new AttributeValue(CONNECTION_PREFIX + record.ConnectionId),
                    ["SK"] = new AttributeValue(INFO)
                },
                ConditionExpression = "#State = :expectedState",
                UpdateExpression = "SET #State = :state, #ARN = :arn, #Modified = :modified",
                ExpressionAttributeNames = {
                    ["#State"] = nameof(record.State),
                    ["#ARN"] = nameof(record.SubscriptionArn),
                    ["#Modified"] = "_Modified"
                },
                ExpressionAttributeValues = {
                    [":expectedState"] = new AttributeValue(record.State.ToString()),
                    [":state"] = new AttributeValue(state.ToString()),
                    [":arn"] = new AttributeValue(subscriptionArn),
                    [":modified"] = new AttributeValue {
                        N = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()
                    }
                }
            });
            record.State = state;
            record.SubscriptionArn = subscriptionArn;
            return true;
        } catch(ConditionalCheckFailedException) {
            return false;
        }
    }

    public Task DeleteConnectionRecordAsync(string connectionId, CancellationToken cancellationToken = default)
        => DeleteItemAsync(
            pk: CONNECTION_PREFIX + connectionId,
            sk: INFO,
            cancellationToken
        );
    #endregion

    #region Rule Record
    public Task CreateOrUpdateRuleRecordAsync(RuleRecord record, CancellationToken cancellationToken = default)
        => CreateOrUpdateItemAsync(
            record,
            pk: CONNECTION_PREFIX + record.ConnectionId,
            sk: RULE_PREFIX + record.Rule,
            cancellationToken
        );
    public Task DeleteRuleRecordAsync(string connectionId, string ruleId, CancellationToken cancellationToken = default)
        => DeleteItemAsync(
            pk: CONNECTION_PREFIX + connectionId,
            sk: RULE_PREFIX + ruleId,
            cancellationToken
        );

    public async Task<IEnumerable<RuleRecord>> GetAllRuleRecordAsync(string connectionId, CancellationToken cancellationToken = default)
        => (await Search(
            pk: CONNECTION_PREFIX + connectionId,
            skOperator: QueryOperator.BeginsWith,
            skValue: RULE_PREFIX,
            cancellationToken
        )).Select(document => Deserialize<RuleRecord>(document)).ToList();

    public Task DeleteAllRuleRecordAsync(IEnumerable<RuleRecord> records, CancellationToken cancellationToken = default)
        => DeleteItemsAsync(records.Select(record => (
            PK: CONNECTION_PREFIX + record.ConnectionId,
            SK: RULE_PREFIX + record.Rule
        )), cancellationToken);
    #endregion
}

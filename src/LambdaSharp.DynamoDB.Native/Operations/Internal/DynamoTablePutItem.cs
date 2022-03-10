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
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using LambdaSharp.DynamoDB.Native.Internal;

namespace LambdaSharp.DynamoDB.Native.Operations.Internal {

    internal sealed class DynamoTablePutItem<TRecord> : IDynamoTablePutItem<TRecord>
        where TRecord : class
    {

        //--- Fields ---
        private readonly DynamoTable _table;
        private readonly PutItemRequest _request;
        private readonly DynamoRequestConverter _converter;

        //--- Constructors ---
        public DynamoTablePutItem(DynamoTable table, PutItemRequest request) {
            _table = table ?? throw new ArgumentNullException(nameof(table));
            _request = request ?? throw new ArgumentNullException(nameof(request));
            _converter = new DynamoRequestConverter(_request.ExpressionAttributeNames, _request.ExpressionAttributeValues, _table.SerializerOptions);
        }

        //--- Methods ---
        public IDynamoTablePutItem<TRecord> WithCondition(Expression<Func<TRecord, bool>> condition) {
            _converter.AddCondition(condition.Body);
            return this;
        }

        public IDynamoTablePutItem<TRecord> Set(string key, AttributeValue value) {
            _request.Item[key] = value;
            return this;
        }

        public async Task<bool> ExecuteAsync(CancellationToken cancellationToken) {
            _request.ConditionExpression = _converter.ConvertConditions(_table.Options);
            try {
                await _table.DynamoClient.PutItemAsync(_request);
                return true;
            } catch(ConditionalCheckFailedException) {
                return false;
            }
        }

        public async Task<TRecord?> ExecuteReturnOldItemAsync(CancellationToken cancellationToken) {
            _request.ConditionExpression = _converter.ConvertConditions(_table.Options);
            _request.ReturnValues = ReturnValue.ALL_OLD;
            try {
                var response = await _table.DynamoClient.PutItemAsync(_request);
                return _table.DeserializeItem<TRecord>(response.Attributes);
            } catch(ConditionalCheckFailedException) {
                return default(TRecord);
            }
        }
    }
}

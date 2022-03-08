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
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using LambdaSharp.DynamoDB.Native.Internal;
using LambdaSharp.DynamoDB.Native.Query.Internal;

namespace LambdaSharp.DynamoDB.Native.Operations.Internal {

    internal sealed class DynamoTableQuery<TRecord> : IDynamoTableQuery, IDynamoTableQuery<TRecord>
        where TRecord : class
    {

        //--- Fields ---
        private readonly DynamoTable _table;
        private readonly QueryRequest _request;
        private readonly DynamoRequestConverter _converter;
        private readonly ADynamoQueryClause<TRecord> _queryClause;

        //--- Constructors ---
        public DynamoTableQuery(DynamoTable table, QueryRequest request, ADynamoQueryClause<TRecord> queryClause) {
            _table = table ?? throw new ArgumentNullException(nameof(table));
            _request = request ?? throw new ArgumentNullException(nameof(request));
            _queryClause = queryClause ?? throw new ArgumentNullException(nameof(queryClause));
            _converter = new DynamoRequestConverter(_request.ExpressionAttributeNames, _request.ExpressionAttributeValues, _table.SerializerOptions);
        }

        //--- Methods ---
        private void PrepareRequest(bool fetchAllAttributes) {

            // inherit the expected types from the query select construct
            foreach(var expectedType in _queryClause.TypeFilters) {
                _converter.AddExpectedType(expectedType);
            }

            // initialize request
            _request.IndexName = _queryClause.IndexName;
            _request.KeyConditionExpression = _queryClause.GetKeyConditionExpression(_converter);
            _request.FilterExpression = _converter.ConvertConditions(_table.Options);
            _request.ProjectionExpression = _converter.ConvertProjections();

            // NOTE (2021-06-23, bjorg): the following logic matches the default behavior, but makes it explicit
            // if `ProjectionExpression` is set, only return specified attributes; otherwise, for an index, return projected attributes only; for tables, return all attributes from each row
            if(_request.ProjectionExpression is null) {
                if((_request.IndexName is null) || fetchAllAttributes) {
                    _request.Select = Select.ALL_ATTRIBUTES;
                } else {
                    _request.Select = Select.ALL_PROJECTED_ATTRIBUTES;
                }
            } else {
                _request.Select = Select.SPECIFIC_ATTRIBUTES;
            }
        }

        //--- IDynamoTableQuery<TRecord> Members ---
        IDynamoTableQuery<TRecord> IDynamoTableQuery<TRecord>.Where(Expression<Func<TRecord, bool>> filter) {
            _converter.AddCondition(filter.Body);
            return this;
        }

        IDynamoTableQuery<TRecord> IDynamoTableQuery<TRecord>.Get<T>(Expression<Func<TRecord, T>> attribute) {
            _converter.AddProjection(attribute.Body);

            // NOTE (2021-06-24, bjorg): we always fetch `_t` to allow polymorphic deserialization
            _converter.AddProjection("_t");
            return this;
        }

        async IAsyncEnumerable<TRecord> IDynamoTableQuery<TRecord>.ExecuteAsyncEnumerable(bool fetchAllAttributes, [EnumeratorCancellation] CancellationToken cancellationToken) {
            PrepareRequest(fetchAllAttributes);
            do {
                var response = await _table.DynamoClient.QueryAsync(_request, cancellationToken);
                foreach(var item in response.Items) {
                    var record = _table.DeserializeItemUsingRecordType(item, typeof(TRecord), _converter.ExpectedTypes);
                    if(!(record is null) && (record is TRecord typedRecord)) {
                        yield return typedRecord;
                    }
                }
                _request.ExclusiveStartKey = response.LastEvaluatedKey;
            } while(_request.ExclusiveStartKey.Any());
        }

        async Task<IEnumerable<TRecord>> IDynamoTableQuery<TRecord>.ExecuteAsync(bool fetchAllAttributes, CancellationToken cancellationToken) {
            PrepareRequest(fetchAllAttributes);
            var result = new List<TRecord>();
            do {
                var response = await _table.DynamoClient.QueryAsync(_request, cancellationToken);
                foreach(var item in response.Items) {
                    var record = _table.DeserializeItemUsingRecordType(item, typeof(TRecord), _converter.ExpectedTypes);
                    if(!(record is null) && (record is TRecord typedRecord)) {
                        result.Add(typedRecord);
                    }
                }
                _request.ExclusiveStartKey = response.LastEvaluatedKey;
            } while(_request.ExclusiveStartKey.Any());
            return result;
        }

        //--- IDynamoTableQuery Members ---
        IDynamoTableQuery IDynamoTableQuery.Where<TRecord1>(Expression<Func<TRecord1, bool>> filter) {
            _converter.AddCondition(filter.Body);
            return this;
        }

        IDynamoTableQuery IDynamoTableQuery.Get<TRecord1, T>(Expression<Func<TRecord1, T>> attribute) {
            _converter.AddProjection(attribute.Body);

            // NOTE (2021-06-24, bjorg): we always fetch `_t` to allow polymorphic deserialization
            _converter.AddProjection("_t");
            return this;
        }

        async IAsyncEnumerable<object> IDynamoTableQuery.ExecuteAsyncEnumerable(bool fetchAllAttributes, [EnumeratorCancellation] CancellationToken cancellationToken) {
            PrepareRequest(fetchAllAttributes);
            do {
                var response = await _table.DynamoClient.QueryAsync(_request, cancellationToken);
                foreach(var item in response.Items) {
                    var record = _table.DeserializeItemUsingRecordType(item, typeof(TRecord), _converter.ExpectedTypes);
                    if(!(record is null) && (record is TRecord typedRecord)) {
                        yield return typedRecord;
                    }
                }
                _request.ExclusiveStartKey = response.LastEvaluatedKey;
            } while(_request.ExclusiveStartKey.Any());
        }

        async Task<IEnumerable<object>> IDynamoTableQuery.ExecuteAsync(bool fetchAllAttributes, CancellationToken cancellationToken) {
            PrepareRequest(fetchAllAttributes);
            var result = new List<TRecord>();
            do {
                var response = await _table.DynamoClient.QueryAsync(_request, cancellationToken);
                foreach(var item in response.Items) {
                    var record = _table.DeserializeItemUsingRecordType(item, typeof(TRecord), _converter.ExpectedTypes);
                    if(!(record is null) && (record is TRecord typedRecord)) {
                        result.Add(typedRecord);
                    }
                }
                _request.ExclusiveStartKey = response.LastEvaluatedKey;
            } while(_request.ExclusiveStartKey.Any());
            return result;
        }
    }
}

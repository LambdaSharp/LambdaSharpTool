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
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using LambdaSharp.DynamoDB.Native.Operations;

namespace LambdaSharp.DynamoDB.Native.Internal {

    internal sealed class DynamoTableQuery<TRecord> : IDynamoTableQuerySortKeyCondition<TRecord>, IDynamoTableQuery<TRecord>
        where TRecord : class
    {

        //--- Fields ---
        private readonly DynamoTable _table;
        private readonly QueryRequest _request;
        private readonly DynamoRequestConverter _converter;
        private string _sortKeyName;

        //--- Constructors ---
        public DynamoTableQuery(DynamoTable table, QueryRequest request, string primaryKeyName, string sortKeyName, string primaryKeyValue) {
            _table = table ?? throw new ArgumentNullException(nameof(table));
            _request = request ?? throw new ArgumentNullException(nameof(request));
            _converter = new DynamoRequestConverter(_request.ExpressionAttributeNames, _request.ExpressionAttributeValues, _table.SerializerOptions);
            _sortKeyName = sortKeyName;
            _request.KeyConditionExpression = $"{_converter.GetAttributeName(primaryKeyName)} = {_converter.GetExpressionValueName(primaryKeyValue)}";
        }

        //--- Methods ---
        private void PrepareRequest(bool fetchAllAttributes) {
            _request.FilterExpression = _converter.ConvertConditions();
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
        IDynamoTableQuery<TRecord> IDynamoTableQuery<TRecord>.WithFilter(Expression<Func<TRecord, bool>> filter) {
            _converter.AddCondition(filter.Body);
            return this;
        }

        IDynamoTableQuery<TRecord> IDynamoTableQuery<TRecord>.Get<T>(Expression<Func<TRecord, T>> attribute) {
            _converter.AddProjection(attribute.Body);

            // NOTE (2021-06-24, bjorg): we always fetch `_t` to allow polymorphic deserialization
            _converter.AddProjection("_t");
            return this;
        }

        async IAsyncEnumerable<TRecord> IDynamoTableQuery<TRecord>.ExecuteAsyncEnumerable([EnumeratorCancellation] CancellationToken cancellationToken) {
            PrepareRequest(fetchAllAttributes: false);
            do {
                var response = await _table.DynamoClient.QueryAsync(_request, cancellationToken);
                foreach(var item in response.Items) {
                    var record = _table.DeserializeItem<TRecord>(item);
                    if(!(record is null)) {
                        yield return record;
                    }
                }
                _request.ExclusiveStartKey = response.LastEvaluatedKey;
            } while(_request.ExclusiveStartKey.Any());
        }

        async IAsyncEnumerable<TRecord> IDynamoTableQuery<TRecord>.ExecuteFetchAllAttributesAsyncEnumerable([EnumeratorCancellation] CancellationToken cancellationToken) {
            PrepareRequest(fetchAllAttributes: true);
            do {
                var response = await _table.DynamoClient.QueryAsync(_request, cancellationToken);
                foreach(var item in response.Items) {
                    var record = _table.DeserializeItem<TRecord>(item);
                    if(!(record is null)) {
                        yield return record;
                    }
                }
                _request.ExclusiveStartKey = response.LastEvaluatedKey;
            } while(_request.ExclusiveStartKey.Any());
        }

        //--- IDynamoTableQuerySortKeyCondition<TRecord> Members ---
        IDynamoTableQuery<TRecord> IDynamoTableQuerySortKeyCondition<TRecord>.WhereSKBeginsWith(string sortKeyValuePrefix) {
            _request.KeyConditionExpression += $" AND begins_with({_converter.GetAttributeName(_sortKeyName)}, {_converter.GetExpressionValueName(sortKeyValuePrefix)})";
            return this;
        }

        IDynamoTableQuery<TRecord> IDynamoTableQuerySortKeyCondition<TRecord>.WhereSKEquals(string sortKeyValue) {
            _request.KeyConditionExpression += $" AND {_converter.GetAttributeName(_sortKeyName)} = {_converter.GetExpressionValueName(sortKeyValue)}";
            return this;
        }

        IDynamoTableQuery<TRecord> IDynamoTableQuerySortKeyCondition<TRecord>.WhereSKIsBetween(string sortKeyValueLow, string sortKeyValueHigh) {
            _request.KeyConditionExpression += $" AND {_converter.GetAttributeName(_sortKeyName)} BETWEEN {_converter.GetExpressionValueName(sortKeyValueLow)} AND {_converter.GetExpressionValueName(sortKeyValueHigh)}";
            return this;
        }

        IDynamoTableQuery<TRecord> IDynamoTableQuerySortKeyCondition<TRecord>.WhereSKIsGreaterThan(string sortKeyValue) {
            _request.KeyConditionExpression += $" AND {_converter.GetAttributeName(_sortKeyName)} > {_converter.GetExpressionValueName(sortKeyValue)}";
            return this;
        }

        IDynamoTableQuery<TRecord> IDynamoTableQuerySortKeyCondition<TRecord>.WhereSKIsGreaterThanOrEquals(string sortKeyValue) {
            _request.KeyConditionExpression += $" AND {_converter.GetAttributeName(_sortKeyName)} >= {_converter.GetExpressionValueName(sortKeyValue)}";
            return this;
        }

        IDynamoTableQuery<TRecord> IDynamoTableQuerySortKeyCondition<TRecord>.WhereSKIsLessThan(string sortKeyValue) {
            _request.KeyConditionExpression += $" AND {_converter.GetAttributeName(_sortKeyName)} < {_converter.GetExpressionValueName(sortKeyValue)}";
            return this;
        }

        IDynamoTableQuery<TRecord> IDynamoTableQuerySortKeyCondition<TRecord>.WhereSKIsLessThanOrEquals(string sortKeyValue) {
            _request.KeyConditionExpression += $" AND {_converter.GetAttributeName(_sortKeyName)} <= {_converter.GetExpressionValueName(sortKeyValue)}";
            return this;
        }

        IDynamoTableQuery<TRecord> IDynamoTableQuerySortKeyCondition<TRecord>.WhereSKMatchesAny()
            => this;
    }
}

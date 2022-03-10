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
using Amazon.DynamoDBv2.Model;
using LambdaSharp.DynamoDB.Native.Internal;

namespace LambdaSharp.DynamoDB.Native.Operations.Internal {

    internal sealed class DynamoTableGetItem<TRecord> : IDynamoTableGetItem<TRecord>
        where TRecord : class
    {

        //--- Fields ---
        private readonly DynamoTable _table;
        private readonly GetItemRequest _request;
        private readonly DynamoRequestConverter _converter;

        //--- Constructors ---
        public DynamoTableGetItem(DynamoTable table, GetItemRequest request) {
            _table = table ?? throw new ArgumentNullException(nameof(table));
            _request = request ?? throw new ArgumentNullException(nameof(request));
            _converter = new DynamoRequestConverter(_request.ExpressionAttributeNames, _table.SerializerOptions);
        }

        //--- Methods ---
        public IDynamoTableGetItem<TRecord> Get<T>(Expression<Func<TRecord, T>> attribute) {
            _converter.AddProjection(attribute.Body);
            return this;
        }

        public async Task<TRecord?> ExecuteAsync(CancellationToken cancellationToken) {
            _request.ProjectionExpression = _converter.ConvertProjections();
            var response = await _table.DynamoClient.GetItemAsync(_request, cancellationToken);
            return response.IsItemSet
                ? _table.DeserializeItem<TRecord>(response.Item)
                : null;
        }
    }
}

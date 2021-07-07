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
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;

namespace LambdaSharp.DynamoDB.Native.Operations {

    public interface IDynamoTablePutItem<TRecord> where TRecord : class {
        /*
         * CONDITION EXPRESSION OPERATORS AND FUNCTIONS
         * https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Expressions.OperatorsAndFunctions.html
         *
         * condition-expression ::=
         *     operand comparator operand
         *     | operand BETWEEN operand AND operand
         *     | operand IN ( operand (',' operand (, ...) ))
         *     | function
         *     | condition AND condition
         *     | condition OR condition
         *     | NOT condition
         *     | ( condition )
         *
         * comparator ::=
         *     =
         *     | <>
         *     | <
         *     | <=
         *     | >
         *     | >=
         *
         * function ::=
         *     attribute_exists (path)
         *     | attribute_not_exists (path)
         *     | attribute_type (path, type)
         *     | begins_with (path, substr)
         *     | contains (path, operand)
         *     | size (path)
         */

        //--- Methods ---
        IDynamoTablePutItem<TRecord> WithCondition(Expression<Func<TRecord, bool>> condition);
        IDynamoTablePutItem<TRecord> Set(string key, AttributeValue value);
        Task<bool> ExecuteAsync(CancellationToken cancellationToken = default);
        Task<TRecord?> ExecuteReturnOldItemAsync(CancellationToken cancellationToken = default);

        //--- Default Methods ---
        IDynamoTablePutItem<TRecord> Set(string key, string value) => Set(key, new AttributeValue(value));
    }
}

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
using LambdaSharp.DynamoDB.Native.Internal;

namespace LambdaSharp.DynamoDB.Native.Query.Internal {

    internal abstract class ADynamoQueryClause<TRecord> :
        IDynamoQueryClause,
        IDynamoQueryClause<TRecord>,
        IDynamoQuerySortKeyConstraint,
        IDynamoQuerySortKeyConstraint<TRecord>
        where TRecord : class
    {

        //--- Constructors ---
        protected ADynamoQueryClause(string? indexName, string pkName, string skName, string pkValue, IEnumerable<Type> typeFilters) {
            PKName = pkName ?? throw new ArgumentNullException(nameof(pkName));
            SKName = skName ?? throw new ArgumentNullException(nameof(skName));
            PKValue = pkValue ?? throw new ArgumentNullException(nameof(pkValue));
            TypeFilters = typeFilters.ToList();
        }

        //--- Properties ---
        public string? IndexName { get; }
        public string PKName { get; }
        public string PKValue { get; }
        public string SKName { get; }
        public List<Type> TypeFilters { get; } = new List<Type>();

        //--- Abstract Methods ---
        public abstract string GetKeyConditionExpression(DynamoRequestConverter converter);

        //--- IDynamoQuerySelect Members ---
        IDynamoQueryClause IDynamoQuerySortKeyConstraint.WhereSKMatchesAny( )
            => new DynamoQuerySelectAny<TRecord>(IndexName, PKName, SKName, PKValue, TypeFilters);

        IDynamoQueryClause IDynamoQuerySortKeyConstraint.WhereSKEquals(string skValue)
            => new DynamoQuerySelectCompare<TRecord>(IndexName, PKName, SKName, PKValue, TypeFilters, DynamoQueryComparison.Equals, skValue);

        IDynamoQueryClause IDynamoQuerySortKeyConstraint.WhereSKIsGreaterThan(string skValue)
            => new DynamoQuerySelectCompare<TRecord>(IndexName, PKName, SKName, PKValue, TypeFilters, DynamoQueryComparison.GreaterThan, skValue);

        IDynamoQueryClause IDynamoQuerySortKeyConstraint.WhereSKIsGreaterThanOrEquals(string skValue)
            => new DynamoQuerySelectCompare<TRecord>(IndexName, PKName, SKName, PKValue, TypeFilters, DynamoQueryComparison.GreaterThanOrEquals, skValue);

        IDynamoQueryClause IDynamoQuerySortKeyConstraint.WhereSKIsLessThan(string skValue)
            => new DynamoQuerySelectCompare<TRecord>(IndexName, PKName, SKName, PKValue, TypeFilters, DynamoQueryComparison.LessThan, skValue);

        IDynamoQueryClause IDynamoQuerySortKeyConstraint.WhereSKIsLessThanOrEquals(string skValue)
            => new DynamoQuerySelectCompare<TRecord>(IndexName, PKName, SKName, PKValue, TypeFilters, DynamoQueryComparison.LessThanOrEquals, skValue);

        IDynamoQueryClause IDynamoQuerySortKeyConstraint.WhereSKIsBetween(string skLowerBound, string skUpperBound)
            => new DynamoQuerySelectRange<TRecord>(IndexName, PKName, SKName, PKValue, TypeFilters, skLowerBound, skUpperBound);

        IDynamoQueryClause IDynamoQuerySortKeyConstraint.WhereSKBeginsWith(string skValuePrefix)
            => new DynamoQuerySelectBeginsWith<TRecord>(IndexName, PKName, SKName, PKValue, TypeFilters, skValuePrefix);


        //--- IDynamoQueryClause Members ---
        IDynamoQueryClause IDynamoQueryClause.WithTypeFilter(Type type) {
            TypeFilters.Add(type ?? throw new ArgumentNullException(nameof(type)));
            return this;
        }

        //--- IDynamoQuerySelect<TRecord> Members ---
        IDynamoQueryClause<TRecord> IDynamoQuerySortKeyConstraint<TRecord>.WhereSKMatchesAny()
            => new DynamoQuerySelectAny<TRecord>(IndexName, PKName, SKName, PKValue, TypeFilters);

        IDynamoQueryClause<TRecord> IDynamoQuerySortKeyConstraint<TRecord>.WhereSKEquals(string skValue)
            => new DynamoQuerySelectCompare<TRecord>(IndexName, PKName, SKName, PKValue, TypeFilters, DynamoQueryComparison.Equals, skValue);

        IDynamoQueryClause<TRecord> IDynamoQuerySortKeyConstraint<TRecord>.WhereSKIsGreaterThan(string skValue)
            => new DynamoQuerySelectCompare<TRecord>(IndexName, PKName, SKName, PKValue, TypeFilters, DynamoQueryComparison.GreaterThan, skValue);

        IDynamoQueryClause<TRecord> IDynamoQuerySortKeyConstraint<TRecord>.WhereSKIsGreaterThanOrEquals(string skValue)
            => new DynamoQuerySelectCompare<TRecord>(IndexName, PKName, SKName, PKValue, TypeFilters, DynamoQueryComparison.GreaterThanOrEquals, skValue);

        IDynamoQueryClause<TRecord> IDynamoQuerySortKeyConstraint<TRecord>.WhereSKIsLessThan(string skValue)
            => new DynamoQuerySelectCompare<TRecord>(IndexName, PKName, SKName, PKValue, TypeFilters, DynamoQueryComparison.LessThan, skValue);

        IDynamoQueryClause<TRecord> IDynamoQuerySortKeyConstraint<TRecord>.WhereSKIsLessThanOrEquals(string skValue)
            => new DynamoQuerySelectCompare<TRecord>(IndexName, PKName, SKName, PKValue, TypeFilters, DynamoQueryComparison.LessThanOrEquals, skValue);

        IDynamoQueryClause<TRecord> IDynamoQuerySortKeyConstraint<TRecord>.WhereSKIsBetween(string skLowerBound, string skUpperBound)
            => new DynamoQuerySelectRange<TRecord>(IndexName, PKName, SKName, PKValue, TypeFilters, skLowerBound, skUpperBound);

        IDynamoQueryClause<TRecord> IDynamoQuerySortKeyConstraint<TRecord>.WhereSKBeginsWith(string skValuePrefix)
            => new DynamoQuerySelectBeginsWith<TRecord>(IndexName, PKName, SKName, PKValue, TypeFilters, skValuePrefix);
    }

    internal class DynamoQuerySelectAny<TRecord> : ADynamoQueryClause<TRecord> where TRecord : class {

        //--- Constructors ---
        public DynamoQuerySelectAny(string? indexName, string pkName, string skName, string pkValue, IEnumerable<Type> typeFilters)
            : base(indexName, pkName, skName, pkValue, typeFilters) { }

        //--- Methods ---
        public override string GetKeyConditionExpression(DynamoRequestConverter converter)
            => $"{converter.GetAttributeName(PKName)} = {converter.GetExpressionValueName(PKValue)}";
    }

    internal enum DynamoQueryComparison {
        Equals,
        GreaterThan,
        GreaterThanOrEquals,
        LessThan,
        LessThanOrEquals
    }

    internal class DynamoQuerySelectCompare<TRecord> : ADynamoQueryClause<TRecord> where TRecord : class {

        //--- Constructors ---
        public DynamoQuerySelectCompare(string? indexName, string pkName, string skName, string pkValue, IEnumerable<Type> typeFilters, DynamoQueryComparison sortKeyComparison, string sortKeyComparisonOperand)
            : base(indexName, pkName, skName, pkValue, typeFilters)
        {
            SortKeyComparison = sortKeyComparison;
            SortKeyComparisonOperand = sortKeyComparisonOperand;
        }

        //--- Properties ---
        public DynamoQueryComparison SortKeyComparison { get; }
        public string SortKeyComparisonOperand { get; }

        public string SortKeyComparisonOperator => SortKeyComparison switch {
            DynamoQueryComparison.Equals => "=",
            DynamoQueryComparison.GreaterThan => ">",
            DynamoQueryComparison.GreaterThanOrEquals => ">=",
            DynamoQueryComparison.LessThan => "<",
            DynamoQueryComparison.LessThanOrEquals => "<=",
            _ => throw new InvalidOperationException($"unknown sort key comparison: {SortKeyComparison}")
        };

        //--- Methods ---
        public override string GetKeyConditionExpression(DynamoRequestConverter converter)
            => $"{converter.GetAttributeName(PKName)} = {converter.GetExpressionValueName(PKValue)} AND {converter.GetAttributeName(SKName)} {SortKeyComparisonOperator} {converter.GetExpressionValueName(SortKeyComparisonOperand)}";
    }

    internal class DynamoQuerySelectRange<TRecord> : ADynamoQueryClause<TRecord> where TRecord : class {

        //--- Constructors ---
        public DynamoQuerySelectRange(string? indexName, string pkName, string skName, string pkValue, IEnumerable<Type> typeFilters, string sortKeyLowerBound, string sortKeyUpperBound)
            : base(indexName, pkName, skName, pkValue, typeFilters)
        {
            SortKeyLowerBound = sortKeyLowerBound ?? throw new ArgumentNullException(nameof(sortKeyLowerBound));
            SortKeyUpperBound = sortKeyUpperBound ?? throw new ArgumentNullException(nameof(sortKeyUpperBound));
        }

        //--- Properties ---
        public string SortKeyLowerBound { get; }
        public string SortKeyUpperBound { get; }

        //--- Methods ---
        public override string GetKeyConditionExpression(DynamoRequestConverter converter)
            => $"{converter.GetAttributeName(PKName)} = {converter.GetExpressionValueName(PKValue)} AND {converter.GetAttributeName(SKName)} BETWEEN {converter.GetExpressionValueName(SortKeyLowerBound)} AND  {converter.GetExpressionValueName(SortKeyUpperBound)}";
    }

    internal class DynamoQuerySelectBeginsWith<TRecord> : ADynamoQueryClause<TRecord> where TRecord : class {

        //--- Constructors ---
        public DynamoQuerySelectBeginsWith(string? indexName, string pkName, string skName, string pkValue, IEnumerable<Type> typeFilters, string sortKeyPrefix)
            : base(indexName, pkName, skName, pkValue, typeFilters)
            => SortKeyPrefix = sortKeyPrefix ?? throw new ArgumentNullException(nameof(sortKeyPrefix));

        //--- Properties ---
        public string SortKeyPrefix { get; }

        //--- Methods ---
        public override string GetKeyConditionExpression(DynamoRequestConverter converter)
            => $"{converter.GetAttributeName(PKName)} = {converter.GetExpressionValueName(PKValue)} AND begins_with({converter.GetAttributeName(SKName)}, {converter.GetExpressionValueName(SortKeyPrefix)})";
    }
}

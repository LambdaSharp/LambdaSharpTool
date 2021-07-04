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

namespace LambdaSharp.DynamoDB.Native.Internal {

    internal abstract class ADynamoQuerySelect<TRecord> : IDynamoQuerySelect, IDynamoQuerySelect<TRecord>
        where TRecord : class
    {

        //--- Constructors ---
        protected ADynamoQuerySelect(string? indexName, string partitionKeyName, string sortKeyName, string partitionKeyValue) {
            PartitionKeyName = partitionKeyName ?? throw new ArgumentNullException(nameof(partitionKeyName));
            SortKeyName = sortKeyName ?? throw new ArgumentNullException(nameof(sortKeyName));
            PartitionKeyValue = partitionKeyValue ?? throw new ArgumentNullException(nameof(partitionKeyValue));
        }

        //--- Properties ---
        public string? IndexName { get; }
        public string PartitionKeyName { get; }
        public string PartitionKeyValue { get; }
        public string SortKeyName { get; }

        //--- Abstract Methods ---
        public abstract string GetKeyConditionExpression(DynamoRequestConverter converter);

        //--- IDynamoQuerySelect Members ---
        IDynamoQuerySelect IDynamoQuerySelect.WhereSKMatchesAny( )
            => new DynamoQuerySelectAny<TRecord>(IndexName, PartitionKeyName, SortKeyName, PartitionKeyValue);

        IDynamoQuerySelect IDynamoQuerySelect.WhereSKEquals(string skValue)
            => new DynamoQuerySelectCompare<TRecord>(IndexName, PartitionKeyName, SortKeyName, PartitionKeyValue, DynamoQueryComparison.Equals, skValue);

        IDynamoQuerySelect IDynamoQuerySelect.WhereSKIsGreaterThan(string skValue)
            => new DynamoQuerySelectCompare<TRecord>(IndexName, PartitionKeyName, SortKeyName, PartitionKeyValue, DynamoQueryComparison.GreaterThan, skValue);

        IDynamoQuerySelect IDynamoQuerySelect.WhereSKIsGreaterThanOrEquals(string skValue)
            => new DynamoQuerySelectCompare<TRecord>(IndexName, PartitionKeyName, SortKeyName, PartitionKeyValue, DynamoQueryComparison.GreaterThanOrEquals, skValue);

        IDynamoQuerySelect IDynamoQuerySelect.WhereSKIsLessThan(string skValue)
            => new DynamoQuerySelectCompare<TRecord>(IndexName, PartitionKeyName, SortKeyName, PartitionKeyValue, DynamoQueryComparison.LessThan, skValue);

        IDynamoQuerySelect IDynamoQuerySelect.WhereSKIsLessThanOrEquals(string skValue)
            => new DynamoQuerySelectCompare<TRecord>(IndexName, PartitionKeyName, SortKeyName, PartitionKeyValue, DynamoQueryComparison.LessThanOrEquals, skValue);

        IDynamoQuerySelect IDynamoQuerySelect.WhereSKIsBetween(string skLowerBound, string skUpperBound)
            => new DynamoQuerySelectRange<TRecord>(IndexName, PartitionKeyName, SortKeyName, PartitionKeyValue, skLowerBound, skUpperBound);

        IDynamoQuerySelect IDynamoQuerySelect.WhereSKBeginsWith(string skValuePrefix)
            => new DynamoQuerySelectBeginsWith<TRecord>(IndexName, PartitionKeyName, SortKeyName, PartitionKeyValue, skValuePrefix);

        //--- IDynamoQuerySelect<TRecord> Members ---
        IDynamoQuerySelect<TRecord> IDynamoQuerySelect<TRecord>.WhereSKMatchesAny()
            => new DynamoQuerySelectAny<TRecord>(IndexName, PartitionKeyName, SortKeyName, PartitionKeyValue);

        IDynamoQuerySelect<TRecord> IDynamoQuerySelect<TRecord>.WhereSKEquals(string skValue)
            => new DynamoQuerySelectCompare<TRecord>(IndexName, PartitionKeyName, SortKeyName, PartitionKeyValue, DynamoQueryComparison.Equals, skValue);

        IDynamoQuerySelect<TRecord> IDynamoQuerySelect<TRecord>.WhereSKIsGreaterThan(string skValue)
            => new DynamoQuerySelectCompare<TRecord>(IndexName, PartitionKeyName, SortKeyName, PartitionKeyValue, DynamoQueryComparison.GreaterThan, skValue);

        IDynamoQuerySelect<TRecord> IDynamoQuerySelect<TRecord>.WhereSKIsGreaterThanOrEquals(string skValue)
            => new DynamoQuerySelectCompare<TRecord>(IndexName, PartitionKeyName, SortKeyName, PartitionKeyValue, DynamoQueryComparison.GreaterThanOrEquals, skValue);

        IDynamoQuerySelect<TRecord> IDynamoQuerySelect<TRecord>.WhereSKIsLessThan(string skValue)
            => new DynamoQuerySelectCompare<TRecord>(IndexName, PartitionKeyName, SortKeyName, PartitionKeyValue, DynamoQueryComparison.LessThan, skValue);

        IDynamoQuerySelect<TRecord> IDynamoQuerySelect<TRecord>.WhereSKIsLessThanOrEquals(string skValue)
            => new DynamoQuerySelectCompare<TRecord>(IndexName, PartitionKeyName, SortKeyName, PartitionKeyValue, DynamoQueryComparison.LessThanOrEquals, skValue);

        IDynamoQuerySelect<TRecord> IDynamoQuerySelect<TRecord>.WhereSKIsBetween(string skLowerBound, string skUpperBound)
            => new DynamoQuerySelectRange<TRecord>(IndexName, PartitionKeyName, SortKeyName, PartitionKeyValue, skLowerBound, skUpperBound);

        IDynamoQuerySelect<TRecord> IDynamoQuerySelect<TRecord>.WhereSKBeginsWith(string skValuePrefix)
            => new DynamoQuerySelectBeginsWith<TRecord>(IndexName, PartitionKeyName, SortKeyName, PartitionKeyValue, skValuePrefix);
    }

    internal class DynamoQuerySelectAny<TRecord> : ADynamoQuerySelect<TRecord> where TRecord : class {

        //--- Constructors ---
        public DynamoQuerySelectAny(string? indexName, string partitionKeyName, string sortKeyName, string partitionKeyValue)
            : base(indexName, partitionKeyName, sortKeyName, partitionKeyValue) { }

        //--- Methods ---
        public override string GetKeyConditionExpression(DynamoRequestConverter converter)
            => $"{converter.GetAttributeName(PartitionKeyName)} = {converter.GetExpressionValueName(PartitionKeyValue)}";
    }

    internal enum DynamoQueryComparison {
        Equals,
        GreaterThan,
        GreaterThanOrEquals,
        LessThan,
        LessThanOrEquals
    }

    internal class DynamoQuerySelectCompare<TRecord> : ADynamoQuerySelect<TRecord> where TRecord : class {

        //--- Constructors ---
        public DynamoQuerySelectCompare(string? indexName, string partitionKeyName, string sortKeyName, string partitionKeyValue, DynamoQueryComparison sortKeyComparison, string sortKeyComparisonOperand)
            : base(indexName, partitionKeyName, sortKeyName, partitionKeyValue)
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
            => $"{converter.GetAttributeName(PartitionKeyName)} = {converter.GetExpressionValueName(PartitionKeyValue)} AND {converter.GetAttributeName(SortKeyName)} {SortKeyComparisonOperator} {converter.GetExpressionValueName(SortKeyComparisonOperand)}";
    }

    internal class DynamoQuerySelectRange<TRecord> : ADynamoQuerySelect<TRecord> where TRecord : class {

        //--- Constructors ---
        public DynamoQuerySelectRange(string? indexName, string partitionKeyName, string sortKeyName, string partitionKeyValue, string sortKeyLowerBound, string sortKeyUpperBound)
            : base(indexName, partitionKeyName, sortKeyName, partitionKeyValue)
        {
            SortKeyLowerBound = sortKeyLowerBound ?? throw new ArgumentNullException(nameof(sortKeyLowerBound));
            SortKeyUpperBound = sortKeyUpperBound ?? throw new ArgumentNullException(nameof(sortKeyUpperBound));
        }

        //--- Properties ---
        public string SortKeyLowerBound { get; }
        public string SortKeyUpperBound { get; }

        //--- Methods ---
        public override string GetKeyConditionExpression(DynamoRequestConverter converter)
            => $"{converter.GetAttributeName(PartitionKeyName)} = {converter.GetExpressionValueName(PartitionKeyValue)} AND {converter.GetAttributeName(SortKeyName)} BETWEEN {converter.GetExpressionValueName(SortKeyLowerBound)} AND  {converter.GetExpressionValueName(SortKeyUpperBound)}";
    }

    internal class DynamoQuerySelectBeginsWith<TRecord> : ADynamoQuerySelect<TRecord> where TRecord : class {

        //--- Constructors ---
        public DynamoQuerySelectBeginsWith(string? indexName, string partitionKeyName, string sortKeyName, string partitionKeyValue, string sortKeyPrefix)
            : base(indexName, partitionKeyName, sortKeyName, partitionKeyValue)
            => SortKeyPrefix = sortKeyPrefix ?? throw new ArgumentNullException(nameof(sortKeyPrefix));

        //--- Properties ---
        public string SortKeyPrefix { get; }

        //--- Methods ---
        public override string GetKeyConditionExpression(DynamoRequestConverter converter)
            => $"{converter.GetAttributeName(PartitionKeyName)} = {converter.GetExpressionValueName(PartitionKeyValue)} AND begins_with({converter.GetAttributeName(SortKeyName)}, {converter.GetExpressionValueName(SortKeyPrefix)})";
    }
}

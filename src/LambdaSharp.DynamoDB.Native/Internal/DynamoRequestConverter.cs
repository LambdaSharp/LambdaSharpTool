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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using Amazon.DynamoDBv2.Model;
using LambdaSharp.DynamoDB.Serialization;

namespace LambdaSharp.DynamoDB.Native.Internal {

    internal partial class DynamoRequestConverter {

        //--- Types ---

        /* PRECEDENCE
         *
         * 0: attribute literal
         * 1: = <> < <= > >=
         * 2: IN
         * 3: BETWEEN
         * 4: attribute_exists attribute_not_exists begins_with contains
         * 5: Parentheses
         * 6: NOT
         * 7: AND
         * 8: OR
         */
        public enum Precedence {
            Undefined,
            Atomic,
            ScalarAddSubtract,
            ScalarComparison,
            InOperator,
            BetweenOperator,
            NotOperator,
            AndOperator,
            OrOperator
        }

        //--- Class Fields ---
        private static readonly string[] _reservedWords = new[] {
            #region *** Long list of reserved keywords ***
            "ABORT", "ABSOLUTE", "ACTION", "ADD", "AFTER", "AGENT", "AGGREGATE", "ALL", "ALLOCATE", "ALTER", "ANALYZE", "AND", "ANY", "ARCHIVE", "ARE", "ARRAY", "AS", "ASC", "ASCII", "ASENSITIVE",
            "ASSERTION", "ASYMMETRIC", "AT", "ATOMIC", "ATTACH", "ATTRIBUTE", "AUTH", "AUTHORIZATION", "AUTHORIZE", "AUTO", "AVG", "BACK", "BACKUP", "BASE", "BATCH", "BEFORE", "BEGIN", "BETWEEN",
            "BIGINT", "BINARY", "BIT", "BLOB", "BLOCK", "BOOLEAN", "BOTH", "BREADTH", "BUCKET", "BULK", "BY", "BYTE", "CALL", "CALLED", "CALLING", "CAPACITY", "CASCADE", "CASCADED", "CASE", "CAST",
            "CATALOG", "CHAR", "CHARACTER", "CHECK", "CLASS", "CLOB", "CLOSE", "CLUSTER", "CLUSTERED", "CLUSTERING", "CLUSTERS", "COALESCE", "COLLATE", "COLLATION", "COLLECTION", "COLUMN", "COLUMNS",
            "COMBINE", "COMMENT", "COMMIT", "COMPACT", "COMPILE", "COMPRESS", "CONDITION", "CONFLICT", "CONNECT", "CONNECTION", "CONSISTENCY", "CONSISTENT", "CONSTRAINT", "CONSTRAINTS", "CONSTRUCTOR",
            "CONSUMED",  "CONTINUE", "CONVERT", "COPY", "CORRESPONDING", "COUNT", "COUNTER", "CREATE", "CROSS", "CUBE", "CURRENT", "CURSOR", "CYCLE", "DATA", "DATABASE", "DATE", "DATETIME", "DAY",
            "DEALLOCATE", "DEC", "DECIMAL", "DECLARE", "DEFAULT", "DEFERRABLE", "DEFERRED", "DEFINE", "DEFINED", "DEFINITION", "DELETE", "DELIMITED", "DEPTH", "DEREF", "DESC", "DESCRIBE", "DESCRIPTOR",
            "DETACH", "DETERMINISTIC", "DIAGNOSTICS", "DIRECTORIES", "DISABLE", "DISCONNECT", "DISTINCT", "DISTRIBUTE", "DO", "DOMAIN", "DOUBLE", "DROP", "DUMP", "DURATION", "DYNAMIC", "EACH", "ELEMENT",
            "ELSE", "ELSEIF", "EMPTY", "ENABLE", "END", "EQUAL", "EQUALS", "ERROR", "ESCAPE", "ESCAPED", "EVAL", "EVALUATE", "EXCEEDED", "EXCEPT", "EXCEPTION", "EXCEPTIONS", "EXCLUSIVE", "EXEC",
            "EXECUTE", "EXISTS", "EXIT", "EXPLAIN", "EXPLODE", "EXPORT", "EXPRESSION", "EXTENDED", "EXTERNAL", "EXTRACT", "FAIL", "FALSE", "FAMILY", "FETCH", "FIELDS", "FILE", "FILTER", "FILTERING",
            "FINAL", "FINISH", "FIRST", "FIXED", "FLATTERN", "FLOAT", "FOR", "FORCE", "FOREIGN", "FORMAT", "FORWARD", "FOUND", "FREE", "FROM", "FULL", "FUNCTION", "FUNCTIONS", "GENERAL", "GENERATE",
            "GET", "GLOB", "GLOBAL", "GO", "GOTO", "GRANT", "GREATER", "GROUP", "GROUPING", "HANDLER", "HASH", "HAVE", "HAVING", "HEAP", "HIDDEN", "HOLD", "HOUR", "IDENTIFIED", "IDENTITY", "IF", "IGNORE",
            "IMMEDIATE", "IMPORT", "IN", "INCLUDING", "INCLUSIVE", "INCREMENT", "INCREMENTAL", "INDEX", "INDEXED", "INDEXES", "INDICATOR", "INFINITE", "INITIALLY", "INLINE", "INNER", "INNTER", "INOUT",
            "INPUT", "INSENSITIVE", "INSERT", "INSTEAD", "INT", "INTEGER", "INTERSECT", "INTERVAL", "INTO", "INVALIDATE", "IS", "ISOLATION", "ITEM", "ITEMS", "ITERATE", "JOIN", "KEY", "KEYS", "LAG",
            "LANGUAGE", "LARGE", "LAST", "LATERAL", "LEAD", "LEADING", "LEAVE", "LEFT", "LENGTH", "LESS", "LEVEL", "LIKE", "LIMIT", "LIMITED", "LINES", "LIST", "LOAD", "LOCAL", "LOCALTIME",
            "LOCALTIMESTAMP", "LOCATION", "LOCATOR", "LOCK", "LOCKS", "LOG", "LOGED", "LONG", "LOOP", "LOWER", "MAP", "MATCH", "MATERIALIZED", "MAX", "MAXLEN", "MEMBER", "MERGE", "METHOD", "METRICS",
            "MIN", "MINUS", "MINUTE", "MISSING", "MOD", "MODE", "MODIFIES", "MODIFY", "MODULE", "MONTH", "MULTI", "MULTISET", "NAME", "NAMES", "NATIONAL", "NATURAL", "NCHAR", "NCLOB", "NEW", "NEXT",
            "NO", "NONE", "NOT", "NULL", "NULLIF", "NUMBER", "NUMERIC", "OBJECT", "OF", "OFFLINE", "OFFSET", "OLD", "ON", "ONLINE", "ONLY", "OPAQUE", "OPEN", "OPERATOR", "OPTION", "OR", "ORDER",
            "ORDINALITY", "OTHER", "OTHERS", "OUT", "OUTER", "OUTPUT", "OVER", "OVERLAPS", "OVERRIDE", "OWNER", "PAD", "PARALLEL", "PARAMETER", "PARAMETERS", "PARTIAL", "PARTITION", "PARTITIONED",
            "PARTITIONS", "PATH", "PERCENT", "PERCENTILE", "PERMISSION", "PERMISSIONS", "PIPE", "PIPELINED", "PLAN", "POOL", "POSITION", "PRECISION", "PREPARE", "PRESERVE", "PRIMARY", "PRIOR",
            "PRIVATE", "PRIVILEGES", "PROCEDURE", "PROCESSED", "PROJECT", "PROJECTION", "PROPERTY", "PROVISIONING", "PUBLIC", "PUT", "QUERY", "QUIT", "QUORUM", "RAISE", "RANDOM", "RANGE", "RANK", "RAW",
            "READ", "READS", "REAL", "REBUILD", "RECORD", "RECURSIVE", "REDUCE", "REF", "REFERENCE", "REFERENCES", "REFERENCING", "REGEXP", "REGION", "REINDEX", "RELATIVE", "RELEASE", "REMAINDER", "RENAME",
            "REPEAT", "REPLACE", "REQUEST", "RESET", "RESIGNAL", "RESOURCE", "RESPONSE", "RESTORE", "RESTRICT", "RESULT", "RETURN", "RETURNING", "RETURNS", "REVERSE", "REVOKE", "RIGHT", "ROLE", "ROLES",
            "ROLLBACK", "ROLLUP", "ROUTINE", "ROW", "ROWS", "RULE", "RULES", "SAMPLE", "SATISFIES", "SAVE", "SAVEPOINT", "SCAN", "SCHEMA", "SCOPE", "SCROLL", "SEARCH", "SECOND", "SECTION", "SEGMENT",
            "SEGMENTS", "SELECT", "SELF", "SEMI", "SENSITIVE", "SEPARATE", "SEQUENCE", "SERIALIZABLE", "SESSION", "SET", "SETS", "SHARD", "SHARE", "SHARED", "SHORT", "SHOW", "SIGNAL", "SIMILAR", "SIZE",
            "SKEWED", "SMALLINT", "SNAPSHOT", "SOME", "SOURCE", "SPACE", "SPACES", "SPARSE", "SPECIFIC", "SPECIFICTYPE", "SPLIT", "SQL", "SQLCODE", "SQLERROR", "SQLEXCEPTION", "SQLSTATE", "SQLWARNING",
            "START", "STATE", "STATIC", "STATUS", "STORAGE", "STORE", "STORED", "STREAM", "STRING", "STRUCT", "STYLE", "SUB", "SUBMULTISET", "SUBPARTITION", "SUBSTRING", "SUBTYPE", "SUM", "SUPER",
            "SYMMETRIC", "SYNONYM", "SYSTEM", "TABLE", "TABLESAMPLE", "TEMP", "TEMPORARY", "TERMINATED", "TEXT", "THAN", "THEN", "THROUGHPUT", "TIME", "TIMESTAMP", "TIMEZONE", "TINYINT", "TO", "TOKEN",
            "TOTAL", "TOUCH", "TRAILING", "TRANSACTION", "TRANSFORM", "TRANSLATE", "TRANSLATION", "TREAT", "TRIGGER", "TRIM", "TRUE", "TRUNCATE", "TTL", "TUPLE", "TYPE", "UNDER", "UNDO", "UNION", "UNIQUE",
            "UNIT", "UNKNOWN", "UNLOGGED", "UNNEST", "UNPROCESSED", "UNSIGNED", "UNTIL", "UPDATE", "UPPER", "URL", "USAGE", "USE", "USER", "USERS", "USING", "UUID", "VACUUM", "VALUE", "VALUED", "VALUES",
            "VARCHAR", "VARIABLE", "VARIANCE", "VARINT", "VARYING", "VIEW", "VIEWS", "VIRTUAL", "VOID", "WAIT", "WHEN", "WHENEVER", "WHERE", "WHILE", "WINDOW", "WITH", "WITHIN", "WITHOUT", "WORK", "WRAPPED",
            "WRITE", "YEAR", "ZONE"
            #endregion
        };

        //--- Class Methods ---
        public static bool DoesAttributeNameRequireEncoding(string name) =>
            !char.IsLetter(name[0])
            || name.Contains('.')
            || Array.BinarySearch(_reservedWords, name.ToUpperInvariant()) >= 0;

        //--- Constructors ---
        public DynamoRequestConverter(Dictionary<string, string> expressionAttributes, DynamoSerializerOptions serializerOptions) {
            ExpressionAttributes = expressionAttributes ?? throw new ArgumentNullException(nameof(expressionAttributes));
            SerializerOptions = serializerOptions ?? throw new ArgumentNullException(nameof(serializerOptions));
        }

        public DynamoRequestConverter(Dictionary<string, string> expressionAttributes, Dictionary<string, AttributeValue> expressionValues, DynamoSerializerOptions serializerOptions) {
            ExpressionAttributes = expressionAttributes ?? throw new ArgumentNullException(nameof(expressionAttributes));
            ExpressionValues = expressionValues ?? throw new ArgumentNullException(nameof(expressionValues));
            SerializerOptions = serializerOptions ?? throw new ArgumentNullException(nameof(serializerOptions));
        }

        //--- Properties ---
        public Dictionary<string, string> ExpressionAttributes { get; }
        public Dictionary<string, AttributeValue>? ExpressionValues { get; }
        public DynamoSerializerOptions SerializerOptions { get; }
        public Dictionary<string, Type> ExpectedTypes { get; } = new Dictionary<string, Type>();
        private List<(string Expression, DynamoRequestConverter.Precedence Precedence)> Conditions { get; } = new List<(string Expression, Precedence Precedence)>();
        private HashSet<string> ProjectionAttributes { get; } = new HashSet<string>();

        //--- Methods ---
        public void AddExpectedType(Type type) {
            var typeFullName = type.FullName ?? throw new ArgumentException("type name is <null>", nameof(type));
            ExpectedTypes[typeFullName] = type;
        }

        public void AddCondition(Expression condition)
            => Conditions.Add(ParseCondition(condition));

        public void AddProjection(Expression attribute)
            => ProjectionAttributes.Add(ParseAttributePath(attribute));

        public void AddProjection(string attributeName)
            => ProjectionAttributes.Add(GetAttributeName(attributeName));

        public string GetExpressionValueName(object? value)
            => GetExpressionValueName(DynamoSerializer.Serialize(value, SerializerOptions) ?? new AttributeValue {
                NULL = true
            });

        public string GetExpressionValueName(bool value)
            => GetExpressionValueName(new AttributeValue {
                BOOL = value
            });

        public string GetExpressionValueName(string value)
            => GetExpressionValueName(new AttributeValue {
                S = value
            });

        public string GetExpressionValueName(int value)
            => GetExpressionValueName(new AttributeValue {
                N = value.ToString(CultureInfo.InvariantCulture)
            });

        public string GetExpressionValueName(long value)
            => GetExpressionValueName(new AttributeValue {
                N = value.ToString(CultureInfo.InvariantCulture)
            });

        public string GetExpressionValueName(double value)
            => GetExpressionValueName(new AttributeValue {
                N = value.ToString(CultureInfo.InvariantCulture)
            });

        public string GetExpressionValueName(decimal value)
            => GetExpressionValueName(new AttributeValue {
                N = value.ToString(CultureInfo.InvariantCulture)
            });

        public string GetExpressionValueName(AttributeValue attributeValue) {
            if(ExpressionValues is null) {
                throw new InvalidOperationException("instance was initialized without expression values");
            }

            // check if this attribute path is new or already exists
            string? valueName = null;
            if(attributeValue.IsBOOLSet) {
                valueName = ExpressionValues
                    .SingleOrDefault(expressionValue => expressionValue.Value.BOOL == attributeValue.BOOL)
                    .Key;
            } else if(!(attributeValue.N is null)) {
                valueName = ExpressionValues
                    .SingleOrDefault(expressionValue => expressionValue.Value.N == attributeValue.N)
                    .Key;
            } else if(!(attributeValue.S is null)) {
                valueName = ExpressionValues
                    .SingleOrDefault(expressionValue => expressionValue.Value.S == attributeValue.S)
                    .Key;
            }

            // add value if it's not present yet
            if(valueName is null) {
                valueName = ":v_" + (ExpressionValues.Count + 1);
                ExpressionValues.Add(valueName, attributeValue);
            }
            return valueName;
        }

        public (string Expression, Precedence Precedence) Combine(string operation, Precedence precedence, (string Expression, Precedence Precedence) left, (string Expression, Precedence Precedence) right) {
            if((precedence >= left.Precedence) && (precedence >= right.Precedence)) {
                return (Expression: $"{left.Expression} {operation} {right.Expression}", Precedence: precedence);
            } else if(precedence >= left.Precedence) {
                return (Expression: $"{left.Expression} {operation} ({right.Expression})", Precedence: precedence);
            } else if(precedence >= right.Precedence) {
                return (Expression: $"({left.Expression}) {operation} {right.Expression}", Precedence: precedence);
            } else {
                return (Expression: $"({left.Expression}) {operation} ({right.Expression})", Precedence: precedence);
            }
        }

        public (string Expression, Precedence Precedence) Prefix(string operation, Precedence precedence, (string Expression, Precedence Precedence) inner) {
            if(precedence > inner.Precedence) {
                return (Expression: $"{operation} {inner.Expression}", Precedence: precedence);
            } else {
                return (Expression: $"{operation} ({inner.Expression})", Precedence: precedence);
            }
        }

        public string? ConvertConditions(DynamoTableOptions options) {

            // combine type conditions with OR operator
            string? typeConditionsAccumulator = null;
            if(ExpectedTypes.Any()) {
                var firstExpectedType = ExpectedTypes.Values.First();
                typeConditionsAccumulator = LiftToTypeConditionExpression(firstExpectedType);
                foreach(var expectedType in ExpectedTypes.Values.Skip(1)) {
                    typeConditionsAccumulator = Combine(
                        "OR",
                        Precedence.OrOperator,
                        (typeConditionsAccumulator, Precedence.OrOperator),
                        (Expression: LiftToTypeConditionExpression(expectedType), Precedence: Precedence.OrOperator)
                    ).Expression;
                }
            }

            // combine conditions with AND operator
            string? conditionsAccumulator = null;
            if(Conditions.Any()) {
                conditionsAccumulator = Conditions.First().Expression;
                foreach(var condition in Conditions.Skip(1)) {
                    conditionsAccumulator = Combine("AND", Precedence.AndOperator, (conditionsAccumulator, Precedence.AndOperator), condition).Expression;
                }
            }

            // combine both conditions
            if((typeConditionsAccumulator is null) && (conditionsAccumulator is null)) {
                return null;
            }
            if(typeConditionsAccumulator is null) {
                return conditionsAccumulator;
            }
            if(conditionsAccumulator is null) {
                return typeConditionsAccumulator;
            }
            return Combine("AND", Precedence.AndOperator, (conditionsAccumulator, Precedence.AndOperator), (typeConditionsAccumulator, Precedence.OrOperator)).Expression;

            // local functions
            string LiftToTypeConditionExpression(Type expectedType)
                => $"{GetAttributeName("_t")} = {GetExpressionValueName(options.GetShortTypeName(expectedType))}";
        }

        public string? ConvertProjections()
            => ProjectionAttributes.Any()
                ? string.Join(",", ProjectionAttributes)
                : null;

        public string GetAttributeName(string attributeName) {

            // check if the attribute name is reserved
            if(DoesAttributeNameRequireEncoding(attributeName)) {

                // check if this attribute path is new or already exists
                var existingAttributeName = ExpressionAttributes
                    .SingleOrDefault(expressionAttribute => expressionAttribute.Value == attributeName)
                    .Key;

                // attribute value is new, add it to the collection with a new name
                if(existingAttributeName is null) {
                    existingAttributeName = "#a_" + (ExpressionAttributes.Count + 1);
                    ExpressionAttributes.Add(existingAttributeName, attributeName);
                }

                // subsitute attribute name with its placeholder
                attributeName = existingAttributeName;
            }
            return attributeName;
        }
    }
}

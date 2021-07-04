> TODO: move docs to the right place

> TODO: rename `partitionKeyValuePattern` to `partitionKeyValueFormat` or `pkValueFormat`

> TODO: Ability to designated a property to hold the "_m" (modified) date-timestamp

> TODO: Test deserialization of custom type derived from `IList<Foo>`

> TODO: Add `ExecuteTransaction` operation

> TODO: Add `Scan()` operation

> TODO: Add link to serialization readme

> TODO: Look for all `/*` comments to see if they should be pulled into the readme

> TODO: Better name for `QueryMixed()` and `BatchGetMixed()`?

> TODO: test if a `HashSet<string>` property could always be initialized to an empty site and round-tripped; it would make logic much simpler!

> TODO: enum type are treated as string!

> TODO: test `GetItem<>` when item doesn't exist

> TODO: `TransactGetItems()` vs. `BatchGetItem()`: as is plural, the other is not

> TODO: extension method to write more than 25 items in a batch

> TODO: `GetAttributePath` should fail on record

> TODO: the current `Query(keys)` mechanism is too complicated; it requires the dev to know the access pattern, which is not good; instead it should be:
> * `_table.Query(new MySubRecord.GetSubRecordsBelongingTo(parentRecord))`
> * `_table.Query(new CustomerRecord.AllCustomers())`

> TODO: only `PutItem()` operations need secondary keys

> TODO: pre-defined key projections that are done by `PutItem()` and `UpdateItem()` when needed: `[DynamoProjectedAttribute("GS1PK", "ORDER#{OrderId}")]`
> * register: `AddAttributeProjection("GS1PK", record => $"CUSTOMER#{record.CustomerId}")`
> * implicitly registered: `AddAttributeProjection("_t", record => record.GetType().FullName")`
> * implicitly registered: `AddAttributeProjection("_m", record => DateTimeOffset.UtcNow")`
```csharp
new DynamoTableOptions {
    ExpectedTypeNamespace = "Foo",

    Model = {
        new() {
            Type = typeof(OrderRecord),
            Attributes = {
                ["GS1PK"] = record => $"ORDER#{record.OrderId}"),
                ["GSI1SK"] = record => $"INFO"
            }
        },
        new() {
            Type = typeof(OrderItemRecord),
            Attributes = {
                ["GS1PK"] = record => $"ORDER#{record.OrderId}"),
                ["GSI1SK"] = record => $"ITEM#{record.ItemId}"
            }
        }
    }
}
```

> TODO: register in table options what types to expect so we can always properly deserialize

> TODO: should we use the `_t` attribute to error out when deserializing the wrong row type?


```csharp
IDynamoTableQuery WhereSKEquals(string skValue);
IDynamoTableQuery WhereSKBeginsWith(string skValuePrefix);
IDynamoTableQuery WhereSKIsGreaterThan(string skValue);
IDynamoTableQuery WhereSKIsGreaterThanOrEquals(string skValue);
IDynamoTableQuery WhereSKIsLessThan(string skValue);
IDynamoTableQuery WhereSKIsLessThanOrEquals(string skValue);
IDynamoTableQuery WhereSKIsBetween(string skLowValue, string skHighValue);


DynamoQueryPattern(string partitionKeyValuePattern, params string[] values); // use main index and (PK,SK) as primary key
DynamoQueryPattern(string indexName, string partitionKeyName, string sortKeyName, string partitionKeyValuePattern, params string[] values);


ADynamoQueryPattern MakeCustomerAndOrdersQueryPattern(string customerUsername) =>
    new DynamoQueryPattern(CUSTOMER_PK_PATTERN, customerUsername);


ADynamoQueryPattern MakeCustomerAndOrdersQueryPattern(string customerUsername) =>
    new DynamoQueryPattern(indexName: "GSI1", partitionKeyName: "GSI1PK", sortKeyName: "GSI1SK", CUSTOMER_PK_PATTERN, customerUsername)
        .Where(sk => sk.BeginsWith("foo"));



const string CUSTOMER_PK_PATTERN = "CUSTOMER#{0}";
DynamoPrimaryKey MakeCustomerAndOrdersQueryPattern(string customerUsername) => new DynamoPrimaryKey(CUSTOMER_PK_PATTERN, "<NOT-USED>", customerUsername);




Table.QueryMixed(DataModel.MakeCustomerAndOrdersQueryPattern(customerUsername), limit: 11, scanIndexForward: false)
    .WithTypeFilter<CustomerRecord>()
    .WithTypeFilter<OrderRecord>()
    .ExecuteAsync(cancellationToken: cancellationToken);
```

# LambdaSharp.DynamoDB.Native

## Data Mapping

### TODO (missing alternative for NULL and L)

> TODO: IList<T> handling in serialization

> TODO: enum handling in serialization

> TODO: equivalence rules for SET update operations (T, IDictionary<string, T>, IList<T>, ISet<T>)

> TODO: list all valid literal expressions (`"abc"`, `123`, `true`, `new string[0]`, `new[] { "hello" }`)

|DynamoDB Data Type |Default .NET Type                      |Alternative .NET Type                  |
|-------------------|---------------------------------------|---------------------------------------|
|`B` (Binary)       |`byte[]`                               |N/A
|`BOOL` (Boolean)   |`bool`                                 |`bool?`
|`BS` (Binary Set)  |`HashSet<byte[]>`                      |implementations of `ISet<byte[]>`
|`L` (List)         |`List<object>`                         |implementations of `IList<T>` where `T` is one of `object`, `bool`, `string`, `byte[]`, ...
|`M` (Map)          |`Dictionary<string, object>`           |implementations of `IDictionary<string, T>` where `T` is one of  `object`, `bool`, `string`, `byte[]`, ...; or any concrete, constructor-less class
|`N` (Number)       |`double`                               |`int`, `long`, `decimal`, `int?`, `long?`, `double?`, `decimal?`
|`NS` (Number Set)  |`HashSet<double>`                      |implementations of `ISet<int>`, `ISet<long>`, `ISet<double>`, or `ISet<decimal>`
|`NULL` (Null)      |N/A (`null` if true, error otherwise)  |any concrete class
|`S` (String)       |`string`                               |N/A
|`SS` (String Set)  |`HashSet<string>`                      |implementations of `ISet<string>`

### Attribute to .NET conversion without type hints

|DynamoDB Data Type |.NET Data Type |
|-------------------|---------------|
|`B` (Binary)       |`byte[]`
|`BOOL` (Boolean)   |`bool`
|`BS` (Binary Set)  |`HashSet<byte[]>`
|`L` (List)         |`List<object>`
|`M` (Map)          |`Dictionary<string, object>`
|`N` (Number)       |`double`
|`NS` (Number Set)  |`HashSet<double>`
|`NULL` (Null)      |`null`
|`S` (String)       |`string`
|`SS` (String Set)  |`HashSet<string>`


### Attribute to .NET conversion with type hints

|.NET Data Type Hint            |DynamoDB Value     |
|-------------------------------|-------------------|
|`bool`                         |`BOOL` (Boolean)   |
|`byte[]`                       |`B` (Binary)       |
|`HashSet<byte[]>`              |`BS` (Binary Set)  |
|`List<object>`                 |`L` (List)         |
|`Dictionary<string, object>`   |`M` (Map)          |
|`double`                       |`N` (Number)       |
|`HashSet<double>`              |`NS` (Number Set)  |
|`null`                         |`NULL` (Null)      |
|`string`                       |`S` (String)       |
|`HashSet<string>`              |`SS` (String Set)  |


|DynamoDB Data Type |.NET Declared Type             |.NET Actual Type               |
|-------------------|-------------------------------|-------------------------------|
|`B` (Binary)       |`byte[]`                       |`byte[]`                       |
|`B` (Binary)       |N/A                            |`byte[]`                       |
|`BOOL` (Boolean)   |`bool`                         |`bool`                         |
|`BOOL` (Boolean)   |`bool?`                        |`bool`                         |
|`BOOL` (Boolean)   |N/A                            |`bool`                         |
|`BS` (Binary Set)  |`HashSet<byte[]>`              |`HashSet<byte[]>`              |
|`BS` (Binary Set)  |`ISet<byte[]>`                 |`HashSet<byte[]>`              |
|`L` (List)         |`List<object>`                 |`List<object>`                 |
|`M` (Map)          |`Dictionary<string, object>`   |`Dictionary<string, object>`   |
|`N` (Number)       |`double`                       |`double`                       |
|`NS` (Number Set)  |`HashSet<double>`              |`HashSet<double>`              |
|`NULL` (Null)      |`null`                         |`null`                         |
|`S` (String)       |`string`                       |`string`                       |
|`SS` (String Set)  |`HashSet<string>`              |`HashSet<string>`              |


## Root Query Clause

```csharp
/*
 * KEY EXPRESSION OPERATORS AND FUNCTIONS
 * https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Query.html#Query.KeyConditionExpressions
 *
 * sort-key-condition ::=
 *     operand comparator operand
 *     | operand BETWEEN operand AND operand
 *     | function
 *
 * comparator ::=
 *     =
 *     | <
 *     | <=
 *     | >
 *     | >=
 *
 * function ::=
 *     begins_with (path, substr)
 */
```


## Condition Expressions

### Binary Operators: <, >, <=, >=, =, <>, AND, OR

* `scalar < scalar`
* `scalar > scalar`
* `scalar <= scalar`
* `scalar >= scalar`
* `scalar = scalar`
* `scalar <> scalar`
* `boolean && boolean`
* `boolean || boolean`

### Unary Operator: NOT

* `!boolean`

### Ternary Operator: a BETWEEN b AND c

* `DynamoCondition.Between(scalar, scalar, scalar)`

### a IN ([b])

* `DynamoCondition.In(scalar, new[] { scalar+ })`

### Condition Function: attribute_exists (path)

* `DynamoCondition.Exists(path)`
* `(DynamoPrimaryKey<TRecord> path).Exists()`

### Condition Function: attribute_not_exists (path)

* `DynamoCondition.DoesNotExist(path)`

### Condition Function: attribute_type (path, type)

* `DynamoCondition.HasType(path, string_literal)`

### Condition Function: begins_with (path, prefix)

* `DynamoCondition.BeginsWith(path, scalar_expression)`
* `(String path).StartsWith(scalar_expression)`

### Condition Function: contains (path, operand)

* `DynamoCondition.Contains(path, scalar_expression)`
* `Enumerable.Contains(path, scalar_expression)`
* `(IList path).Contains(scalar_expression)`

### Condition Function: size (path)

* `DynamoCondition.Size(path)`
* `Enumerable.Count(path, scalar_expression)`
* `(ICollection path).Count`
* `(Array path).Length`

## SET Expressions

### Binary Operators: +, -

* `scalar + scalar`
* `scalar - scalar`

### SET Function: if_not_exists (path, value)

* `DynamoSet.IfNotExists(path, scalar)`

### SET Function: list_append (operand, operand)

* `DynamoSet.ListAppend(path, scalar)`

/* SET EXPRESSION
    * https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Expressions.UpdateExpressions.html#Expressions.UpdateExpressions.SET

```
SET-ACTION ::=
    SET-ACTION-LHS '=' SET-ACTION-RHS

SET-ACTION-LHS ::= ATTRIBUTE-PATH

ATTRIBUTE-PATH ::=
    parameter
    |  ATTRIBUTE-PATH '.' member-name
    |  ATTRIBUTE-PATH '[' int-expression ']'
    |  ATTRIBUTE-PATH '[' string-expression ']'

SET-ACTION-RHS ::= VALUE

VALUE ::=
    OPERAND
    | OPERAND '+' OPERAND
    | OPERAND '-' OPERAND

OPERAND ::=
    SET-FUNCTION
    | ATTRIBUTE-PATH
    | LITERAL

LITERAL ::=
    | null-expression
    | bool-expression
    | binary-expression
    | string-expression
    | int-expression
    | long-expression
    | double-expression
    | decimal-expression
    | list-expression
    | map-expression
    | binary-set-expression
    | string-set-expression
    | int-set-expression
    | long-set-expression
    | double-set-expression
    | decimal-set-expression

SET-FUNCTION ::=
    IF-NOT-EXIST
    | LIST-APPEND

IF-NOT-EXIST ::=
    'if_not_exists' '(' ATTRIBUTE-PATH, VALUE ')'

LIST-APPEND ::=
    'list_append' '(' OPERAND, OPERAND ')'
```


* CONDITION EXPRESSION OPERATORS AND FUNCTIONS
* https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Expressions.OperatorsAndFunctions.html

* FILTER EXPRESSION OPERATORS AND FUNCTIONS
* https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Query.html#Query.FilterExpression

```
CONDITION-EXPRESSION ::=
    CONDITION-OPERAND '=' CONDITION-OPERAND
    | CONDITION-OPERAND '<>' CONDITION-OPERAND
    | CONDITION-OPERAND '<' CONDITION-OPERAND
    | CONDITION-OPERAND '<=' CONDITION-OPERAND
    | CONDITION-OPERAND '=' CONDITION-OPERAND
    | CONDITION-OPERAND '>=' CONDITION-OPERAND
    | CONDITION-OPERAND '>' CONDITION-OPERAND
    | CONDITION-OPERAND `between` CONDITION-OPERAND `and` CONDITION-OPERAND
    | CONDITION-OPERAND 'in' ( CONDITION-OPERAND (',' CONDITION-OPERAND)* )
    | CONDITION-FUNCTION
    | CONDITION-EXPRESSION 'and' CONDITION-EXPRESSION
    | CONDITION-EXPRESSION 'or' CONDITION-EXPRESSION
    | 'not' CONDITION-EXPRESSION
    | '(' CONDITION-EXPRESSION ')'

CONDITION-OPERAND ::=
    CONDITION-FUNCTION
    | ATTRIBUTE-PATH
    | LITERAL

CONDITION-FUNCTION ::=
    'attribute_exists' '(' ATTRIBUTE-PATH ')'
    | 'attribute_not_exists' '(' ATTRIBUTE-PATH ')'
    | 'attribute_type' '(' ATTRIBUTE-PATH, string-expression ')'
    | 'begins_with' '(' ATTRIBUTE-PATH, string-expression ')'
    | 'contains' '(' ATTRIBUTE-PATH, CONDITION-OPERAND ')'
    | 'size' '(' ATTRIBUTE-PATH ')'

ATTRIBUTE-PATH ::=
    parameter
    |  ATTRIBUTE-PATH '.' member-name
    |  ATTRIBUTE-PATH '[' int-expression ']'
    |  ATTRIBUTE-PATH '[' string-expression ']'
```



## BACKUP

            switch(expression) {
            case BinaryExpression binaryExpression:
            case BlockExpression blockExpression:
            case ConditionalExpression conditionalExpression:
            case ConstantExpression constantExpression:
            case DebugInfoExpression debugInfoExpression:
            case DefaultExpression defaultExpression:
            case DynamicExpression dynamicExpression:
            case GotoExpression gotoExpression:
            case IndexExpression indexExpression:
            case InvocationExpression invocationExpression:
            case LabelExpression labelExpression:
            case LambdaExpression lambdaExpression:
            case ListInitExpression listInitExpression:
            case LoopExpression loopExpression:
            case MemberExpression memberExpression:
            case MemberInitExpression memberInitExpression:
            case MethodCallExpression methodCallExpression:
            case NewArrayExpression newArrayExpression:
            case NewExpression newExpression:
            case ParameterExpression parameterExpression:
            case RuntimeVariablesExpression runtimeVariablesExpression:
            case SwitchExpression switchExpression:
            case TryExpression tryExpression:
            case TypeBinaryExpression typeBinaryExpression:
            case UnaryExpression unaryExpression:
                break;
            }

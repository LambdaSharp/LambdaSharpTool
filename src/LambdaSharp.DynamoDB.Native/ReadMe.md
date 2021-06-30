> TODO: move docs to the right place

> TODO: Ability to designated a property to hold the "_m" (modified) date-timestamp

> TODO: Test deserialization of custom type derived from `IList<Foo>`

> TODO: Add `BatchWriteItem` operation

> TODO: Add `ExecuteTransaction` operation

> TODO: Add `BatchGetItem()` operation (can only fetch up to 100 items)

> TODO: Add `Scan()` operation

> TODO: Add link to serialization readme

> TODO: Look for all `/*` comments to see if they should be pulled into the readme

> TODO: Better name for `QueryAnyType()`?

> TODO: Rename `LambdaSharp.DynamoDB.Native`

> TODO: test if a `HashSet<string>` property could always be initialized to an empty site and round-tripped; it would make logic much simpler!

> TODO: enum type are treated as string!

> TODO: test `GetItem<>` when item doesn't exist

> TODO: test `Get(record => record)` to see if it add `PK` and `SK` to the projection

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

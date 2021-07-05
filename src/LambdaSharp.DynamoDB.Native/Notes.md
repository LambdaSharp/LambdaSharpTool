> TODO: move docs to the right place

> TODO: Ability to designated a property to hold the "_m" (modified) date-timestamp


> TODO: Add link to serialization readme

> TODO: doc: enum type are treated as string!

> TODO: `GetAttributePath` should fail on record

> TODO: only `PutItem()` operations need secondary keys

> TODO: pre-defined key projections that are done by `PutItem()` and `UpdateItem()` when needed: `[DynamoProjectedAttribute("GS1PK", "ORDER#{OrderId}")]`
> * register: `AddAttributeProjection("GS1PK", record => $"CUSTOMER#{record.CustomerId}")`
> * implicitly registered: `AddAttributeProjection("_t", record => record.GetType().FullName")`
> * implicitly registered: `AddAttributeProjection("_m", record => DateTimeOffset.UtcNow")`
```csharp
new DynamoTableOptions {
    ExpectedTypeNamespace = "Foo",

    RecordTypes = {
        new DataTableRecordType<OrderRecord> {
            Attributes = {
                ["GS1PK"] = record => $"ORDER#{record.OrderId}"),
                ["GSI1SK"] = record => $"INFO"
            }
        },
        new DataTableRecordType<OrderItemRecord> {
            Attributes = {
                ["GS1PK"] = record => $"ORDER#{record.OrderId}"),
                ["GSI1SK"] = record => $"ITEM#{record.ItemId}"
            }
        }
    }
}
```

> TODO: should we use the `_t` attribute to error out when deserializing the wrong row type?


# LambdaSharp.DynamoDB.Native

## Data Mapping

### TODO (missing alternative for NULL and L)

> TODO: equivalence rules for SET update operations (T, IDictionary<string, T>, IList<T>, ISet<T>)

> TODO: list all valid literal expressions (`"abc"`, `123`, `true`, `new string[0]`, `new[] { "hello" }`)

### Attribute to .NET conversion without type hints


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

        /* UPDATE EXPRESSION
         * https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Expressions.UpdateExpressions.html
         *
         * update-expression ::=
         *     [ SET action [, action] ... ]
         *     [ REMOVE action [, action] ...]
         *     [ ADD action [, action] ... ]
         *     [ DELETE action [, action] ...]
         *
         * set-action ::=
         *     path = value-expression
         *
         * value-expression ::=
         *     operand
         *     | operand '+' operand
         *     | operand '-' operand
         *
         * operand ::=
         *     path | set-function
         *
         * set-function ::=
         *     if_not_exists (path, value)
         *     | list_append (operand, operand)
         *
         * remove-action ::=
         *     path
         *
         * add-action ::=
         *     path value
         *
         * delete-action ::=
         *     path value
         */


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

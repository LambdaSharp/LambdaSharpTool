---
title: Condition/Filter Expression Specification - DynamoDB for .NET - LambdaSharp
description: Specification for the condition/filter expression in PutItem, DeleteItem, UpdateItem, and Query
keywords: specification, putitem, deleteitem, updateitem, query, api, dynamodb, aws, amazon
---

# Condition/Filter Expression Specification

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

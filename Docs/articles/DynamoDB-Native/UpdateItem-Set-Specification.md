---
title: UpdateItem SET Expression Specification - DynamoDB for .NET - LambdaSharp
description: Specification for the SET expression in UpdateItem
keywords: specification, updateitem, api, dynamodb, aws, amazon
---

# UpdateItem SET Expression Specification

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
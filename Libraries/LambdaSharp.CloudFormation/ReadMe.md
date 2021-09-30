# LambdaSharp.CloudFormation

This assembly is used to build and generate CloudFormation templates.

## Usage

## Rules

* Conditions
    * Declaration
        * validate the condition name pattern: `^[a-zA-Z0-9]*$`
        * validate condition name length is less than 256
        * warn if condition is not used
    * `!And` or `!Or`
        * must have 2 to 10 conditions
        * must be a condition function -or- a condition name
    * `!Not`
        * must have 1 condition
        * must be a condition function -or- a condition name
    * `!Condition`
        * must reference an existing condition
    * `!Equals`
        * must have 2 expressions
        * allowed functions `!Ref`, `!FindInMap`, `!Sub`, `!Join`, `!Select`, `!Split`
        * TODO:
            * Do the return expression types have to match? 
            * Can I compare a string to an int?
    * `!Ref`
        * must reference a parameter

* Rules
    * TODO

* Functions
    * `!Base64`
        * must have 1 expressions
        * expression type must be string
    * `!Cidr`
        * must have 2 or 3 expressions
        * parameter 1
            * allowed functions: `!FindInMap`, `!Select`, `!Ref`, `!GetAtt`, `!Sub`, `!ImportValue`
            * must be valid string literal: `^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])(\/([0-9]|[1-2][0-9]|3[0-2]))$`
        * parameter 2
            * allowed functions: `!FindInMap`, `!Select`, `!Ref`, `!GetAtt`, `!Sub`, `!ImportValue`
            * must be valid int literal
        * parameter 3 (optional)
            * allowed functions: `!FindInMap`, `!Select`, `!Ref`, `!GetAtt`, `!Sub`, `!ImportValue`
            * must be valid int literal
    * `!FindInMap`
        * TODO
    * `!GetAtt`
        * TODO
    * `!GetAz`
        * TODO
    * `!If`
        * TODO
    * `!ImportValue`
        * TODO
    * `!Join`
        * TODO
    * `!Select`
        * TODO
    * `!Split`
        * TODO
    * `!Sub`
        * TODO

* Resources
    * `!Ref`
        * check that a resource with the given name exists
        * if referenced resource has a condition
            * ensure the `!Ref` expression is nested inside an `!If` expression
            * ensure the corresponding `!If` branch is only taken when the resource condition is true

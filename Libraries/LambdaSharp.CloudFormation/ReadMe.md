# LambdaSharp.CloudFormation

This assembly is used to build and generate CloudFormation templates.

## Usage

## Rules

* Conditions
    * Declaration
        * validate the condition name is a string
        * validate string literal: `^[a-zA-Z0-9]*$`
        * warn if condition is not used
    * `!And` or `!Or`
        * must have 2 to 10 conditions
        * must be a condition function
    * `!Not`
        * must have 1 condition
        * must be a condition function
    * `!Condition`
        * must reference a condition from `Conditions` section
    * `!Equals`
        * must have 2 expressions
        * allowed functions `!Ref`, `!FindInMap`, `!Sub`, `!Join`, `!Select`, `!Split`
        * parameters have string type
    * `!Ref`
        * must reference a parameter

* Rules
    * TODO

* Functions in Resources & Export values
    * `!Base64`: https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/intrinsic-function-reference-base64.html
        * must have 1 expression
        * parameter 1 (valueToEncode)
            * expression return type must be string
    * `!Cidr`: https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/intrinsic-function-reference-cidr.html
        * must have either 2 or 3 expressions
        * parameter 1 (ipBlock)
            * allowed functions: `!FindInMap`, `!Select`, `!Ref`, `!GetAtt`, `!Sub`, `!ImportValue`
            * validate string literal: `^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])(\/([0-9]|[1-2][0-9]|3[0-2]))$`
        * parameter 2 (count)
            * allowed functions: `!FindInMap`, `!Select`, `!Ref`, `!GetAtt`, `!Sub`, `!ImportValue`
            * validate int literal: 1 .. 256
        * parameter 3 (cidrBits, optional)
            * allowed functions: `!FindInMap`, `!Select`, `!Ref`, `!GetAtt`, `!Sub`, `!ImportValue`
            * validate int literal: 1 .. 128
    * `!FindInMap`: https://github.com/aws-cloudformation/cfn-lint/blob/main/src/cfnlint/rules/functions/FindInMap.py
        * must have 3 expressions
        * parameter 1 (MapName)
            * allowed functions: `!FindInMap`, `!Ref`
            * must be a string literal
            * must exist in `Mappings` section
        * parameter 2 (TopLevelKey)
            * allowed functions: `!FindInMap`, `!Ref`
            * must be string or int literal
        * parameter 3 (SecondLevelKey)
            * allowed functions: `!FindInMap`, `!Ref`
            * must be string or int literal
    * `!GetAz`: https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/intrinsic-function-reference-getavailabilityzones.html
        * must have 1 expression
        * parameter 1 (region)
            * allowed functions: `!Ref`
            * must be a string literal
            * must be a valid AWS region name -or- an empty string (current region)
    * `!If`
        * must have 3 expressions
        * parameter 1 (condition_name)
            * must be a string literal
            * must exist in `Conditions` section
        * parameter 1 (value_if_true)
            * expression type must match if_false branch -or- `!Ref AWS::NoValue`
        * parameter 1 (value_if_false)
            * expression type must match if_true branch -or- `!Ref AWS::NoValue`
    * `!ImportValue`
        * must have 1 expression
        * parameter 1 (sharedValueToImport)
            * allowed functions: `!Base64`, `!FindInMap`, `!If`, `!Join`, `!Select`, `!Split`, `!Ref`, `!Sub`
            * must be a string expression
    * `!Join`
        * must have 2 expressions
        * parameter 1 (delimiter)
            * must be a string literal
        * parameter 2 (ListOfValues)
            * must be a list expression
            * list items must be string expressions
    * `!Select`
        * must have 2 expressions
        * parameter 1 (index)
            * must be an int expression: `^\d+$`
        * parameter 1 (listOfObjects)
            * must be a list expression
    * `!Split`
        * must have 2 expressions
        * parameter 1 (delimiter)
            * must be a string literal
        * parameter 2 (source string)
            * must be a string expression
    * `!Sub` string
        * must have 1 expression
        * parameter 1 ()
            * TODO
    * `!Ref`
        * check that a resource with the given name exists
        * if referenced resource has a condition
            * ensure the `!Ref` expression is nested inside an `!If` expression
            * ensure the corresponding `!If` branch is only taken when the resource condition is true
    * `!GetAtt`
        * TODO

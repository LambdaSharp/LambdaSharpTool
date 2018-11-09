![λ#](LambdaSharp_v2_small.png)

# LambdaSharp Module - Outputs Section

The `Outputs` section lists the exports, custom resource definitions, and CloudFormation macro definitions for the module.

Module exports are converted into [CloudFormation export](https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/using-cfn-stack-exports.html) values for top-level stacks. For nested stacks, the module exports are converted into CloudFormation stack outputs. This behavior prevents other modules from taking dependencies on nested stacks.

Custom resource definitions create new types of resources that can be used by other modules. Custom resources are a powerful way to expand the capabilities of modules beyond those provided by CloudFormation.

A macro definition creates a CloudFormation macro for the deployment tier. The handler must be a Lambda function. Once deployed, the macro is available to all subsequent module deployments.

__Definitions__
* [Export](Module-Export.md)
* [Custom Resource](Module-CustomResource.md)
* [Macro](Module-Macro.md)

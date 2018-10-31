![λ#](LambdaSharp_v2_small.png)

# LambdaSharp Module - Outputs Section

The `Outputs` section lists the output values and custom resource definitions for the module.

Output values are converted in [CloudFormation export](https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/using-cfn-stack-exports.html) values for top-level stacks. For nested stacks, output values are converted to CloudFormation stack outputs. This behavior prevents other modules from taking dependencies on nested stacks.

Custom resource definitions create new types of resources that can be used by other modules. Custom resources are a powerful way to expand the capabilities of modules beyond those provided by CloudFormation.

__Definitions__
* [Output Value](Module-Output.md)
* [Custom Resource](Module-CustomResource.md)
* [Macro](Module-Macro.md)

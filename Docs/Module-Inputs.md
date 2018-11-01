![λ#](LambdaSharp_v2_small.png)

# LambdaSharp Module - Inputs Section

The `Inputs` section lists the module parameters and imports.

Module parameters are specified at module deployment time by the λ# CLI. Module parameters can be modified subsequently by updating the CloudFormation stack in the AWS console.

Import parameters are cross-module references. By default, these references are resolved by CloudFormation at deployment time. However,they can also be redirected to a different module output value or be given an specific value instead. This capability makes it possible to have a default behavior that is mostly convenient, while enabling modules to be re-wired to import parameters from other modules, or to be given existing values for testing or legacy purposes.

__Definitions__
* [Parameter](Module-Parameter.md)
* [Import (Cross-Module Reference)](Module-Import.md)

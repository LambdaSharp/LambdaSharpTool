# Cebes (v0.3-RC2) - 2018-09-12

* **Breaking Change:** Default module file name was changed to `Module.yml` from `Deploy.yml`. This change has no effect if the input file is explicitly specified.
* **Breaking Change:** S3 event subscriptions are now handled by a custom resource handler, which allows S3 event subscriptions to work with new and existing S3 buckets. However, this change is not backwards compatible with existing S3 event subscriptions.
* Updated [Bootstrap](Bootstrap/) procedure.
* Added support for short form CloudFormation intrinsic functions, such as <code>!Ref</code>, <code>!Join</code>, <code>!Sub</code>, etc.
* Added support to allow a parameter value to be a CloudFormation expression (e.g. `!Ref MyResource`).
* Added support parameter collections. All parameter types can have nested parameters.
* Added support for CloudFormation Macro sources. See [CloudFormation Macro](Samples/MacroSample/) sample.
* Added support for Kinesis Stream sources. See [Kinesis Stream](Samples/KinesisSample/) sample.
* Added support for DynamoDB Stream sources. See [DynamoDB Stream](Samples/DynamoDBSample/) sample.
* Added support for exporting functions to the parameter store.
* Added support to allow a Lambda environment variable to be a CloudFormation expression (e.g. `!Ref MyResource`).
* Added support for creating a new module using `lash new module --name ModuleName`
* Deployment artifacts are now created in an output directory.
* Improved how Lambda function parameters are passed in. Instead of relying on an embedded `parameters.json` file, parameters are now passed in via environment variables. This means that Lambda function packages no longer need to be re-uploaded because of parameter changes.
* Included `Sid` attribute for all built-in, automatic permissions being added to provide more context.
* Switched to native AWS Lambda `JsonSerializer` class for serializing/deserializing data.
* Fixed an issue where some CloudFormation properties needed to suffixed with `_` to work with [Humidifier](https://github.com/jakejscott/Humidifier) library for generating CloudFormation templates correctly.
* Upgraded [YamlDotNet](https://github.com/aaubry/YamlDotNet) library to 5.0.1

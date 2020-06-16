---
title: Finalizer - Constructors/Destructors for CloudFormation - LambdaSharp
description: LambdaSharp Finalizer is the constructor/destructor pattern for CloudFormation
keywords: constructor, destructor, finalizer, cloudformation
---

# Finalizer - Constructors/Destructors for CloudFormation
The finalizer is a Lambda function that is invoked when the last resource in a CloudFormation stack has been created, but before the stack is completed. Similarly, the finalizer is invoked first when a CloudFormation stack is deleted, but before any other resource is affected. Effectively, the finalizer plays the dual role of a constructor and destructor for CloudFormation stacks.

The finalizer is an amazingly helpful construct, which addresses many common use cases.

## Use Case 1: Initializing of Resources
A finalizer can be used to initialize a database, such as a DynamoDB table, as part of the CloudFormation stack creation. This capability is particularly useful for serverless applications that require an initial state to exist, such as content or blog engines. With a finalizer, the CloudFormation stack can import an initial data set as part of its creation.

## Use Case 2: Emptying a Resource
A finalizer can be used to prepare a resource for deletion, such as an S3 bucket. A common and frustrating experience is a failed CloudFormation stack deletion because of a non-empty S3 bucket. With a finalizer, the contents can be purged before the bucket is deleted.

> Note: emptying an S3 bucket is such a common use case that it is captured as a new resource type in the [`LambdaSharp.S3.IO` module](~/modules/LambdaSharp-S3-IO.md) (see [LambdaSharp::S3::EmptyBucket](~/modules/LambdaSharp-S3-EmptyBucket.md) resource for more details).

## Use Case 3: Clean-up of Dynamic Resources
A finalizer is crucial for CloudFormation stacks which create additional resources as part of their operation. Without a finalizer, a CloudFormation stack would otherwise leak resources when deleted.

## Use Case 4: Data Conversion between Deployments
A finalizer can be used to upgrade/downgrade a data representation between two deployments. CloudFormation invokes the finalizer with the module version, so the finalizer can determine if any schema changes need to be applied. If the finalizer fails (e.g. there is no upgrade/downgrade code path), CloudFormation aborts the operation before any other changes occur.

## The LambdaSharp Finalizer
A Lambda function becomes the module finalizer by naming it Finalizer. The rest is done behind the scenes by the LambdaSharp compiler.

```yaml
- Function: Finalizer
  Timeout: 500
  Memory: 256
```

## Implementation
The finalizer is a Lambda function that is referenced by an embedded custom resource. The custom resource causes CloudFormation to invoke the finalizer during the stack creation. To control the timing of the finalizer invocation, the custom resource is declared as depending on every other stack resource.

```yaml
Finalizer:
  Type: AWS::Lambda::Function
  Properties:
    Map of function properties

FinalizerInvocation:
  Type: Custom::ModuleFinalizer
  Properties:
    ServiceToken: !GetAt Finalizer.Arn
    DeploymentChecksum: !Ref DeploymentChecksum
    ModuleVersion: !Ref Module::Version
    DependsOn: List of conditional resources dependencies
  DependsOn:
    - List of non-conditional dependencies
```
In addition, the LambdaSharp compiler passes in the module version and checksum as properties of the custom resource. The module version enables the finalizer to detect upgrade/downgrade situations and react accordingly. The module checksum — which is computed by the LambdaSharp compiler — forces CloudFormation to always invoke the finalizer when the stack is updated.
Conditional resources are a bit more tricky since CloudFormation doesn’t support conditional dependencies. Instead, the LambdaSharp compiler creates a DependsOn property that lists each conditional resource using an Fn::If expression with its specific condition. This ensures that conditional resources are only referenced when they exist. Otherwise, CloudFormation validation would fail during the deployment operation.

## Closing Thoughts
The finalizer is a great addition to the CloudFormation toolkit. It is tricky to implement by hand, but fortunately the LambdaSharp compiler hides this complexity. It is a great example of the latent power of CloudFormation that can be unlocked with the right tooling.

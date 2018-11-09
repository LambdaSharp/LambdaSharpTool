![λ#](../../Docs/LambdaSharp_v2_small.png)

# LambdaSharp CloudFormation Custom Resource Function


Before you begin, make sure to [setup your λ# CLI](../../Runtime/).

## Module Definition

A [CloudFormation Custom Resource](https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/template-custom-resources.html) is defined with the `CustomResource` attribute in the `Outputs` section. The Custom resource definition is global to the deployment tier, which means that the associated resource type (e.g. `MyNamespace::MyResource`) can only be defined once.

```yaml
Module: CustomResourceSample
Description: A sample module for defining a custom resource

Outputs:

  - CustomResource: MyNamespace::MyResource
    Description: Handler for MyNamespace::MyResource custom resource
    Handler: ResourceHandler

Functions:

  - Function: ResourceHandler
    Description: This function is invoked by CloudFormation
    Memory: 128
    Timeout: 30
```

The custom resource can then be used by other modules by using its resource type.
```yaml
Module: MyModule
Description: A sample module that uses a custom resource

Variables:

  - Var: MyCustomResource
    Resource:
      Types: MyNamespace::MyResource
      Properties:
        # add custom resource properties if needed

```

## Function Code

The `ALambdaCustomResourceFunction` base class provides handling of the CloudFormation Custom Resource protocol and ensures that failures are properly communicated to CloudFormation. Failure to do so can lead to stalled deployments or rollback until they timeout, which can take over 30 minutes.

```csharp
public class Function : ALambdaCustomResourceFunction<RequestProperties, ResponseProperties> {

    //--- Methods ---
    public override Task InitializeAsync(LambdaConfig config)
        => Task.CompletedTask;

    protected override async Task<Response<ResponseProperties>> HandleCreateResourceAsync(Request<RequestProperties> request) {

        // TODO: create resource using configuration settings from request properties

        return new Response<ResponseProperties> {

            // assign a physical resource ID to custom resource
            PhysicalResourceId = "MyResource:123",

            // set response properties
            Properties = new ResponseProperties { }
        };
    }

    protected override async Task<Response<ResponseProperties>> HandleDeleteResourceAsync(Request<RequestProperties> request) {

        // TODO: delete resource using information from request properties

        return new Response<ResponseProperties>();
    }

    protected override async Task<Response<ResponseProperties>> HandleUpdateResourceAsync(Request<RequestProperties> request) {

        // TODO: update resource using configuration settings from request properties

        return new Response<ResponseProperties> {

            // optionally assign a new physical resource ID to custom resource
            PhysicalResourceId = "MyResource:123",

            // set updated response properties
            Properties = new ResponseProperties { }
        };
    }
}
}
```

![λ#](../../src/DocFx/images/LambdaSharpLogo.png)

# LambdaSharp Custom Resource Type Definition

Before you begin, make sure to [setup your LambdaSharp CLI](https://lambdasharp.net/articles/Setup.html).

## Module Definition

A custom resource type is defined with the `ResourceType` attribute. The resource type definition is global to the deployment tier, which means that the associated resource type (e.g. `MyNamespace::MyResource`) can only be defined once.

```yaml
Module: Sample.CustomResourceType
Description: A sample module for defining a custom resource
Items:

  - ResourceType: MyNamespace::MyResource
    Description: Definition for MyNamespace::MyResource resource
    Handler: ResourceHandler
    Properties:

      - Name: SampleInput
        Description: SampleInput description
        Type: String
        Required: true

    Attributes:

      - Name: SampleOutput
        Description: SampleOutput description
        Type: String

  - Function: ResourceHandler
    Description: This function is invoked by CloudFormation
    Memory: 128
    Timeout: 30
```

The custom resource can then be used by other modules by using its resource type.
```yaml
Module: MyModule
Description: A sample module that uses a custom resource
Using:

    - Module: Sample.CustomResourceType

Items:

  - Resource: MyCustomResource
    Type: MyNamespace::MyResource
    Properties:
        SampleInput: 123

  - Variable: MyOutput
    Scope: public
    Value: !GetAtt MyCustomResource.SampleOutput
```

## Function Code

The `ALambdaCustomResourceFunction` base class provides handling of the CloudFormation Custom Resource protocol and ensures that failures are properly communicated to CloudFormation. Failure to do so can lead to stalled deployments or rollback until they timeout, which can take over 30 minutes.

```csharp
public class Function : ALambdaCustomResourceFunction<MyResourceProperties, MyResourceAttributes> {

    //--- Methods ---
    public override Task InitializeAsync(LambdaConfig config)
        => Task.CompletedTask;

    protected override async Task<Response<MyResourceAttributes>> HandleCreateResourceAsync(Request<MyResourceProperties> request) {

        // TO-DO: create resource using configuration settings from request properties

        return new Response<MyResourceAttributes> {

            // assign a physical resource ID to custom the resource
            PhysicalResourceId = "MyResource:123",

            // set response attributes
            Attributes = new MyResourceAttributes { }
        };
    }

    protected override async Task<Response<MyResourceAttributes>> HandleDeleteResourceAsync(Request<MyResourceProperties> request) {

        // TO-DO: delete resource using information from request properties

        return new Response<MyResourceAttributes>();
    }

    protected override async Task<Response<MyResourceAttributes>> HandleUpdateResourceAsync(Request<MyResourceProperties> request) {

        // TO-DO: update resource using configuration settings from request properties

        return new Response<MyResourceAttributes> {

            // optionally assign a new physical resource ID to the custom resource
            PhysicalResourceId = "MyResource:123",

            // set updated response attributes
            Attributes = new MyResourceAttributes { }
        };
    }
}
```

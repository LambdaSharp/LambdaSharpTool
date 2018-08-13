![λ#](../../Docs/LambdaSharp_v2_small.png)

# LambdaSharp Slack Command Function 

Before you begin, make sure to [setup your λ# environment](../../Bootstrap/).

## Module File

An invocations schedule is created by adding a `Schedule` source to each function. The schedule can either be directly a [CloudWatch Events schedule expression](https://docs.aws.amazon.com/AmazonCloudWatch/latest/events/ScheduledEvents.html) or it can provide an expression and a name. The `Name` attribute is used to distinguish between multiple schedule events when needed.

A [Slack](https://slack.com) command integration is created by adding the `SlackCommand` source to a function. The λ# tool automatically generates the API Gateway scaffolding including resources, methods, stages, and a RestApi deployment. In addition, the Slack command requests are converted into asynchronous invocation to avoid timeout errors for slow functions. Additional details on the Slack Command integration can be [found below](#reference).

```yaml
Name: SlackCommandSample

Description: A sample module integrating with Slack

Functions:
  - Name: MyFunction
    Description: This function is invoked by a Slack command
    Memory: 128
    Timeout: 30
    Sources:
      - SlackCommand: /slack
```

## Function Code

The following AWS Lambda function derives from the `ALambdaSlackCommandFunction` base class. The base class deserializes the request into a `SlackRequest` instance. In addition, standard output is captured by the base class and sent in response to the request. In case of an exception, the stack trace is sent back as an ephemeral response, which is only visible to the invoker of the Slack command.

```csharp
public class Function : ALambdaSlackCommandFunction {

    //--- Methods ---
    public override Task InitializeAsync(LambdaConfig config)
        => Task.CompletedTask;

    protected async override Task HandleSlackRequestAsync(SlackRequest request)
        => Console.WriteLine("Hello world!");
}
```

## Slack Setup

In order to invoke the Sample API, we need to know the URL. The easiest way is to copy the API Gateway base-URL
from the λ# tool output after the deployment has completed and append the resource URL path.

Copy the complete URL to the API Gateway endpoint and follow these steps:
1. Select *Customize Slack* from your Slack client.
1. Click *Configure Apps*.
1. Click *Custom Integrations*.
1. Click *Slack Commands*.
1. Click *Add Configuration*.
1. Enter `SlackCommandSample` as command.
1. Click *Add Slash Command Integration*.
1. Paste the API Gateway URL into the appropriate box.
1. Click *Save Integration*.
1. Go back to your Slack client.
1. Click on `slackbot`.
1. Type `/SlackCommandSample` and hit **ENTER**.

If all went well, you should see `Hello World!` as response (which may take a few seconds).

## Reference

The Slack integration converts the Slack form post into a JSON data structure that is deserialized by the `ALambdaSlackCommandFunction` base class.

```csharp
public class SlackRequest {

    //--- Properties ---
    public string Token { get; set; }
    public string TeamId { get; set; }
    public string TeamDomain { get; set; }
    public string EnterpriseId { get; set; }
    public string EnterpriseName { get; set; }
    public string ChannelId { get; set; }
    public string ChannelName { get; set; }
    public string UserId { get; set; }
    public string UserName { get; set; }
    public string Command { get; set; }
    public string Text { get; set; }
    public string ResponseUrl { get; set; }
}
```

The payload transformation occurs in the [API Gateway Method Request](https://docs.aws.amazon.com/apigateway/latest/developerguide/api-gateway-method-settings-method-request.html). As part of the Slack integration, the API Gateway endpoint must be configured as asynchronous by injecting the `X-Amz-Invocation-Type` custom HTTP header with the value `'Event'`. Making the invocation asynchronous avoids timeout errors in Slack when the Lambda function is slow to respond. The actual payload transformation is done using an [Apache Velocity](http://velocity.apache.org/) template. The template converts the form URL-encoded payload into a JSON document.

```
{
    #foreach($token in $input.path('$').split('&'))
        #set($keyVal = $token.split('='))
        #set($keyValSize = $keyVal.size())
        #if($keyValSize == 2)
            #set($key = $util.escapeJavaScript($util.urlDecode($keyVal[0])))
            #set($val = $util.escapeJavaScript($util.urlDecode($keyVal[1])))
            "$key": "$val"#if($foreach.hasNext),#end
        #end
    #end
}
```

Since the API Gateway endpoint is asynchronous, the [API Gateway Method Response](https://docs.aws.amazon.com/apigateway/latest/developerguide/api-gateway-method-settings-method-response.html) must provide a default, valid JSON response. An empty response message is accepted by Slack without any visible output.

```json
{
    "response_type": "in_channel",
    "text": ""
}
```

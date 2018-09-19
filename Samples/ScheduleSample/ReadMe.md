![λ#](../../Docs/LambdaSharp_v2_small.png)

# LambdaSharp Schedule Function

Before you begin, make sure to [setup your λ# environment](../../Bootstrap/).

## Module File

An invocations schedule is created by adding a `Schedule` source to each function. The schedule can either be directly a [CloudWatch Events schedule expression](https://docs.aws.amazon.com/AmazonCloudWatch/latest/events/ScheduledEvents.html) or it can provide an expression and a name. The `Name` attribute is used to distinguish between multiple schedule events when needed.

```yaml
Name: ScheduleSample

Description: A sample module using schedule events

Functions:

  - Name: MyFunction
    Description: This function is invoked by a scheduled event
    Memory: 128
    Timeout: 30
    Sources:

      # a simple rate expression
      - Schedule: rate(1 min)

      # a complex cron expression
      - Schedule: cron(0/15 11-17 ? * * *)

      # a schedule event with a name
      - Schedule: rate(1 hour)
        Name: Hourly
```

## Function Code

The schedule event can be parsed into a `LambdaScheduleEvent` message instance by using the `ALambdaFunction<T>` base class.

```csharp
public class Function : ALambdaFunction<LambdaScheduleEvent> {

    //--- Methods ---
    public override Task InitializeAsync(LambdaConfig config)
        => Task.CompletedTask;

    public override async Task<object> ProcessMessageAsync(LambdaScheduleEvent schedule, ILambdaContext context) {
        LogInfo($"Version = {schedule.Version}");
        LogInfo($"Id = {schedule.Id}");
        LogInfo($"Source = {schedule.Source}");
        LogInfo($"Account = {schedule.Account}");
        LogInfo($"Time = {schedule.Time}");
        LogInfo($"Region = {schedule.Region}");
        LogInfo($"Name = {schedule.Name}");
        return "Ok";
    }
}
```

## Reference

The λ# tool creates a CloudWatch Events rule for each schedule expression. The `Name` value, when provided, is injected using an input transformer. This allows the receiving code to differentiate invocations across different events.

```csharp
public class LambdaScheduleEvent {

    //--- Properties ---
    public string Version { get; set; }
    public string Id { get; set; }
    public string Source { get; set; }
    public string Account { get; set; }
    public DateTime Time { get; set; }
    public string Region { get; set; }
    public string Name { get; set; }
}
```

![λ#](../../../Docs/LambdaSharp_v2_small.png)

# LambdaSharp CLI - Delete Orphaned Lambda CloudWatch Logs Command

The `util delete-orphan-lambda-logs` command is used to delete CloudWatch log groups that were created by Lambda functions which no longer exist. Note, λ# modules always clean up their Lambda logs. However, if you have been experimenting with Lambda functions in the past, you may a log of CloudWatch logs that are just sitting there for no reason. This command will take care of them.

## Options

<dl>

<dt><code>--dryrun;</code></dt>
<dd>(optional) Show the result of the clean-up operation without deleting anything</dd>

</dl>

## Examples

### Delete all orphaned Lambda CloudWatch logs

__Using PowerShell/Bash:__
```bash
lash util delete-orphan-lambda-logs
```

Output:
```
LambdaSharp CLI (v0.5) - Delete orphaned Lambda CloudWatch logs

Found 87 log groups. Deleted 0. Skipped 0.

Done (finished: 1/17/2019 3:33:06 PM; duration: 00:00:06.7101744)
```

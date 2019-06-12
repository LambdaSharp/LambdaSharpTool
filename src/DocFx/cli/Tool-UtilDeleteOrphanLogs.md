# Delete Orphaned Lambda and API Gateway V1/V2 CloudWatch Logs Command

The `util delete-orphan-logs` command is used to delete CloudWatch log groups that were created by Lambda functions and API Gateway V1/V2 instances which no longer exist. Note, λ# modules always clean up their Lambda and API Gateway V1/V2 logs. However, if you have been experimenting with Lambda or API Gateway V1/V2 in the past, you may have a lot of CloudWatch logs that are lingering for no reason. This command will take care of them.

## Options

<dl>

<dt><code>--dryrun;</code></dt>
<dd>

(optional) Show the result of the clean-up operation without deleting anything
</dd>

<dt><code>--aws-profile|-P &lt;NAME&gt;</code></dt>
<dd>

(optional) Use a specific AWS profile from the AWS credentials file
</dd>

</dl>

## Examples

### Delete all orphaned Lambda CloudWatch logs

__Using PowerShell/Bash:__
```bash
lash util delete-orphan-logs
```

Output:
```
LambdaSharp CLI (v0.6.0.1) - Delete orphaned Lambda CloudWatch logs

* deleted 'API-Gateway-Execution-Logs_s540s2ian5/staging'
* deleted 'API-Gateway-Execution-Logs_sdpwn23vx4/LATEST'
...
* deleted 'API-Gateway-Execution-Logs_udr9mnhukg/LATEST'
* deleted 'API-Gateway-Execution-Logs_ury6rahzhj/LATEST'

Found 285 log groups. Active 112. Orphaned 172. Skipped 0.

Done (finished: 6/12/2019 5:30:21 AM; duration: 00:00:05.8609430)
```

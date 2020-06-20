---
title: LambdaSharp Util Tier Command - Show Kinesis Failed Logs
description: Show the CloudWatch Log entries that were ingested by Kinesis Firehose, but failed to be processed by LambdaSharp.Core
keywords: cli, kinesis, firehose, logs
---
# Show Failed Kinesis Firehose CloudWatch Log Entries

The `util show-kinesis-failed-logs` fetches the failed Kinesis Firehose records from the S3 logging bucket. For each record, the source key is shown, as well as the record properties, and the decoded CloudWatch Log entries that are part of the record.

## Options

<dl>

<dt><code>--key-prefix|-k &lt;PREFIX&gt;</code></dt>
<dd>

(optional) S3 key prefix where the failed logging records are stored (default: logging-failed/processing-failed/)
</dd>

<dt><code>--tier|-T &lt;NAME&gt;</code></dt>
<dd>

(optional) Name of deployment tier (default: <code>LAMBDASHARP_TIER</code> environment variable)
</dd>

<dt><code>--aws-profile|-P &lt;NAME&gt;</code></dt>
<dd>

(optional) Use a specific AWS profile from the AWS credentials file
</dd>

<dt><code>--aws-region &lt;NAME&gt;</code></dt>
<dd>

(optional) Use a specific AWS region (default: read from AWS profile)
</dd>

<dt><code>--verbose|-V[:&lt;LEVEL&gt;]</code></dt>
<dd>

(optional) Show verbose output (0=Quiet, 1=Normal, 2=Detailed, 3=Exceptions; Normal if LEVEL is omitted)
</dd>

<dt><code>--no-ansi</code></dt>
<dd>

(optional) Disable colored ANSI terminal output
</dd>

<dt><code>--quiet</code></dt>
<dd>

(optional) Don't show banner or execution time
</dd>

</dl>

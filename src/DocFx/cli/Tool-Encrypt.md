---
title: LambdaSharp CLI - Encrypt Command
description: Encrypt a plaintext value using a KMS key
keywords: cli, encrypt, kms, security
---
# Encrypt Text

The `encrypt` command is used to encrypt sensitive information using a managed encryption key. The encryption key is selected by using the `--key` option.

## Options

<dl>

<dt><code>--key &lt;KEY-ID&gt;</code></dt>
<dd>

Specify encryption key ID or alias to use (default: use default deployment tier key)
</dd>

<dt><code>--decrypt</code></dt>
<dd>

(optional) Decrypt value before encrypting it
</dd>

<dt><code>--tier|-T &lt;NAME&gt;</code></dt>
<dd>

(optional) Name of deployment tier (default: <code>LAMBDASHARP_TIER</code> environment variable)
</dd>

<dt><code>--cli-profile|-C &lt;NAME&gt;</code></dt>
<dd>

(optional) Use a specific LambdaSharp CLI profile (default: Default)
</dd>

<dt><code>--aws-profile|-P &lt;NAME&gt;</code></dt>
<dd>

(optional) Use a specific AWS profile from the AWS credentials file
</dd>

<dt><code>--verbose|-V[:&lt;LEVEL&gt;]</code></dt>
<dd>

(optional) Show verbose output (0=Quiet, 1=Normal, 2=Detailed, 3=Exceptions; Normal if LEVEL is omitted)
</dd>

<dt><code>--no-ansi</code></dt>
<dd>

Disable colored ANSI terminal output
</dd>

</dl>

## Examples

### Encrypt file using a specific key

__Using PowerShell/Bash:__
```bash
lash encrypt --key alias/MySecretKey --tier Sandbox < api-key.txt
```

Output:
```
LambdaSharp CLI (v0.5) - Encrypt Value

AQICAHh7n6rans2ZnBXULLCW2KSdUUy7RTem4YuI0CcwDz0FoQHUw/OCuEEIRMyqYb0pR9WBAAAAcjBwBgkqhkiG9w0BBwagYzBhAgEAMFwGCSqGSIb3DQEHATAeBglghkgBZQMEAS4wEQQMOIUjrz5+SAYgcVsWAgEQgC+ZetbV40nNwQFf3CMWJkEdoDrfECWor3TwSMogNcTgFSknmXYElw3+xo1y2qIGqw==

Done (finished: 1/18/2019 1:23:54 PM; duration: 00:00:00.9146055)
```

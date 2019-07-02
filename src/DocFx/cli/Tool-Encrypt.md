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

### Encrypt command line argument using the default LambdaSharp secrets key

__Using PowerShell/Bash:__
```bash
lash encrypt --tier Sandbox "My private API key"
```

Output:
```
LambdaSharp CLI (v0.5) - Encrypt Value

AQICAHh7n6rans2ZnBXULLCW2KSdUUy7RTem4YuI0CcwDz0FoQGkGadrZzSIDzll+mhZDv4PAAAAcDBuBgkqhkiG9w0BBwagYTBfAgEAMFoGCSqGSIb3DQEHATAeBglghkgBZQMEAS4wEQQMPVWgBIicHN7OoJ8cAgEQgC1Lv5Z54jaH9xrYGFgLOfZ1CUTV5KLsFUex50bNBZZlIWNzIJ7Tb+OkHcVF7gs=

Done (finished: 1/18/2019 1:19:29 PM; duration: 00:00:00.9495032)
```

### Encrypt file using a specific key

__Using PowerShell/Bash:__
```bash
lash encrypt --key alias/Sandbox-LambdaSharpDefaultSecretKey --tier Sandbox < api-key.txt
```

Output:
```
LambdaSharp CLI (v0.5) - Encrypt Value

AQICAHh7n6rans2ZnBXULLCW2KSdUUy7RTem4YuI0CcwDz0FoQHUw/OCuEEIRMyqYb0pR9WBAAAAcjBwBgkqhkiG9w0BBwagYzBhAgEAMFwGCSqGSIb3DQEHATAeBglghkgBZQMEAS4wEQQMOIUjrz5+SAYgcVsWAgEQgC+ZetbV40nNwQFf3CMWJkEdoDrfECWor3TwSMogNcTgFSknmXYElw3+xo1y2qIGqw==

Done (finished: 1/18/2019 1:23:54 PM; duration: 00:00:00.9146055)
```

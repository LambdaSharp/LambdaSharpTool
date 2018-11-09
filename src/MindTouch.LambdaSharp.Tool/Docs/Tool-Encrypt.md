![λ#](../../../Docs/LambdaSharp_v2_small.png)

# LambdaSharp CLI - Encrypt Command

The `encrypt` command is used to encrypt sensitive information using a managed encryption key. The encryption key can either be selected by using the `--key` option or by specifying the deployment tier with the `--tier` option. In the latter case, the default LambdaSharp secret key for the chosen deployment tier is used.

## Options

<dl>

<dt><code>--key &lt;KEY-ID&gt;</code></dt>
<dd>Specify encryption key ID or alias to use</dd>

<dt><code>--tier|-T &lt;NAME&gt;</code></dt>
<dd>(optional) Name of deployment tier (default: <code>LAMBDASHARP_TIER</code> environment variable)</dd>

<dt><code>--cli-profile|-CLI &lt;NAME&gt;</code></dt>
<dd>(optional) Use a specific LambdaSharp CLI profile (default: Default)</dd>

<dt><code>--aws-profile|-P &lt;NAME&gt;</code></dt>
<dd>(optional) Use a specific AWS profile from the AWS credentials file</dd>

<dt><code>--verbose|-V:&lt;LEVEL&gt;</code></dt>
<dd>(optional) Show verbose output (0=quiet, 1=normal, 2=detailed, 3=exceptions)</dd>

</dl>

## Examples

### Encrypt command line argument using the default LambdaSharp secrets key

__Using Powershell/Bash:__
```bash
dotnet lash encrypt --tier Sandbox "My private API key"
```

Output:
```
MindTouch LambdaSharp CLI (v0.4) - Encrypt Value

AQICAHgSrVOcRYhYRlcuUe2MsGsBpVM/uMqHGnk3lkiOr+Z4zQEcazWl2Yj7k4FOaQvxigjlAAAAYTBfBgkqhkiG9w0BBwagUjBQAgEAMEsGCSqGSIb3DQEHATAeBglghkgBZQMEAS4wEQQMLkz18nq708B6qAwLAgEQgB6R8WTqQOGsd3unH3aJom9G7cFIiVZcI6B/H69AlEc=

Done (duration: 00:00:00.7073050)
```

### Encrypt file using a specific key

__Using Powershell/Bash:__
```bash
dotnet lash encrypt --key alias/MyOtherKey < api-key.txt
```

Output:
```
MindTouch LambdaSharp CLI (v0.4) - Encrypt Value

AQICAHgSrVOcRYhYRlcuUe2MsGsBpVM/uMqHGnk3lkiOr+Z4zQGny1unpOQD2gXQetH+kePVAAAAYTBfBgkqhkiG9w0BBwagUjBQAgEAMEsGCSqGSIb3DQEHATAeBglghkgBZQMEAS4wEQQMcuq8txyppwr47P/zAgEQgB5GsXDieoaObT6YaCxPEUGrlSy8Yvu8P9FWnIoEvgs=

Done (duration: 00:00:00.7421492)
```

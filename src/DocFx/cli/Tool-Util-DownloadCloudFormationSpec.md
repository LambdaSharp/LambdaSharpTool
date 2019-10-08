---
title: LambdaSharp CLI Util Command - Download CloudFormation Specification
description: Download the latest CloudFormation specification
keywords: cli, cloudformation, download
---
# Download CloudFormation Types Specification

The `util download-cloudformation-spec` is used to download the latest CloudFormation specification. By default, the CloudFormation specification is downloaded into the application data folder, located under the user account. However, for LambdaSharp contributors, the downloaded file is processed, compressed, and saved in the `$LAMBDASHARP/src/LambdaSharp.Tool/Resources` folder, so it can be used as an embedded resource.

## Examples

### Update CloudFormation Types Specification

__Using PowerShell/Bash:__
```bash
lash util download-cloudformation-spec
```

```bash
LambdaSharp CLI (v0.7.0.4) - Download CloudFormation JSON specification

Fetching specification from https://d1uauaxba7bl26.cloudfront.net/latest/gzip/CloudFormationResourceSpecification.json
Original size: 2,417,148
Stripped size: 549,585

Done (finished: 10/8/2019 2:56:08 PM; duration: 00:00:02.6376173)
```

### Update CloudFormation Types Specification for LambdaSharp Contributors

__Using PowerShell/Bash:__
```bash
lash util download-cloudformation-spec
```

Output:
```
LambdaSharp CLI (v0.7.0.4) - Download CloudFormation JSON specification

Fetching specification from https://d1uauaxba7bl26.cloudfront.net/latest/gzip/CloudFormationResourceSpecification.json
Original size: 2,417,148
Stripped size: 549,585
Stored compressed spec file C:/LambdaSharp/LambdaSharpTool\src\LambdaSharp.Tool\Resources\CloudFormationResourceSpecification.json.gz
Compressed file size: 90,574

Done (finished: 10/8/2019 2:55:38 PM; duration: 00:00:02.6412424)
```

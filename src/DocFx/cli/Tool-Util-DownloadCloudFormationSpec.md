---
title: LambdaSharp CLI Util Command - Download CloudFormation Specification
description: Download the latest CloudFormation specification
keywords: cli, cloudformation, download
---
# Download CloudFormation Types Specification

The `util download-cloudformation-spec` is used by LambdaSharp contributors to download the latest CloudFormation specification. The downloaded file is automatically processed, compressed, and saved in the `$LAMBDASHARP/src/LambdaSharp.Tool/Resources` folder.

**NOTE:** this command is does not work unless the `LAMBDASHARP` environment variable is defined.

## Examples

### Update CloudFormation Types Specification

__Using PowerShell/Bash:__
```bash
lash util download-cloudformation-spec
```

Output:
```
LambdaSharp CLI (v0.5) - Download CloudFormation JSON specification to LAMBDASHARP development folder

Fetching specification from https://d1uauaxba7bl26.cloudfront.net/latest/gzip/CloudFormationResourceSpecification.json
Original size: 1,706,310
Stripped size: 452,018
Stored compressed spec file C:/LambdaSharpTool/src/LambdaSharp.Tool/Resources/CloudFormationResourceSpecification.json.gz
Compressed file size: 54,086

Done (finished: 1/17/2019 3:43:07 PM; duration: 00:00:02.3330652)
```

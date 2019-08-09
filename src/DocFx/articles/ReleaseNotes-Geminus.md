# λ# - Geminus (v0.7) - 2019-06-26

> Geminus of Rhodes, was a Greek astronomer and mathematician, who flourished in the 1st century BC. An astronomy work of his, the Introduction to the Phenomena, still survives; it was intended as an introductory astronomy book for students. He also wrote a work on mathematics, of which only fragments quoted by later authors survive. [(Wikipedia)](https://en.wikipedia.org/wiki/Geminus)

## What's New

> TODO:
> * `lash init --quick-start`
> * cannot publish a stable version with DIRTY changes
> *
```
Using:

  - Module: LambdaSharp.S3.IO:0.5
```
> is now
```
Using:

  - Module: LambdaSharp.S3.IO@lambdasharp
```

### Request Payer format for S3 buckets

### New S3 Bucket Path Format

* Module version file (JSON): `{Origin}/{ModulePrefix}/{ModuleSuffix}/{ModuleVersion}`
* Module assets: `{Origin}/{ModulePrefix}/{ModuleSuffix}/.assets/{AssetName}`

### Publish

* `--module-origin`
* TODO: add docs

## Removed Module::DefaultSecretKey

## Minimal Deployment Tier

* `lash config` is gone
* default secret key is gone
* ability to create a deployment tier without core services

## Updated Rollbar messages
* `Task timed out after 15.02 seconds` vs `Lambda timed out after 15.02 seconds`
* `Process exited before completing request` vs `Lambda exited before completing request`
* `Process ran out of memory (Max: 128 MB)` vs `Lambda ran out of memory (Max: 128 MB)`
* `Process nearing execution limits (Memory 80.12 %, Duration: 91.23 %)` vs `Lambda nearing execution limits (Memory 80.12 %, Duration: 91.23 %)`

## New format for LambdaSharp dependencies in .csproj files

* Making it contributor friendly

## ALambdaCustomResourceFunction

* added `Abort()` method

## X-Ray

* can be enabled for root module or all modules
* added support for api gateway

## lash encrypt

* new `--decrypt` option

## lash info

* show how much lambda storage is used
* show how much lambda reserved capacity is used
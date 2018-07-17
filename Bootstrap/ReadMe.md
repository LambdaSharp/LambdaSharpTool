![λ#](../Docs/LambdaSharp_v2_small.png)

# Setup LambdaSharp Environment

Setting up the λ# environment is required for each deployment name (e.g. `Test`, `Stage`, `Prod`, etc.).

## 1) Clone Repository

Switch to your preferred folder for Git projects and create a clone of the λ# tool.

```bash
git clone https://github.com/LambdaSharp/LambdaSharpTool.git
```

## 2) Set-up Environment

Define the `LAMBDASHARP` environment variable to point to the folder of the `LambdaSharpTool` clone. Furthermore, define `lst` as alias to invoke the λ# tool. The following script assumes the λ# tool was cloned into the `/Repose/LambdaSharpTool` directory.

```bash
export LAMBDASHARP=/Repos/LambdaSharpTool
alias lst="dotnet run -p $LAMBDASHARP/src/MindTouch.LambdaSharp.Tool/MindTouch.LambdaSharp.Tool.csproj --"
```

## 3) λ# Bootstrap

The λ# environment requires an AWS account to be setup for each deployment name (e.g. `Test`, `Stage`, `Prod`, etc.). λ# apps can be deployed once the deployment has the needed AWS resources.

λ# requires some resources to exist to deploy apps. These are created during the bootstrap phase. The following command creates the basic resources needed to deploy lambda functions, such as a the deployment bucket and dead-letter queue.

For the purpose of this tutorial, we use `MyDeployment` as the new deployment name. The next step MUST to be repeated for each deployment name.

```bash
lst deploy \
    --bootstrap \
    --deployment MyDeployment \
    --input $LAMBDASHARP/Bootstrap/LambdaSharp/Deploy.yml
```

## 4) λ# Rollbar Integration (optional)

λ# can optionally integrate with [Rollbar](https://rollbar.com). Rollbar is a service for capturing errors and warnings in apps. When the Rollbar integration is used a Rollbar project will be created for the app and all functions within the stack will automatically report any errors that occur to that project.

To prepare the Rollbar integration for deployment, you will need to obtain the `read` and `write` access tokens from your Rollbar account under _Account Access Tokens_:

In addition, create a `LambdaSharpRollbar-{{Deployment}}` project that will be used to track any errors for the Rollbar integration. Under _Project Access Tokens_ make a copy of the `post_server_item`.

Encrypt all three values using a KMS key (e.g. `developer-secrets-key`) and edit your `Deploy.yml` file as follows:
1. Under `RollbarToken`, paste the encrypted `post_server_item` value. This parameter is used by the Rollbar integration to post any errors that occur.
1. Under `ReadAccessToken`, paste the encrypted `read` access token. This parameter is used by the Rollbar integration to query the Rollbar account.
1. Under `WriteAccessToken`, paste the encrypted `write` access token. This parameter is used by the Rollbar integration to create new or delete old Rollbar projects.

Finally, under `Secrets`, list the alias of the encryption key that was used (e.g. `developer-secrets-key`).

Now you are ready to deploy your Rollbar integration. The following command deploys the custom resource handler for Rollbar. Once deployed, all subsequent λ# deployments will be assigned a dedicated Rollbar project for monitoring.

```bash
lst deploy \
    --bootstrap \
    --deployment MyDeployment \
    --input $LAMBDASHARP/Bootstrap/LambdaSharpRollbar/Deploy.yml
```

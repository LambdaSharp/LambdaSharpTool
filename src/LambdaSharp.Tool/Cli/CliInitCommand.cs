/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2022
 * lambdasharp.net
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.APIGateway.Model;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.IdentityManagement.Model;
using LambdaSharp.Build;
using LambdaSharp.Modules;
using LambdaSharp.Modules.Metadata;
using LambdaSharp.Tool.Internal;
using McMaster.Extensions.CommandLineUtils;

namespace LambdaSharp.Tool.Cli {
    using ModuleInfo = LambdaSharp.Modules.ModuleInfo;

    public class CliInitCommand : ACliCommand {

        //--- Constants ---
        private const string DEFAULT_API_GATEWAY_ROLE = "LambdaSharp-ApiGatewayRole";

        //--- Methods --
        public void Register(CommandLineApplication app) {
            app.Command("init", cmd => {
                cmd.HelpOption();
                cmd.Description = "Create or update a LambdaSharp deployment tier";

                // init options
                var protectStackOption = cmd.Option("--protect", "(optional) Enable termination protection for the CloudFormation stack", CommandOptionType.NoValue);
                var enableXRayTracingOption = cmd.Option("--xray[:<LEVEL>]", "(optional) Enable service-call tracing with AWS X-Ray for all resources in module  (0=Disabled, 1=RootModule, 2=AllModules; RootModule if LEVEL is omitted)", CommandOptionType.SingleOrNoValue);
                var versionOption = cmd.Option("--version <VERSION>", "(optional) Specify version for LambdaSharp.Core module (default: same as CLI version)", CommandOptionType.SingleValue);
                var parametersFileOption = cmd.Option("--parameters <FILE>", "(optional) Specify source filename for module parameters (default: none)", CommandOptionType.SingleValue);
                var forceDeployOption = cmd.Option("--force-deploy", "(optional) Force module deployment", CommandOptionType.NoValue);
                var quickStartOption = cmd.Option("--quick-start", "(optional, create-only) Use safe defaults for quickly setting up a LambdaSharp deployment tier.", CommandOptionType.NoValue);
                var coreServicesOption = cmd.Option("--core-services <VALUE>", "(optional, create-only) Select if LambdaSharp.Core services should be enabled or not (either Enabled or Disabled, default prompts)", CommandOptionType.SingleValue);
                var existingS3BucketNameOption = cmd.Option("--existing-s3-bucket-name <NAME>", "(optional, create-only) Existing S3 bucket name for module deployments (blank value creates new bucket)", CommandOptionType.SingleValue);
                var localOption = cmd.Option("--local <PATH>", "(optional) Provide a path to a local check-out of the LambdaSharp modules (default: LAMBDASHARP environment variable)", CommandOptionType.SingleValue);
                var usePublishedOption = cmd.Option("--use-published", "(optional) Force the init command to use the published LambdaSharp modules", CommandOptionType.NoValue);
                var promptAllParametersOption = cmd.Option("--prompt-all", "(optional) Prompt for all missing parameters values (default: only prompt for missing parameters with no default value)", CommandOptionType.NoValue);
                var allowUpgradeOption = cmd.Option("--allow-upgrade", "(optional) Allow upgrading LambdaSharp.Core across major releases (default: prompt)", CommandOptionType.NoValue);
                var skipApiGatewayCheckOption = cmd.Option("--skip-apigateway-check", "(optional) Skip API Gateway role check", CommandOptionType.NoValue);
                var initSettingsCallback = CreateSettingsInitializer(cmd);
                AddStandardCommandOptions(cmd);
                cmd.OnExecute(async () => {
                    ExecuteCommandActions(cmd);

                    // check if .aws/credentials file needs to be created
                    if(
                        !File.Exists(CredentialsFilePath)
                        && (
                            (Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID") == null)
                            || (Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY") == null)
                        )
                    ) {
                        var tmpSettings = new Settings(Version);

                        // prompt for AWS credentials information
                        Console.WriteLine();
                        tmpSettings.PromptLabel("Create AWS profile");
                        var accessKey = tmpSettings.PromptString("Enter the AWS Access Key ID");
                        var secretAccessKey = tmpSettings.PromptString("Enter the AWS Secret Access Key");
                        var region = tmpSettings.PromptString("Enter the AWS region", "us-east-1");

                        // create credentials file
                        var credentialsTemplate = ReadResource("credentials.txt", new Dictionary<string, string> {
                            ["REGION"] = region,
                            ["ACCESSKEY"] = accessKey,
                            ["SECRETACCESSKEY"] = secretAccessKey
                        });
                        try {
                            if(!Directory.Exists(CredentialsFolder)) {
                                Directory.CreateDirectory(CredentialsFolder);
                            }
                            File.WriteAllText(CredentialsFilePath, credentialsTemplate);
                        } catch {
                            LogError("unable to create .aws/credentials file");
                            return;
                        }
                    }

                    // initialize settings
                    var settings = await initSettingsCallback();
                    if(settings == null) {
                        return;
                    }

                    // check x-ray settings
                    if(!TryParseEnumOption(enableXRayTracingOption, XRayTracingLevel.Disabled, XRayTracingLevel.RootModule, out var xRayTracingLevel)) {

                        // NOTE (2018-08-04, bjorg): no need to add an error message since it's already added by 'TryParseEnumOption'
                        return;
                    }
                    if(!TryParseEnumOption(coreServicesOption, CoreServices.Undefined, CoreServices.Undefined, out var coreServices)) {

                        // NOTE (2018-08-04, bjorg): no need to add an error message since it's already added by 'TryParseEnumOption'
                        return;
                    }

                    // set initialization parameters
                    var existingS3BucketName = existingS3BucketNameOption.Value();
                    if(quickStartOption.HasValue()) {

                        // check if --core-services was specified, if not default to 'disabled'
                        coreServices = coreServicesOption.HasValue()
                            ? coreServices
                            : CoreServices.Disabled;

                        // check if --existing-s3-bucket-name was specified, if not default to ""
                        existingS3BucketName = existingS3BucketNameOption.HasValue()
                            ? existingS3BucketNameOption.Value()
                            : "";
                    }

                    // begin tier initialization
                    await Init(
                        settings,
                        allowDataLoos: true,
                        protectStackOption.HasValue(),
                        forceDeployOption.HasValue(),
                        versionOption.HasValue() ? VersionInfo.Parse(versionOption.Value()) : VersionInfoCompatibility.GetCoreServicesReferenceVersion(Version),
                        usePublishedOption.HasValue()
                            ? null
                            : (localOption.Value() ?? Environment.GetEnvironmentVariable("LAMBDASHARP")),
                        parametersFileOption.Value(),
                        promptAllParametersOption.HasValue(),
                        xRayTracingLevel,
                        coreServices,
                        existingS3BucketName,
                        allowUpgradeOption.HasValue(),
                        skipApiGatewayCheckOption.HasValue(),
                        quickStartOption.HasValue()
                    );
                });
            });
        }

        public async Task<bool> Init(
            Settings settings,
            bool allowDataLoos,
            bool protectStack,
            bool forceDeploy,
            VersionInfo version,
            string lambdaSharpPath,
            string parametersFilename,
            bool promptAllParameters,
            XRayTracingLevel xRayTracingLevel,
            CoreServices coreServices,
            string existingS3BucketName,
            bool allowUpgrade,
            bool skipApiGatewayCheck,
            bool quickStart
        ) {

            // NOTE (2019-08-15, bjorg): the deployment tier initialization must support the following scenarios:
            //  1. New deployment tier
            //  2. Updating an existing tier with any configuration changes
            //  3. Upgrading an existing tier to enable LambdaSharp.Core services
            //  4. Downgrading an existing tier to disable LambdaSharp.Core services

            // read the current deployment tier settings if possible
            await PopulateDeploymentTierSettingsAsync(
                settings,

                // bucket name and core services settings may be missing for deployment tier v0.6 or earlier
                requireBucketName: false,
                requireCoreServices: false,

                // version is more explicitly checked below
                requireVersionCheck: false,

                // deployment tier may not exist yet
                optional: true
            );
            if(HasErrors) {
                return false;
            }

            // check if a new installation is required
            var createNewTier = (settings.TierVersion == null);
            var updateExistingTier = forceDeploy;
            var disableCoreServicesForUpgrade = false;
            if(!createNewTier && !updateExistingTier) {

                // check if core services state was not specified
                if(coreServices == CoreServices.Undefined) {

                    // inherit current state
                    coreServices = settings.CoreServices;
                }

                // always upgrade Bootstrap state to Enabled, since the former is a transitional state only
                if(coreServices == CoreServices.Bootstrap) {
                    coreServices = CoreServices.Enabled;
                }

                // determine if the deployment tier needs to be updated
                var tierToToolVersionComparison = VersionInfoCompatibility.CompareTierVersionToToolVersion(settings.TierVersion, settings.ToolVersion);
                if(tierToToolVersionComparison == 0) {

                    // versions are identical; nothing to do, unless we're forced to update
                    updateExistingTier = forceDeploy

                        // it's a pre-release, which always needs to be updated
                        || settings.ToolVersion.IsPreRelease()

                        // we're running in contributor mode, which means new binaries may be built
                        || (lambdaSharpPath != null)

                        // deployment tier is running core services state is different from requested state
                        || (settings.CoreServices != coreServices)
                        || await IsNewerCoreModuleVersionAvailable();

                    // local functions
                    async Task<bool> IsNewerCoreModuleVersionAvailable() {
                        var coreModuleInfo = new ModuleInfo("LambdaSharp", "Core", settings.CoreServicesReferenceVersion, "lambdasharp");
                        var latestCoreModuleLocation = await new ModelManifestLoader(settings, "").ResolveInfoToLocationAsync(
                            coreModuleInfo,
                            coreModuleInfo.Origin,
                            ModuleManifestDependencyType.Shared,
                            allowImport: true,
                            showError: false
                        );
                        var latestCoreModuleVersion = latestCoreModuleLocation?.ModuleInfo.Version;
                        return settings.TierVersion.IsLessThanVersion(latestCoreModuleVersion);
                    }
                } else if(tierToToolVersionComparison > 0) {

                    // tier is newer; tool needs to get updated
                    LogError($"LambdaSharp tool is out of date (tool: {settings.ToolVersion}, tier: {settings.TierVersion})", new LambdaSharpToolOutOfDateException(settings.TierVersion));
                    return false;
                } else if(tierToToolVersionComparison < 0) {

                    // tier is older; let's only upgrade it if requested
                    updateExistingTier = true;

                    // tool version is more recent; if it's a minor update, proceed without prompting, otherwise ask user to confirm upgrade
                    if(!VersionInfoCompatibility.IsTierVersionCompatibleWithToolVersion(settings.TierVersion, settings.ToolVersion)) {

                        // update requires core service to be replaced
                        disableCoreServicesForUpgrade = true;

                        // check if an interactive confirmation prompt is required
                        if(!allowUpgrade) {
                            Console.WriteLine($"LambdaSharp Tier is out of date");
                            updateExistingTier = settings.PromptYesNo($"Do you want to upgrade LambdaSharp Tier '{settings.TierName}' from v{settings.TierVersion} to v{settings.CoreServicesReferenceVersion}?", defaultAnswer: false);
                        }
                    }
                    if(!updateExistingTier) {
                        return false;
                    }
                } else if(!forceDeploy) {
                    LogError($"LambdaSharp tool is not compatible (tool: {settings.ToolVersion}, tier: {settings.TierVersion}); use --force-deploy to proceed anyway");
                    return false;
                } else {

                    // force deploy it is!
                    updateExistingTier = true;
                }
            }

            // check if deployment tier with disabled core services needs to be created/updated
            Dictionary<string, string> parameters = null;
            var tierCommand = new CliTierCommand();
            var updated = false;
            if(
                createNewTier
                || (updateExistingTier && (

                    // deployment tier doesn't have core services (pre-0.7); so the bootstrap stack needs to be installed first
                    (settings.CoreServices == CoreServices.Undefined)

                    // deployment tier core services need to be disabled
                    || (coreServices == CoreServices.Disabled)

                    // we have to disable core services to upgrade
                    || disableCoreServicesForUpgrade
                ))
            ) {

                // deploy bootstrap stack with disabled core services
                if(!await DeployCoreServicesDisabledTemplate()) {
                    return false;
                }
                updated = true;
            }

            // check if API Gateway role needs to be set or updated
            if(!skipApiGatewayCheck) {
                await CheckApiGatewayRole(settings);
            }
            if(HasErrors) {
                return false;
            }

            // standard modules
            var standardModules = new List<string> {
                "LambdaSharp.Core"
            };

            // check if the module must be built and published first (only applicable when running lash in contributor mode)
            var buildPublishDeployCommand = new CliBuildPublishDeployCommand();
            if(lambdaSharpPath != null) {
                Console.WriteLine($"Building LambdaSharp modules");

                // attempt to parse the tool version from environment variables
                if(!VersionInfo.TryParse(Environment.GetEnvironmentVariable("LAMBDASHARP_VERSION"), out var moduleVersion)) {
                    LogError("unable to parse module version from LAMBDASHARP_VERSION");
                    return false;
                }

                // gather list of module to build and publish
                standardModules.AddRange(
                    Directory.GetDirectories(Path.Combine(lambdaSharpPath, "Modules"))
                        .Where(directory => File.Exists(Path.Combine(directory, "Module.yml")))
                        .Select(directory => Path.GetFileName(directory))
                        .Where(module => module != "LambdaSharp.Core")
                );
                foreach(var module in standardModules) {
                    var moduleSource = Path.Combine(lambdaSharpPath, "Modules", module, "Module.yml");
                    settings.WorkingDirectory = Path.GetDirectoryName(moduleSource);
                    settings.OutputDirectory = Path.Combine(settings.WorkingDirectory, "bin");

                    // build local module
                    if(!await buildPublishDeployCommand.BuildStepAsync(
                        settings,
                        Path.Combine(settings.OutputDirectory, "cloudformation.json"),
                        noAssemblyValidation: true,
                        noPackageBuild: false,
                        gitSha: new GitTool(BuildEventsConfig).GetGitShaValue(settings.WorkingDirectory, showWarningOnFailure: false),
                        gitBranch: new GitTool(BuildEventsConfig).GetGitBranch(settings.WorkingDirectory, showWarningOnFailure: false),
                        buildConfiguration: "Release",
                        selector: null,
                        moduleSource: moduleSource,
                        moduleVersion: moduleVersion,
                        forceBuild: true,
                        buildPolicy: null
                    )) {
                        return false;
                    }

                    // publish module
                    var moduleReference = await buildPublishDeployCommand.PublishStepAsync(settings, forcePublish: true, moduleOrigin: "lambdasharp");
                    if(moduleReference == null) {
                        return false;
                    }
                }
            }

            // check if core services do not need to be updated further
            if(coreServices == CoreServices.Disabled) {
                if(!updated) {
                    Console.WriteLine();
                    Console.WriteLine("Core Services disabled. No update required");
                }
                return true;
            }

            // check if operating services need to be installed/updated
            if(createNewTier) {
                Console.WriteLine();
                Console.WriteLine($"Creating new deployment tier {Settings.InfoColor}{settings.TierName}{Settings.ResetColor}");
            } else if(updateExistingTier) {
                Console.WriteLine();
                Console.WriteLine($"Updating deployment tier {Settings.InfoColor}{settings.TierName}{Settings.ResetColor}");
            } else {
                if(!updated) {
                    Console.WriteLine();
                    Console.WriteLine("No update required");
                }
                return true;
            }

            // read parameters if they haven't been read yet
            if(parameters == null) {
                if(parametersFilename != null) {
                    parameters = CliBuildPublishDeployCommand.ReadInputParametersFiles(settings, parametersFilename);
                    if(HasErrors) {
                        return false;
                    }
                } else {
                    parameters = new Dictionary<string, string>();
                }
            }
            if(quickStart) {

                // set all LambdaSharp.Core parameters to their default input values
                if(!parameters.ContainsKey("DeadLetterQueue")) {
                    parameters["DeadLetterQueue"] = "";
                }
                if(!parameters.ContainsKey("LoggingFirehoseStream")) {
                    parameters["LoggingFirehoseStream"] = "";
                }
                if(!parameters.ContainsKey("CoreSecretsKey")) {
                    parameters["CoreSecretsKey"] = "";
                }
                if(!parameters.ContainsKey("LoggingStreamRole")) {
                    parameters["LoggingStreamRole"] = "";
                }
                if(!parameters.ContainsKey("LoggingBucket")) {
                    parameters["LoggingBucket"] = "";
                }
            }

            // deploy LambdaSharp module
            foreach(var module in standardModules) {
                var isLambdaSharpCoreModule = (module == "LambdaSharp.Core");

                // explicitly import the LambdaSharp module (if it wasn't built locally)
                if(lambdaSharpPath == null) {
                    if(!await buildPublishDeployCommand.ImportStepAsync(
                        settings,
                        ModuleInfo.Parse($"{module}:{version}@lambdasharp"),
                        forcePublish: true
                    )) {
                        return false;
                    }
                }

                // deploy module
                if(!await buildPublishDeployCommand.DeployStepAsync(
                    settings,
                    dryRun: null,
                    moduleReference: $"{module}:{version}@lambdasharp",
                    instanceName: null,
                    allowDataLoos: allowDataLoos,
                    protectStack: protectStack,
                    parameters: parameters,
                    forceDeploy: forceDeploy,
                    promptAllParameters: promptAllParameters,
                    xRayTracingLevel: xRayTracingLevel,
                    deployOnlyIfExists: !isLambdaSharpCoreModule,
                    allowDependencyUpgrades: true
                )) {
                    return false;
                }

                // reset tier version if core module was deployed; this will force the tier settings to be refetched
                if(isLambdaSharpCoreModule) {
                    await PopulateDeploymentTierSettingsAsync(settings, force: true);
                }
            }

            // check if core services need to be enabled for deployed modules
            if(settings.CoreServices == CoreServices.Enabled) {
                await tierCommand.UpdateCoreServicesAsync(settings, enabled: true, showModules: false);
            }
            return !HasErrors;

            // local function
            async Task<bool> DeployCoreServicesDisabledTemplate() {

                // initialize stack with seed CloudFormation template
                var template = ReadResource("LambdaSharpCore.yml", new Dictionary<string, string> {
                    ["CORE-VERSION"] = settings.CoreServicesReferenceVersion.ToString(),
                    ["TOOL-VERSION"] = settings.ToolVersion.ToString(),
                    ["CHECKSUM"] = settings.ToolVersion.ToString().ToMD5Hash()
                });

                // check if bootstrap template is being updated or installed
                if(createNewTier) {
                    Console.WriteLine($"Creating LambdaSharp tier {Settings.InfoColor}{settings.TierName}{Settings.ResetColor}");
                } else {
                    Console.WriteLine($"Updating LambdaSharp tier {Settings.InfoColor}{settings.TierName}{Settings.ResetColor}");
                }

                // create lambdasharp CLI bootstrap stack
                var stackName = $"{settings.TierPrefix}LambdaSharp-Core";
                parameters = (parametersFilename != null)
                    ? CliBuildPublishDeployCommand.ReadInputParametersFiles(settings, parametersFilename)
                    : new Dictionary<string, string>();
                if(HasErrors) {
                    return false;
                }
                var bootstrapParameters = new Dictionary<string, string>(parameters) {
                    ["TierName"] = settings.Tier
                };

                // check if command line options were provided to set template parameters
                if((coreServices == CoreServices.Enabled) || (coreServices == CoreServices.Disabled)) {
                    bootstrapParameters["CoreServices"] = coreServices.ToString();
                }
                if(existingS3BucketName != null) {
                    bootstrapParameters["ExistingDeploymentBucket"] = (
                        string.IsNullOrEmpty(existingS3BucketName)
                        || existingS3BucketName.StartsWith("arn:", StringComparison.Ordinal)
                    )
                        ? existingS3BucketName
                        : "arn:aws:s3:::" + existingS3BucketName;
                }

                // prompt for missing parameters
                var templateParameters = await PromptMissingTemplateParameters(
                    settings,
                    stackName,
                    bootstrapParameters,
                    template,
                    quickStart
                );
                if(coreServices == CoreServices.Undefined) {

                    // determine wanted core services state from template parameters
                    var coreServicesValue = templateParameters.First(parameter => parameter.ParameterKey == "CoreServices")?.ParameterValue;
                    if(!Enum.TryParse<CoreServices>(coreServicesValue, ignoreCase: true, out coreServices)) {
                        LogError($"unable to parse CoreServices value from template parameters (found: '{coreServicesValue}')");
                        return false;
                    }
                }
                if(HasErrors) {
                    return false;
                }

                // disable core services in all deployed modules
                if(!createNewTier) {
                    await tierCommand.UpdateCoreServicesAsync(settings, enabled: false, showModules: false);
                    if(HasErrors) {
                        return false;
                    }
                }

                // create/update cloudformation stack
                if(createNewTier) {
                    Console.WriteLine($"=> Stack creation initiated for {Settings.InfoColor}{stackName}{Settings.ResetColor}");
                    var response = await settings.CfnClient.CreateStackAsync(new CreateStackRequest {
                        StackName = stackName,
                        Capabilities = new List<string> { },
                        OnFailure = OnFailure.DELETE,
                        Parameters = templateParameters,
                        EnableTerminationProtection = protectStack,
                        TemplateBody = template,
                        Tags = settings.GetCloudFormationStackTags("LambdaSharp.Core", stackName)
                    });
                    var created = await settings.CfnClient.TrackStackUpdateAsync(stackName, response.StackId, mostRecentStackEventId: null, logError: LogError);
                    if(created.Success) {
                        Console.WriteLine("=> Stack creation finished");
                    } else {
                        Console.WriteLine("=> Stack creation FAILED");
                        return false;
                    }
                } else {
                    Console.WriteLine();
                    Console.WriteLine($"=> Stack update initiated for {Settings.InfoColor}{stackName}{Settings.ResetColor}");
                    try {
                        var mostRecentStackEventId = await settings.CfnClient.GetMostRecentStackEventIdAsync(stackName);
                        var response = await settings.CfnClient.UpdateStackAsync(new UpdateStackRequest {
                            StackName = stackName,
                            Capabilities = new List<string> { },
                            Parameters = templateParameters,
                            TemplateBody = template,
                            Tags = settings.GetCloudFormationStackTags("LambdaSharp.Core", stackName)
                        });
                        var created = await settings.CfnClient.TrackStackUpdateAsync(stackName, response.StackId, mostRecentStackEventId, logError: LogError);
                        if(created.Success) {
                            Console.WriteLine("=> Stack update finished");
                        } else {
                            Console.WriteLine("=> Stack update FAILED");
                            return false;
                        }
                    } catch(AmazonCloudFormationException e) when(e.Message == "No updates are to be performed.") {

                        // this error is thrown when no required updates where found
                        Console.WriteLine("=> No stack update required");
                    }
                }
                await PopulateDeploymentTierSettingsAsync(settings, force: true);
                return !HasErrors;
            }
        }

        private async Task<List<Parameter>> PromptMissingTemplateParameters(
            Settings settings,
            string stackName,
            IDictionary<string, string> providedParameters,
            string templateBody,
            bool quickStart
        ) {

            // get summary of new template
            GetTemplateSummaryResponse templateSummary;
            try {

                // TODO (2019-08-09, bjorg): should we fetch the template JSON instead?
                templateSummary = await settings.CfnClient.GetTemplateSummaryAsync(new GetTemplateSummaryRequest {
                    TemplateBody = templateBody
                });
            } catch(AmazonCloudFormationException e) {
                LogError(e.Message);
                return null;
            }

            // find configuration for existing stack
            Stack existing = null;
            if(stackName != null) {
                try {
                    existing = (await settings.CfnClient.DescribeStacksAsync(new DescribeStacksRequest {
                        StackName = stackName
                    })).Stacks.First();
                } catch(AmazonCloudFormationException) { }
            }
            var result = new List<Parameter>();
            var missingParameters = new List<ParameterDeclaration>();
            foreach(var templateParameter in templateSummary.Parameters) {
                if(providedParameters.TryGetValue(templateParameter.ParameterKey, out var providedValue)) {

                    // use the provided parameter value
                    result.Add(new Parameter {
                        ParameterKey = templateParameter.ParameterKey,
                        ParameterValue = providedValue
                    });
                } else if(existing?.Parameters.Any(existingParam => existingParam.ParameterKey == templateParameter.ParameterKey) == true) {

                    // re-use the existing parameter value
                    result.Add(new Parameter {
                        ParameterKey = templateParameter.ParameterKey,
                        UsePreviousValue = true
                    });
                } else {

                    // add parameter to missing parameters
                    missingParameters.Add(templateParameter);
                }
            }

            // ask user for missing values
            if(missingParameters.Any()) {
                if(quickStart) {
                    foreach(var missingParameter in missingParameters) {
                        result.Add(new Parameter {
                            ParameterKey = missingParameter.ParameterKey,
                            ParameterValue = ""
                        });
                    }
                } else {
                    Console.WriteLine();
                    Console.WriteLine($"Configuring {templateSummary.Description} Parameters");
                    foreach(var missingParameter in missingParameters) {
                        if(missingParameter.ParameterConstraints?.AllowedValues.Any() ?? false) {
                            var enteredValue = settings.PromptChoice(
                                $"{missingParameter.Description ?? missingParameter.ParameterKey}",
                                missingParameter.ParameterConstraints.AllowedValues
                            );
                            result.Add(new Parameter {
                                ParameterKey = missingParameter.ParameterKey,
                                ParameterValue = enteredValue
                            });
                        } else {

                            // TODO (2019-08-09, bjorg): add pattern and constraint description
                            var enteredValue = settings.PromptString($"{missingParameter.Description ?? missingParameter.ParameterKey}", missingParameter.DefaultValue) ?? "";
                            result.Add(new Parameter {
                                ParameterKey = missingParameter.ParameterKey,
                                ParameterValue = enteredValue
                            });
                        }
                    }
                    Console.WriteLine();
                }
            }

            // NOTE (2019-06-06, bjorg): extraneous parameters are ignored as they might be relevant to the LambdaSharp.Core initialization

            // return the collected paramaters
            return result;
        }

        protected async Task CheckApiGatewayRole(Settings settings) {

            // retrieve the CloudWatch/X-Ray role from the API Gateway account
            Console.WriteLine("=> Checking API Gateway role");
            var account = await settings.ApiGatewayClient.GetAccountAsync(new GetAccountRequest());
            var role = await GetOrCreateRole(account.CloudwatchRoleArn?.Split('/').Last() ?? DEFAULT_API_GATEWAY_ROLE);

            // check if the role has the expected managed policies; if not, attach them
            var attachedPolicies = (await settings.IamClient.ListAttachedRolePoliciesAsync(new ListAttachedRolePoliciesRequest {
                RoleName = role.RoleName
            })).AttachedPolicies;
            await CheckOrAttachPolicy("arn:aws:iam::aws:policy/service-role/AmazonAPIGatewayPushToCloudWatchLogs");
            await CheckOrAttachPolicy("arn:aws:iam::aws:policy/AWSXrayWriteOnlyAccess");

            // update API Gateway Account role if needed
            if(account.CloudwatchRoleArn != role.Arn) {
                Console.WriteLine("=> Updating API Gateway role");
                while(true) {
                    try {
                        var response = await settings.ApiGatewayClient.UpdateAccountAsync(new UpdateAccountRequest {
                            PatchOperations = new List<PatchOperation> {
                                new PatchOperation {
                                    Op = Amazon.APIGateway.Op.Replace,
                                    Path = "/cloudwatchRoleArn",
                                    Value = role.Arn
                                }
                            }
                        });
                        break;
                    } catch(BadRequestException) {
                        Console.WriteLine("=> Waiting for new API Gateway role to become available, trying again in 5 seconds (this may take up 30 seconds)");
                    } catch(TooManyRequestsException) {
                        Console.WriteLine("=> Waiting for API Gateway to stop throttling, trying again in 5 seconds (this may take up 30 seconds)");
                    }
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
            }

            // local functions
            async Task CheckOrAttachPolicy(string managedPolicyArn) {

                // check if managed policy is already attached; it not, attach it
                if(!attachedPolicies.Any(policy => policy.PolicyArn == managedPolicyArn)) {
                    Console.WriteLine($"=> Attaching managed policy to API Gateway role: {managedPolicyArn}");
                    await settings.IamClient.AttachRolePolicyAsync(new AttachRolePolicyRequest {
                        PolicyArn = managedPolicyArn,
                        RoleName = role.RoleName
                    });
                }
            }

            async Task<Role> GetOrCreateRole(string roleName) {

                // attempt to resolve the given role by name
            again:
                try {
                    return (await settings.IamClient.GetRoleAsync(new GetRoleRequest {
                        RoleName = roleName
                    })).Role;
                } catch(NoSuchEntityException) {

                    // check if we looked up a custom name; if so, we need to fallback to the default role and check again
                    if(roleName != DEFAULT_API_GATEWAY_ROLE) {
                        roleName = DEFAULT_API_GATEWAY_ROLE;
                        goto again;
                    }

                    // IAM role not found, fallthrough to the next step
                }

                // only create the LambdaSharp API Gateway Role when the account has no role or the role no longer exists
                Console.WriteLine("=> Creating API Gateway role");
                return (await settings.IamClient.CreateRoleAsync(new CreateRoleRequest {
                    RoleName = DEFAULT_API_GATEWAY_ROLE,
                    Description = "API Gateway Role for LambdaSharp modules",
                    AssumeRolePolicyDocument = @"{""Version"":""2012-10-17"",""Statement"":[{""Sid"": ""ApiGatewayPrincipal"",""Effect"":""Allow"",""Principal"":{""Service"":""apigateway.amazonaws.com""},""Action"":""sts:AssumeRole""}]}"
                })).Role;
            }
        }
    }
}

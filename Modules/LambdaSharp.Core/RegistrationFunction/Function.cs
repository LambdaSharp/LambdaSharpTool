/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2021
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
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using LambdaSharp.Core.Registrations;
using LambdaSharp.Core.RollbarApi;
using LambdaSharp.CustomResource;
using LambdaSharp.Exceptions;

namespace LambdaSharp.Core.RegistrationFunction {

    public class RegistrationResourceProperties {

        //--- Properties ---
        public string? ResourceType { get; set; }

        #region --- LambdaSharp::Registration::Module ---
        public string? ModuleId { get; set; }
        public string? ModuleInfo { get; set; }

        // NOTE (2020-07-27, bjorg): this property was replaced by `ModuleInfo`
        public string? Module { get; set; }
        #endregion

        #region --- LambdaSharp::Registration::Function ---
        // public string? ModuleId { get; set; }
        public string? FunctionId { get; set; }
        public string? FunctionName { get; set; }
        public string? FunctionLogGroupName { get; set; }
        public int FunctionMaxMemory { get; set; }
        public int FunctionMaxDuration { get; set; }
        public string? FunctionPlatform { get; set; }
        public string? FunctionFramework { get; set; }
        public string? FunctionLanguage { get; set; }
        #endregion

        #region --- LambdaSharp::Registration::App ---
        // public string? ModuleId { get; set; }
        public string? AppId { get; set; }
        public string? AppName { get; set; }
        public string? AppLogGroup { get; set; }
        public string? AppPlatform { get; set; }
        public string? AppFramework { get; set; }
        public string? AppLanguage { get; set; }
        #endregion

        //--- Methods ---
        public string? GetModuleInfo() => ModuleInfo ?? Module;

        public string? GetModuleFullName() {
            var moduleInfo = GetModuleInfo();
            if(moduleInfo == null) {
                return null;
            }
            var index = moduleInfo.IndexOfAny(new[] { ':', '@' });
            if(index < 0) {
                return moduleInfo;
            }
            return moduleInfo.Substring(0, index);
        }

        public string? GetModuleNamespace() => GetModuleFullName()?.Split('.', 2)[0];
        public string? GetModuleName() => GetModuleFullName()?.Split('.', 2)[1];
    }

    public class RegistrationResourceAttributes {

        //--- Properties ---
        public string? Registration { get; set; }
    }

    public class RegistrarException : ALambdaException {

        //--- Constructors ---
        public RegistrarException(string format, params object[] args) : base(format, args) { }
    }

    public sealed class Function : ALambdaCustomResourceFunction<RegistrationResourceProperties, RegistrationResourceAttributes> {

        //--- Constants ---
        private const int PROJECT_HASH_LENGTH = 6;

        //--- Fields ---
        private RegistrationTable? _registrations;
        private RollbarClient? _rollbarClient;
        private string? _rollbarProjectPattern;
        private string? _coreSecretsKey;

        //--- Constructors ---
        public Function() : base(new LambdaSharp.Serialization.LambdaSystemTextJsonSerializer()) { }

        //--- Properties ---
        private RegistrationTable Registrations => _registrations ?? throw new InvalidOperationException();
        private RollbarClient RollbarClient => _rollbarClient ?? throw new InvalidOperationException();
        private string CoreSecretsKey => _coreSecretsKey ?? throw new InvalidOperationException();
        private string RollbarProjectPattern => _rollbarProjectPattern ?? throw new InvalidOperationException();

        //--- Methods ---
        public async override Task InitializeAsync(LambdaConfig config) {
            var tableName = config.ReadDynamoDBTableName("RegistrationTable");
            _registrations = new RegistrationTable(new AmazonDynamoDBClient(), tableName);
            _rollbarClient = new RollbarClient(
                HttpClient,
                config.ReadText("RollbarReadAccessToken", defaultValue: null),
                config.ReadText("RollbarWriteAccessToken", defaultValue: null),
                message => LogInfo(message)
            );
            _rollbarProjectPattern = config.ReadText("RollbarProjectPattern");
            _coreSecretsKey = config.ReadText("CoreSecretsKey");

            // set default project pattern if none is specified
            if(string.IsNullOrEmpty(_rollbarProjectPattern)) {
                var rollbarProjectPrefix = config.ReadText("RollbarProjectPrefix");
                _rollbarProjectPattern = $"{rollbarProjectPrefix}{{ModuleFullName}}";
            }
        }

        public override async Task<Response<RegistrationResourceAttributes>> ProcessCreateResourceAsync(Request<RegistrationResourceProperties> request, CancellationToken cancellationToken) {
            var properties = request.ResourceProperties;

            // request validation
            if(properties == null) {
                throw new RegistrarException("missing resource properties");
            }
            if(properties.ModuleId == null) {
                throw new RegistrarException("missing module ID");
            }
            if(properties.ResourceType == null) {
                throw new RegistrarException("missing resource type");
            }

            // determine the kind of registration that is requested
            switch(properties.ResourceType) {
            case "LambdaSharp::Registration::Module": {
                    LogInfo($"Adding Module: Id={properties.ModuleId}, Info={properties.GetModuleInfo()}");
                    var owner = PopulateOwnerMetaData(properties);

                    // create new rollbar project
                    var rollbarProject = await RegisterRollbarProject(properties);
                    owner.RollbarProjectId = rollbarProject.ProjectId;
                    owner.RollbarAccessToken = rollbarProject.ProjectAccessToken;

                    // create module record
                    await Registrations.PutOwnerMetaDataAsync($"M:{properties.ModuleId}", owner);
                    return Respond($"registration:module:{properties.ModuleId}");
                }
            case "LambdaSharp::Registration::Function": {
                    if(properties.FunctionId == null) {
                        throw new RegistrarException("missing function ID");
                    }

                    // create function record
                    LogInfo($"Adding Function: Id={properties.FunctionId}, Name={properties.FunctionName}, ModuleId={properties.ModuleId}");
                    var owner = await Registrations.GetOwnerMetaDataAsync($"M:{properties.ModuleId}");
                    if(owner == null) {
                        throw new RegistrarException("no registration found for module {0}", properties.ModuleId);
                    }
                    owner = PopulateOwnerMetaData(properties, owner);
                    await Registrations.PutOwnerMetaDataAsync($"F:{properties.FunctionId}", owner);
                    return Respond($"registration:function:{properties.FunctionId}");
                }
            case "LambdaSharp::Registration::App": {
                    if(properties.AppId == null) {
                        throw new RegistrarException("missing app ID");
                    }

                    // create function record
                    LogInfo($"Adding App: Id={properties.AppId}, Name={properties.AppName}, ModuleId={properties.ModuleId}");
                    var owner = await Registrations.GetOwnerMetaDataAsync($"M:{properties.ModuleId}");
                    if(owner == null) {
                        throw new RegistrarException("no registration found for module {0}", properties.ModuleId);
                    }
                    owner = PopulateOwnerMetaData(properties, owner);
                    await Registrations.PutOwnerMetaDataAsync($"L:{properties.AppLogGroup}", owner);
                    return Respond($"registration:app:{properties.AppId}");
                }
            default:
                throw new RegistrarException("unrecognized resource type: {0}", properties.ResourceType);
            }
        }

        public override async Task<Response<RegistrationResourceAttributes>> ProcessDeleteResourceAsync(Request<RegistrationResourceProperties> request, CancellationToken cancellationToken) {
            var properties = request.ResourceProperties;

            // request validation
            if(properties == null) {
                throw new RegistrarException("missing resource properties");
            }
            if(properties.ModuleId == null) {
                throw new RegistrarException("missing module ID");
            }

            // determine the kind of de-registration that is requested
            switch(properties.ResourceType) {
            case "LambdaSharp::Registration::Module": {

                    // delete old rollbar project

                    // TODO (2018-10-22, bjorg): only delete rollbar project if ALL registrations have been deleted

                    // if(_rollbarClient.HasTokens) {
                    //     var owner = await _registrations.GetOwnerMetaDataAsync($"M:{properties.ModuleId}");
                    //     try {
                    //         if(owner.RollbarProjectId > 0) {
                    //             await _rollbarClient.DeleteProject(owner.RollbarProjectId);
                    //         }
                    //     } catch(Exception e) {
                    //         LogErrorAsWarning(e, "failed to delete rollbar project: {0}", owner.RollbarProjectId);
                    //     }
                    // }

                    // delete module record
                    LogInfo($"Removing Module: Id={properties.ModuleId}, Info={properties.GetModuleInfo()}");
                    await Registrations.DeleteOwnerMetaDataAsync($"M:{properties.ModuleId}");
                    break;
                }
            case "LambdaSharp::Registration::Function": {
                    if(properties.FunctionId == null) {
                        throw new RegistrarException("missing function ID");
                    }

                    // delete function record
                    LogInfo($"Removing Function: Id={properties.FunctionId}, Name={properties.FunctionName}, ModuleId={properties.ModuleId}");
                    await Registrations.DeleteOwnerMetaDataAsync($"F:{properties.FunctionId}");
                    break;
                }
            case "LambdaSharp::Registration::App": {
                    if(properties.AppId == null) {
                        throw new RegistrarException("missing app ID");
                    }

                    // delete function record
                    LogInfo($"Removing App: Id={properties.AppId}, Name={properties.AppName}, ModuleId={properties.ModuleId}");
                    await Registrations.DeleteOwnerMetaDataAsync($"L:{properties.AppLogGroup}");
                    break;
                }
            default:

                // nothing to do since we didn't process this request successfully in the first place!
                break;
            }
            return new Response<RegistrationResourceAttributes>();
        }

        public override async Task<Response<RegistrationResourceAttributes>> ProcessUpdateResourceAsync(Request<RegistrationResourceProperties> request, CancellationToken cancellationToken) {

            // request validation
            if(request.PhysicalResourceId == null) {
                throw new RegistrarException("missing physical id");
            }
            var properties = request.ResourceProperties;
            if(properties == null) {
                throw new RegistrarException("missing resource properties");
            }
            if(properties.ModuleId == null) {
                throw new RegistrarException("missing module ID");
            }
            if(properties.ResourceType == null) {
                throw new RegistrarException("missing resource type");
            }

            // determine the kind of registration that is requested
            switch(properties.ResourceType) {
            case "LambdaSharp::Registration::Module": {

                    // update module record
                    LogInfo($"Updating Module: Id={properties.ModuleId}, Info={properties.GetModuleInfo()}");
                    var owner = PopulateOwnerMetaData(properties);
                    await Registrations.PutOwnerMetaDataAsync($"M:{properties.ModuleId}", owner);
                    return Respond(request.PhysicalResourceId);
                }
            case "LambdaSharp::Registration::Function": {
                    if(properties.FunctionId == null) {
                        throw new RegistrarException("missing function ID");
                    }

                    // update function record
                    LogInfo($"Updating Function: Id={properties.FunctionId}, Name={properties.FunctionName}, ModuleId={properties.ModuleId}");
                    var owner = await Registrations.GetOwnerMetaDataAsync($"M:{properties.ModuleId}");
                    if(owner == null) {
                        throw new RegistrarException("no registration found for module {0}", properties.ModuleId);
                    }
                    owner = PopulateOwnerMetaData(properties, owner);
                    await Registrations.PutOwnerMetaDataAsync($"F:{properties.FunctionId}", owner);
                    return Respond(request.PhysicalResourceId);
                }
            case "LambdaSharp::Registration::App": {
                    if(properties.AppId == null) {
                        throw new RegistrarException("missing app ID");
                    }

                    // update function record
                    LogInfo($"Updating App: Id={properties.AppId}, Name={properties.AppName}, ModuleId={properties.ModuleId}");
                    var owner = await Registrations.GetOwnerMetaDataAsync($"M:{properties.ModuleId}");
                    if(owner == null) {
                        throw new RegistrarException("no registration found for module {0}", properties.ModuleId);
                    }
                    owner = PopulateOwnerMetaData(properties, owner);
                    await Registrations.PutOwnerMetaDataAsync($"L:{properties.AppLogGroup}", owner);
                    return Respond(request.PhysicalResourceId);
                }
            default:
                throw new RegistrarException("unrecognized resource type: {0}", properties.ResourceType);
            }
        }

        private Response<RegistrationResourceAttributes> Respond(string registration)
            => new Response<RegistrationResourceAttributes> {
                PhysicalResourceId = registration,
                Attributes = new RegistrationResourceAttributes {
                    Registration = registration
                }
            };

        private OwnerMetaData PopulateOwnerMetaData(RegistrationResourceProperties properties, OwnerMetaData? owner = null) {
            if(owner == null) {
                owner = new OwnerMetaData();
            }

            // support pre-0.8 notation where only 'Module' is present in the properties (no 'ModuleInfo')
            string? moduleInfo = null;
            string? module = null;
            if(properties.ModuleInfo != null) {
                moduleInfo = properties.ModuleInfo;
                module = properties.ModuleInfo.Split(':', 2)[0];
            } else if(properties.Module != null) {
                moduleInfo = properties.Module;
                module = properties.Module?.Split(':', 2)[0];
            }

            // create/update owner record
            owner.ModuleId = properties.ModuleId ?? owner.ModuleId;
            owner.Module = module ?? owner.Module;
            owner.ModuleInfo = moduleInfo ?? owner.ModuleInfo;

            // function record
            owner.FunctionId = properties.FunctionId ?? owner.FunctionId;
            owner.FunctionName = properties.FunctionName ?? owner.FunctionName;
            owner.FunctionLogGroupName = properties.FunctionLogGroupName ?? owner.FunctionLogGroupName;
            owner.FunctionPlatform = properties.FunctionPlatform ?? owner.FunctionPlatform;
            owner.FunctionFramework = properties.FunctionFramework ?? owner.FunctionFramework;
            owner.FunctionLanguage = properties.FunctionLanguage ?? owner.FunctionLanguage;
            owner.FunctionMaxMemory = (properties.FunctionMaxMemory != 0)
                ? properties.FunctionMaxMemory
                : owner.FunctionMaxMemory;
            owner.FunctionMaxDuration = (TimeSpan.FromSeconds(properties.FunctionMaxDuration) != TimeSpan.Zero)
                ? TimeSpan.FromSeconds(properties.FunctionMaxDuration)
                : TimeSpan.FromSeconds(owner.FunctionMaxDuration.TotalSeconds);

            // app record
            owner.AppId = properties.AppId ?? owner.AppId;
            owner.AppName = properties.AppName ?? owner.AppName;
            owner.AppLogGroup = properties.AppLogGroup ?? owner.AppLogGroup;
            owner.AppPlatform = properties.AppPlatform ?? owner.AppPlatform;
            owner.AppFramework = properties.AppFramework ?? owner.AppFramework;
            owner.AppLanguage = properties.AppLanguage ?? owner.AppLanguage;
            return owner;
        }

        private async Task<(int ProjectId, string? ProjectAccessToken)> RegisterRollbarProject(RegistrationResourceProperties properties) {
            if(!RollbarClient.HasTokens) {
                return (ProjectId: 0, ProjectAccessToken: null);
            }

            // generate the Rollbar project name
            var name = Regex.Replace(_rollbarProjectPattern, @"\{(?!\!)[^\}]+\}", match => {
                var value = match.ToString();
                switch(value) {
                case "{ModuleFullName}":
                    return properties.GetModuleFullName();
                case "{ModuleNamespace}":
                    return properties.GetModuleNamespace();
                case "{ModuleName}":
                    return properties.GetModuleName();
                case "{ModuleId}":
                    return properties.ModuleId;
                case "{ModuleIdNoTierPrefix}":
                    return string.IsNullOrEmpty(Info.DeploymentTier)
                        ? properties.ModuleId
                        : properties.ModuleId?.Substring(Info.DeploymentTier.Length + 1);
                default:

                    // remove curly braces for unrecognized placeholders
                    return value.Substring(1, value.Length - 2);
                }
            });

            // NOTE (2020-02-19, bjorg): Rollbar projects cannot exceed 32 characters
            if(name.Length > 32) {
                using(var crypto = new SHA256Managed()) {
                    var hash = string.Concat(crypto.ComputeHash(Encoding.UTF8.GetBytes(name)).Select(x => x.ToString("X2")));

                    // keep first X characters for original project name, append (32-X) characters from the hash
                    name = name.Substring(0, 32 - PROJECT_HASH_LENGTH) + hash.Substring(0, PROJECT_HASH_LENGTH);
                }
            }

            // find or create Rollbar project
            var project = await RollbarClient.FindProjectByName(name)
                ?? await RollbarClient.CreateProject(name);

            // retrieve access token for Rollbar project
            var tokens = await RollbarClient.ListProjectTokens(project.Id);
            var token = tokens.FirstOrDefault(t => t.Name == "post_server_item").AccessToken;
            if(token == null) {
                throw new RegistrarException("internal error: unable to retrieve token for new Rollbar project");
            }
            return (ProjectId: project.Id, ProjectAccessToken: await EncryptSecretAsync(token, CoreSecretsKey));
        }
    }
}

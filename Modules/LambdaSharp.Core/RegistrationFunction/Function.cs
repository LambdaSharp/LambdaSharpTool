/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2019
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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.Lambda.Core;
using LambdaSharp;
using LambdaSharp.Core.Registrations;
using LambdaSharp.Core.RollbarApi;
using LambdaSharp.CustomResource;
using LambdaSharp.Exceptions;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace LambdaSharp.Core.RegistrationFunction {

    public class RegistrationResourceProperties {

        //--- Properties ---
        public string ResourceType { get; set; }
        public string Module { get; set; }
        public string ModuleId { get; set; }
        public string FunctionId { get; set; }
        public string FunctionName { get; set; }
        public string FunctionLogGroupName { get; set; }
        public int FunctionMaxMemory { get; set; }
        public int FunctionMaxDuration { get; set; }
        public string FunctionPlatform { get; set; }
        public string FunctionFramework { get; set; }
        public string FunctionLanguage { get; set; }

        //--- Methods ---
        public string GetModuleFullName() {
            if(Module == null) {
                return null;
            }
            var index = Module.IndexOfAny(new[] { ':', '@' });
            if(index < 0) {
                return Module;
            }
            return Module.Substring(0, index);
        }
    }

    public class RegistrationResourceAttributes {

        //--- Properties ---
        public string Registration { get; set; }
    }

    public class RegistrarException : ALambdaException {

        //--- Constructors ---
        public RegistrarException(string format, params object[] args) : base(format, args) { }
    }

    public class Function : ALambdaCustomResourceFunction<RegistrationResourceProperties, RegistrationResourceAttributes> {

        //--- Fields ---
        private RegistrationTable _registrations;
        private RollbarClient _rollbarClient;
        private string _rollbarProjectPrefix;
        private string _coreSecretsKey;

        //--- Methods ---
        public async override Task InitializeAsync(LambdaConfig config) {
            var tableName = config.ReadDynamoDBTableName("RegistrationTable");
            _registrations = new RegistrationTable(new AmazonDynamoDBClient(), tableName);
            _rollbarClient = new RollbarClient(
                config.ReadText("RollbarReadAccessToken", defaultValue: null),
                config.ReadText("RollbarWriteAccessToken", defaultValue: null),
                message => LogInfo(message)
            );
            _rollbarProjectPrefix = config.ReadText("RollbarProjectPrefix");
            _coreSecretsKey = config.ReadText("CoreSecretsKey");
        }

        public override async Task<Response<RegistrationResourceAttributes>> ProcessCreateResourceAsync(Request<RegistrationResourceProperties> request) {
            var properties = request.ResourceProperties;

            // determine the kind of registration that is requested
            switch(request.ResourceProperties.ResourceType) {
            case "LambdaSharp::Registration::Module": {
                    LogInfo($"Adding Module: Id={properties.ModuleId}, Info={properties.Module}");
                    var owner = PopulateOwnerMetaData(properties);

                    // create new rollbar project
                    if(_rollbarClient.HasTokens) {
                        var name = _rollbarProjectPrefix + request.ResourceProperties.GetModuleFullName();
                        var project = await _rollbarClient.FindProjectByName(name)
                            ?? await _rollbarClient.CreateProject(name);
                        var tokens = await _rollbarClient.ListProjectTokens(project.Id);
                        var token = tokens.First(t => t.Name == "post_server_item").AccessToken;
                        owner.RollbarProjectId = project.Id;
                        owner.RollbarAccessToken = await EncryptSecretAsync(token, _coreSecretsKey);
                    }

                    // create owner record
                    await _registrations.PutOwnerMetaDataAsync($"M:{owner.ModuleId}", owner);
                    return Respond($"registration:module:{properties.ModuleId}");
                }
            case "LambdaSharp::Registration::Function": {
                    LogInfo($"Adding Function: Id={properties.FunctionId}, Name={properties.FunctionName}");
                    var owner = await _registrations.GetOwnerMetaDataAsync($"M:{properties.ModuleId}");
                    if(owner == null) {
                        throw new RegistrarException("no registration found for module {0}", properties.ModuleId);
                    }
                    owner = PopulateOwnerMetaData(properties, owner);
                    await _registrations.PutOwnerMetaDataAsync($"F:{owner.FunctionId}", owner);
                    return Respond($"registration:function:{properties.FunctionId}");
                }
            default:
                throw new RegistrarException("unrecognized resource type: {0}", request.ResourceType);
            }
        }

        public override async Task<Response<RegistrationResourceAttributes>> ProcessDeleteResourceAsync(Request<RegistrationResourceProperties> request) {
            var properties = request.ResourceProperties;
            switch(request.ResourceProperties.ResourceType) {
            case "LambdaSharp::Registration::Module": {
                    LogInfo($"Removing Module: Id={properties.ModuleId}, Info={properties.Module}");

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

                    // delete owner record
                    await _registrations.DeleteOwnerMetaDataAsync($"M:{properties.ModuleId}");
                    break;
                }
            case "LambdaSharp::Registration::Function": {
                    LogInfo($"Removing Function: Id={properties.FunctionId}, Name={properties.FunctionName}, LogGroup={properties.FunctionLogGroupName}");
                    await _registrations.DeleteOwnerMetaDataAsync($"F:{properties.FunctionId}");
                    break;
                }
            default:

                // nothing to do since we didn't process this request successfully in the first place!
                break;
            }
            return new Response<RegistrationResourceAttributes>();
        }

        public override async Task<Response<RegistrationResourceAttributes>> ProcessUpdateResourceAsync(Request<RegistrationResourceProperties> request)
            => Respond(request.PhysicalResourceId);

        private Response<RegistrationResourceAttributes> Respond(string registration)
            => new Response<RegistrationResourceAttributes> {
                PhysicalResourceId = registration,
                Attributes = new RegistrationResourceAttributes {
                    Registration = registration
                }
            };

        private OwnerMetaData PopulateOwnerMetaData(RegistrationResourceProperties properties, OwnerMetaData owner = null) {
            if(owner == null) {
                owner = new OwnerMetaData();
            }
            owner.ModuleId = properties.ModuleId ?? owner.ModuleId;
            owner.Module = properties.Module ?? owner.Module;
            owner.FunctionId = properties.FunctionId ?? owner.FunctionId;
            owner.FunctionName = properties.FunctionName ?? owner.FunctionName;
            owner.FunctionLogGroupName = properties.FunctionLogGroupName ?? owner.FunctionLogGroupName;
            owner.FunctionPlatform = properties.FunctionPlatform ?? owner.FunctionPlatform;
            owner.FunctionFramework = properties.FunctionFramework ?? owner.FunctionFramework;
            owner.FunctionLanguage = properties.FunctionLanguage ?? owner.FunctionLanguage;
            owner.FunctionMaxMemory = Math.Max(properties.FunctionMaxMemory, owner.FunctionMaxMemory);
            owner.FunctionMaxDuration = TimeSpan.FromSeconds(Math.Max(properties.FunctionMaxDuration, owner.FunctionMaxDuration.TotalSeconds));
            return owner;
        }
    }
}

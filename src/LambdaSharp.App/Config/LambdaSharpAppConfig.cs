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
using System.Text;

namespace LambdaSharp.App.Config {

    /// <summary>
    /// The <see cref="LambdaSharpAppConfig"/> class is used to deserialize the settings for the <see cref="LambdaSharp.App.LambdaSharpAppClient"/> instance.
    /// </summary>
    public sealed class LambdaSharpAppConfig {

        //--- Fields ---
        private string _effectiveApiKey;
        private string _effectiveEventBusApiKey;

        //--- Properties ---

        /// <summary>
        /// The <see cref="ModuleId"/> property describes the stack name of the deployed LambdaSharp module.
        /// </summary>
        /// <value>The stack name of the deployed LambdaSharp module.</value>
        /// <example>
        /// Sample module ID:
        /// <code>DevTier-MyAcmeModule</code>
        /// </example>
        public string ModuleId { get; set; }

        /// <summary>
        /// The <see cref="ModuleInfo"/> property describes the LambdaSharp module name, version, and origin.
        /// </summary>
        /// <value>The LambdaSharp module name and version.</value>
        /// <example>
        /// Sample module name and version:
        /// <code>My.AcmeModule:1.0-Dev@origin</code>
        /// </example>
        public string ModuleInfo { get; set; }

        /// <summary>
        /// The <see cref="DeploymentTier"/> property describes the LambdaSharp deployment tier name. This property is empty for the default deployment tier.
        /// </summary>
        /// <value>The LambdaSharp deployment tier name.</value>
        /// <example>
        /// Sample deployment tier name:
        /// <code>MyTier</code>
        /// </example>
        public string DeploymentTier { get; set; }

        /// <summary>
        /// The <see cref="AppId"/> property describes the application identifier.
        /// </summary>
        /// <value>The application id.</value>
        public string AppId { get; set; }

        /// <summary>
        /// The <see cref="AppName"/> property describes the application name.
        /// </summary>
        /// <value>The application name.</value>
        public string AppName { get; set; }

        /// <summary>
        /// The <see cref="AppFramework"/> property describes the application execution framework.
        /// </summary>
        /// <value>The application framework.</value>
        public string AppFramework { get; set; }

        /// <summary>
        /// The <see cref="ApiKey"/> property holds the API key for the app API.
        /// </summary>
        /// <value>The API key for the app API.</value>
        public string ApiKey { get; set; }

        /// <summary>
        /// The <see cref="ApiUrl"/> property holds the URL for the app API.
        /// </summary>
        /// <value>The URL of the app API.</value>
        public string ApiUrl { get; set; }

        /// <summary>
        /// The <see cref="DevMode"/> property describes if the app API is running in dev mode (default: Disabled).
        /// </summary>
        /// <value>The dev mode setting.</value>
        public string DevMode { get; set; }

        /// <summary>
        /// The <see cref="AppVersionId"/> property describes app assembly version identifier.
        /// </summary>
        /// <value>The app assembly version GUID.</value>
        public string AppVersionId { get; set; }

        /// <summary>
        /// The <see cref="AppInstanceId"/> property describes app instance identifier.
        /// </summary>
        /// <value>The app assembly version GUID.</value>
        public string AppInstanceId { get; set; }

        /// <summary>
        /// The Git SHA from which the function was built from.
        /// </summary>
        /// <value>The Git SHA from the source code repository.</value>
        public string GitSha { get; set; }

        /// <summary>
        /// The Git branch from which the function was built form.
        /// </summary>
        /// <value>The Git branch from the source code repository.</value>
        public string GitBranch { get; set; }

        /// <summary>
        /// The <see cref="EventBusUrl"/> property holds the URL for the app event bus.
        /// </summary>
        /// <value>The URL of the app API.</value>
        public string EventBusUrl { get; set; }

        /// <summary>
        /// The <see cref="EventBusApiKey"/> property holds the API key for the event bus API.
        /// </summary>
        /// <value>The URL of the app API.</value>
        public string EventBusApiKey { get; set; }

        /// <summary>
        /// The <see cref="AppEventSource"/> property holds the configured event source value, or null when not set.
        /// </summary>
        /// <value>Configured event source name for the app instance, or null.</value>
        public string AppEventSource { get; set; }

        //--- Methods ---

        /// <summary>
        /// The <see cref="GetModuleFullName()"/> method return the module full name from the <see cref="ModuleInfo"/> property.
        /// </summary>
        public string GetModuleFullName() => ModuleInfo.Split(':', 2)[0];

        /// <summary>
        /// The <see cref="GetModuleFullName()"/> method return the module origin from the <see cref="ModuleInfo"/> property.
        /// </summary>
        public string GetModuleOrigin() => ModuleInfo.Split('@', 2)[1];

        /// <summary>
        /// Determines if the developer mode is enabled.
        /// </summary>
        /// <returns><code>true</code> when developer mode is enabled</returns>
        public bool IsDevModeEnabled() => DevMode == "Enabled";

        /// <summary>
        /// The <see cref="GetApiKey()"/> method return the API key for the app API depending on the status of dev mode.
        /// </summary>
        /// <returns>The app API key.</returns>
        public string GetApiKey() {
            if(IsDevModeEnabled()) {
                return ApiKey;
            }
            if(_effectiveApiKey == null) {

                // generate API key by combining the provided API key as a prefix and the app assembly version GUID as a suffix
                var prefix = AppVersionId;
                var suffix = Encoding.UTF8.GetString(Convert.FromBase64String(ApiKey));
                _effectiveApiKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(prefix + ":" + suffix));
            }
            return _effectiveApiKey;
        }

        /// <summary>
        /// The <see cref="GetEventBusApiKey()"/> method return the API key for the event bus API depending on the status of dev mode.
        /// </summary>
        /// <returns>The event bus API key.</returns>
        public string GetEventBusApiKey() {
            if(IsDevModeEnabled()) {
                return EventBusApiKey;
            }
            if(_effectiveEventBusApiKey == null) {

                // generate API key by combining the provided API key as a prefix and the app assembly version GUID as a suffix
                var prefix = AppVersionId;
                var suffix = Encoding.UTF8.GetString(Convert.FromBase64String(EventBusApiKey));
                _effectiveEventBusApiKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(prefix + ":" + suffix));
            }
            return _effectiveEventBusApiKey;
        }
    }
}

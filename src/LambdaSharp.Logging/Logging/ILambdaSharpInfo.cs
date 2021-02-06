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

namespace LambdaSharp.Logging {

    /// <summary>
    /// The <see cref="ILambdaSharpInfo"/> interface exposes information about the LambdaSharp environment.
    /// </summary>
    public interface ILambdaSharpInfo {

        //--- Properties ----

        /// <summary>
        /// The <see cref="ModuleId"/> property holds the CloudFormation stack name.
        /// </summary>
        /// <value>Name of the CloudFormation stack.</value>
        string ModuleId { get; }

        /// <summary>
        /// The module full name, version, and origin.
        /// </summary>
        string ModuleInfo { get; }

        /// <summary>
        /// The <see cref="FunctionName"/> property holds the name of the Lambda function.
        /// </summary>
        /// <value>Name of the Lambda function.</value>
        string FunctionName { get; }

        /// <summary>
        /// The <see cref="AppName"/> property holds the name of the app.
        /// </summary>
        /// <value>Name of the app.</value>
        string AppName { get; }

        /// <summary>
        /// The <see cref="AppId"/> property holds the app instance id.
        /// </summary>
        /// <value>Id of the app instance.</value>
        string AppId { get; }

        /// <summary>
        /// The <see cref="AppInstanceId"/> property describes app instance identifier.
        /// </summary>
        /// <value>The app assembly version GUID.</value>
        string AppInstanceId { get; }

        /// <summary>
        /// The deployment tier name.
        /// </summary>
        string DeploymentTier { get; }

        /// <summary>
        /// The <see cref="GitSha"/> property holds the optional Git SHA of the code.
        /// </summary>
        /// <value>Git SHA value or null.</value>
        string GitSha { get; }

        /// <summary>
        /// The <see cref="GitBranch"/> property hold the optional Git branch of the code.
        /// </summary>
        /// <value>Git branch name or null.</value>
        string GitBranch { get; }
    }
}

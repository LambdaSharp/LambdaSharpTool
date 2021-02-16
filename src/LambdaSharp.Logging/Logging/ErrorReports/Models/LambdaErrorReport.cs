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

using System.Collections.Generic;

namespace LambdaSharp.Logging.ErrorReports.Models {

    /// <summary>
    /// The <see cref="LambdaErrorReport"/> class defines a structured Lambda log entry
    /// for runtime errors and warnings.
    /// </summary>
    public class LambdaErrorReport : ALambdaLogRecord {

        //--- Constructors ---

        /// <summary>
        /// Create a new <see cref="LambdaErrorReport"/> instance.
        /// </summary>
        public LambdaErrorReport() {
            Type = "LambdaError";
            Version = "2020-05-05";
        }

        //--- Properties ---

        // Origin

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
        /// The <see cref="Module"/> property describes the LambdaSharp module name.
        /// </summary>
        /// <value>The LambdaSharp module name.</value>
        /// <example>
        /// Sample module name:
        /// <code>My.AcmeModule</code>
        /// </example>
        public string Module { get; set; }

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
        /// The <see cref="FunctionId"/> property describes the ID of the deployed Lambda function.
        /// </summary>
        /// <value>The ID of the deployed Lambda function.</value>
        /// <example>
        /// Sample function ID:
        /// <code>DevTier-MyAcmeModule-MyFunction-VDLETAGVFYT2</code>
        /// </example>
        public string FunctionId { get; set; }

        /// <summary>
        /// The <see cref="FunctionName"/> property describes the Lambda function name.
        /// </summary>
        /// <value>The Lambda function name.</value>
        /// <example>
        /// Sample function name:
        /// <code>MyFunction</code>
        /// </example>
        public string FunctionName { get; set; }

        /// <summary>
        /// The <see cref="AppId"/> property describes the application identifier.
        /// </summary>
        /// <value>The application identifier.</value>
        /// <example>
        /// Sample application identifier:
        /// <code>MyCloudFormationStack-MyApp</code>
        /// </example>
        public string AppId { get; set; }

        /// <summary>
        /// The <see cref="AppName"/> property describes the application name.
        /// </summary>
        /// <value>The application name.</value>
        /// <example>
        /// Sample application name:
        /// <code>MyApp</code>
        /// </example>
        public string AppName { get; set; }

        /// <summary>
        /// The <see cref="Platform"/> property describes the Lambda function or app execution platform.
        /// </summary>
        /// <value>The Lambda execution platform.</value>
        /// <example>
        /// Sample Lambda execution platform:
        /// <code>AWS Lambda (Unix 4.14.72.68)</code>
        /// </example>
        public string Platform { get; set; }

        /// <summary>
        /// The <see cref="Framework"/> property describes the Lambda function or app execution framework.
        /// </summary>
        /// <value>The Lambda execution framework.</value>
        /// <example>
        /// Sample Lambda execution framework:
        /// <code>dotnetcore2.2</code>
        /// </example>
        public string Framework { get; set; }

        /// <summary>
        /// The <see cref="Language"/> property describes the Lambda function or app implementation language.
        /// </summary>
        /// <value>The Lambda implementation language.</value>
        /// <example>
        /// Sample Lambda implementation language:
        /// <code>csharp</code>
        /// </example>
        public string Language { get; set; }

        /// <summary>
        /// The <see cref="GitSha"/> property holds the git SHA of the executing Lambda code or <c>null</c> if not provided.
        /// </summary>
        /// <value>The git SHA or <c>null</c>.</value>
        public string GitSha { get; set; }

        /// <summary>
        /// The <see cref="GitBranch"/> property holds the git branch name of the executing Lambda code or <c>null</c> if not provided.
        /// </summary>
        /// <value>The git branch name or <c>null</c>.</value>
        public string GitBranch { get; set; }

        // Occurrence

        /// <summary>
        /// The <see cref="RequestId"/> property holds the AWS request ID during which the error log entry was generated.
        /// </summary>
        /// <value>The AWS request ID.</value>
        public string RequestId { get; set; }

        /// <summary>
        /// The <see cref="Level"/> property describes the severity level of the error log entry.
        /// One of <c>WARNING</c>, <c>ERROR</c>, or <c>FATAL</c>.
        /// </summary>
        /// <value>The error severity level.</value>
        public string Level { get; set; }

        /// <summary>
        /// The <see cref="Fingerprint"/> property holds a unique value by which to group related LambdaError records.
        /// </summary>
        /// <value>The log entry fingerprint.</value>
        public string Fingerprint { get; set; }

        /// <summary>
        /// The <see cref="Timestamp"/> property holds the UNIX epoch in milliseconds when the error log entry was generated.
        /// </summary>
        /// <value>The UNIX epoch in milliseconds timestamp.</value>
        public long Timestamp { get; set; }

        /// <summary>
        /// The <see cref="Message"/> property holds the message of the error log entry.
        /// </summary>
        /// <value>The error log entry message.</value>
        public string Message { get; set; }

        /// <summary>
        /// The <see cref="Raw"/> property holds the unprocessed error log entry.
        /// </summary>
        /// <value>The unprocessed error log entry.</value>
        public string Raw { get; set; }

        /// <summary>
        /// The <see cref="Traces"/> property describes the error stack traces or <c>null</c> if none are provided.
        /// </summary>
        /// <value>Enumeration of error stack traces.</value>
        public IEnumerable<LambdaErrorReportStackTrace> Traces { get; set; }
    }
}

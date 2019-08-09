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

namespace LambdaSharp.Exceptions {

    /// <summary>
    /// The <see cref="ILambdaExceptionFingerprinter"/> interface is used to indicate that
    /// an exception type has custom fingerprinting logic. The exception fingerprint is used
    /// by the <see cref="ErrorReports.LambdaErrorReportGenerator"/> instance to allow a log
    /// aggregator to group together related errors.
    /// </summary>
    public interface ILambdaExceptionFingerprinter {

        //--- Properties ---

        /// <summary>
        /// The <see cref="FingerprintValue"/> property returns a deterministic fingerprint value
        /// that can be used to group related exceptions together.
        /// </summary>
        /// <value>The fingerprint value.</value>
        string FingerprintValue { get; }
    }
}

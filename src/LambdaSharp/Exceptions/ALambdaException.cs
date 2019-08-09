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
using System.Linq;

namespace LambdaSharp.Exceptions {

    /// <summary>
    /// The <see cref="ALambdaException"/> abstract class is the recommended base class
    /// for all runtime exceptions. It implements the <see cref="ILambdaExceptionFingerprinter.FingerprintValue"/>
    /// property to create a unique fingerprint from the exception type and the unformatted exception message.
    /// This allows the generated <see cref="ErrorReports.LambdaErrorReport"/> instances to be grouped together by a
    /// log aggregator.
    /// </summary>
    public abstract class ALambdaException : Exception, ILambdaExceptionFingerprinter {

        //--- Fields ---
        private readonly string _unformattedMessage;

        //--- Constructors ---

        /// <summary>
        /// Initializes a <see cref="ALambdaException"/> instance with the specified exception message and arguments.
        /// </summary>
        /// <param name="format">The exception message.</param>
        /// <param name="args">Optional arguments for the exception message.</param>
        protected ALambdaException(string format, params object[] args) : base(args.Any() ? string.Format(format, args) : format)
            => _unformattedMessage = format;

        /// <summary>
        /// Initializes a <see cref="ALambdaConfigException"/> instance with the specified exception message and inner exception.
        /// </summary>
        /// <param name="innerException">The inner exception instance.</param>
        /// <param name="format">The exception message.</param>
        /// <param name="args">Optional arguments for the exception message.</param>
        protected ALambdaException(Exception innerException, string format, params object[] args)
            : base(args.Any() ? string.Format(format, args) : format, innerException ?? new ArgumentNullException(nameof(innerException)))
            => _unformattedMessage = format;

        //--- ILambdaExceptionFingerprinter Members ---
        string ILambdaExceptionFingerprinter.FingerprintValue => $"{GetType()}:{_unformattedMessage}";
    }
}

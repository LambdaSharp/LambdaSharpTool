/*
 * MindTouch λ#
 * Copyright (C) 2018 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit mindtouch.com;
 * please review the licensing section.
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
using MindTouch.Rollbar.Data;

namespace MindTouch.Rollbar.Builders {

    public class ExceptionInfoBuilder {

        //--- Methods ---
        public ExceptionInfo CreateFromException(Exception exception, string description) {
            var className = exception.GetType().Name;
            return new ExceptionInfo(
                className,
                exception.Message,
                BuildDescription(exception, description)
            );
        }

        private static string BuildDescription(Exception exception, string description) {

            // use the exception message when no custom description (i.e. log4net message) is provided
            if(string.IsNullOrWhiteSpace(description)) {
                return exception.ToString();
            }

            // combine the custom description (i.e. log4net message) with the exception message
            return description + Environment.NewLine + exception;
        }
    }
}

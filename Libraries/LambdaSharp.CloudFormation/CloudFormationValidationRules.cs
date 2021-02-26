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
using System.Text.RegularExpressions;

namespace LambdaSharp.CloudFormation {

    public static class CloudFormationValidationRules {

        //--- Class Fields ---
        private static Regex _validResourceNameRegex = new Regex("[a-zA-Z][a-zA-Z0-9]*", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly HashSet<string> _reservedResourceNames = new HashSet<string> {
            "Alexa",
            "AMZN",
            "Amazon",
            "ASK",
            "AWS",
            "Custom",
            "Dev"
        };

        //--- Class Methods ---
        public static bool IsValidCloudFormationName(string name) => _validResourceNameRegex.IsMatch(name);

        public static bool IsReservedCloudFormationName(string name) => _reservedResourceNames.Contains(name);
    }
}
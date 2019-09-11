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

namespace LambdaSharp.Tool.Cli.Tier {

    internal abstract class ASettingsBase {

        //--- Fields ---
        private readonly Settings _settings;

        //--- Constructors ---
        protected ASettingsBase(Settings settings) {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        //--- Properties ---
        protected Settings Settings => _settings;
        protected static bool HasErrors => Settings.HasErrors;

        //--- Methods ---
        protected void LogWarn(string message) => Settings.LogWarn(message);
        protected void LogError(string message, Exception exception = null) => Settings.LogError(message, exception);
        protected void LogError(Exception exception) => Settings.LogError(exception);
    }
}

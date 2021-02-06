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

namespace LambdaSharp.App.EventBus.Actions {

    /// <summary>
    /// The abstract <see cref="AnAction"/> class is the used by all
    /// LambdaSharp App EventBus actions.
    /// </summary>
    public abstract class AnAction {

        //--- Properties ---

        /// <summary>
        /// The <see cref="Action"/> property holds the name of the action.
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// The <see cref="RequestId"/> property holds a unique identifier
        /// used to reference the action in an acknowledgment.
        /// </summary>
        public string RequestId { get; set; } = Guid.NewGuid().ToString();
    }
}
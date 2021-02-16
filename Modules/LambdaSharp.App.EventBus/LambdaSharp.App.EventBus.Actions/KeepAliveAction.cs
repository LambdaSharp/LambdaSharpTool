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

namespace LambdaSharp.App.EventBus.Actions {

    /// <summary>
    /// The <see cref="KeepAliveAction"/> class describes a recurring action
    /// that is emitted by the WebSocket server to keep the connection open.
    /// </summary>
    public sealed class KeepAliveAction : AnAction {

        //--- Constructors ---

        /// <summary>
        /// Create new instance of <see cref="KeepAliveAction"/>.
        /// </summary>
        public KeepAliveAction() => Action = "KeepAlive";
    }
}
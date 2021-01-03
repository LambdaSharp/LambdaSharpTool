﻿/*
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
    /// The <see cref="HelloAction"/> class is used by the LambdaSharp
    /// App EventBus client to announce the app.
    /// </summary>
    public sealed class HelloAction : AnAction {

        //--- Constructors ---

        /// <summary>
        /// Create new instance of <see cref="HelloAction"/>.
        /// </summary>
        public HelloAction() => Action = "Hello";
    }
}
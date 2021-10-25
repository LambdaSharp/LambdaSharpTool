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
using System.Collections.Generic;

namespace LambdaSharp.App.OmniBus.Events.Records {

    public sealed class ConnectionRecord {

        //--- Properties ---
        public string ConnectionId { get; set; }
        public string AppId { get; set; }
        public string SubscriptionArn { get; set; }
        public Dictionary<string, object> Claims { get; set; }
        public int RulesCounter { get; set; }
        public DateTimeOffset? Expiration { get; set; }
    }
}

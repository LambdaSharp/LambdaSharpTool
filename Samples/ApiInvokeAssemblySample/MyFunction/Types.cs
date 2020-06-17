/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2020
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
using System.Runtime.Serialization;

namespace ApiInvokeSample.MyFunction {

    public class Item {

        //--- Properties ---
        [DataMember(IsRequired = true)]
        public string Id { get; set; }

        [DataMember(IsRequired = true)]
        public string Value { get; set; }
    }

    public class AddItemRequest {

        //--- Properties ---
        [DataMember(IsRequired = true)]
        public string Value { get; set; }
    }

    public class AddItemResponse {

        //--- Properties ---
        [DataMember(IsRequired = true)]
        public string Id { get; set; }
    }

    public class GetItemsResponse {

        //--- Properties ---
        [DataMember(IsRequired = true)]
        public List<Item> Items = new List<Item>();
    }

    public class GetItemResponse {

        //--- Properties ---
        [DataMember(IsRequired = true)]
        public string Id { get; set; }

        [DataMember(IsRequired = true)]
        public string Value { get; set; }
    }

    public class DeleteItemResponse {

        //--- Properties ---
        [DataMember(IsRequired = true)]
        public bool Deleted;
    }
}
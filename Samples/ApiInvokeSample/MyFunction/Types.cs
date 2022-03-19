/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2022
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
using System.ComponentModel.DataAnnotations;

namespace ApiInvokeSample.MyFunction {

    public class Item {

        //--- Properties ---
        [Required]
        public string? Id { get; set; }

        [Required]
        public string? Value { get; set; }
    }

    public class AddItemRequest {

        //--- Properties ---
        [Required]
        public string? Value { get; set; }
    }

    public class AddItemResponse {

        //--- Properties ---
        [Required]
        public string? Id { get; set; }
    }

    public class GetItemsResponse {

        //--- Properties ---
        [Required]
        public List<Item> Items = new List<Item>();
    }

    public class GetItemResponse {

        //--- Properties ---
        [Required]
        public string? Id { get; set; }

        [Required]
        public string? Value { get; set; }
    }

    public class DeleteItemResponse {

        //--- Properties ---
        [Required]
        public bool Deleted;
    }
}
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
using System.Collections.Generic;
using System.Linq;
using Amazon.Lambda.Core;
using LambdaSharp.ApiGateway;
using Newtonsoft.Json;

namespace ApiInvokeSample.MyFunction {

    public interface ILogicDependencyProvider {

        //--- Methods ---
        Exception ThrowItemIdNotFound(string message);
    }

    public class Logic {

        //--- Types ---
        public class FilterOptions {

            //--- Properties ---
            [JsonProperty(PropertyName = "contains", Required = Required.DisallowNull)]
            public string Contains { get; set; }

            [JsonProperty(PropertyName = "offset", Required = Required.DisallowNull)]
            public int Offset { get; set; } = 0;

            [JsonProperty(PropertyName = "limit", Required = Required.DisallowNull)]
            public int Limit { get; set; } = 10;
        }

        //--- Fields ---
        private ILogicDependencyProvider _provider;
        private List<Item> _items = new List<Item>();

        //--- Constructors ---
        public Logic(ILogicDependencyProvider provider) {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        //--- Constructors ---
        public AddItemResponse AddItem(AddItemRequest request) {

            // add new item to list
            var item = new Item {
                Id = Guid.NewGuid().ToString("N"),
                Value = request.Value
            };
            _items.Add(item);

            // respond with new item ID
            return new AddItemResponse {
                Id = item.Id
            };
        }

        public GetItemsResponse GetItems([FromUri] FilterOptions options) {

            // response with list of all items
            return new GetItemsResponse {
                Items = new List<Item>(_items.Where(item => (options.Contains == null) || item.Value.Contains(options.Contains)).Skip(options.Offset).Take(options.Limit))
            };
        }

        public GetItemResponse GetItem(string id) {

            // find matching item
            var found = _items.FirstOrDefault(item => item.Id == id);

            // abort if no item was found
            if(found == null) {
                throw _provider.ThrowItemIdNotFound($"Item {id} does not exist");
            }

            // respond with found item
            return new GetItemResponse {
                Id = found.Id,
                Value = found.Value
            };
        }

        public DeleteItemResponse DeleteItem(string id) {

            // remove item matching ID
            var count = _items.RemoveAll(item => item.Id == id);

            // respond with deletion result
            return new DeleteItemResponse {
                Deleted = (count > 0)
            };
        }
    }
}
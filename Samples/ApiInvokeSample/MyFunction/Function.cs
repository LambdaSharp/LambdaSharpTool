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
using System.Linq;
using System.Threading.Tasks;
using LambdaSharp;
using LambdaSharp.ApiGateway;

namespace ApiInvokeSample.MyFunction {

    public sealed class Function : ALambdaApiGatewayFunction {

        //--- Fields ---
        private List<Item> _items = new List<Item>();

        //--- Constructors ---
        public Function() : base(new LambdaSharp.Serialization.LambdaSystemTextJsonSerializer()) { }

        //--- Methods ---
        public override Task InitializeAsync(LambdaConfig config)
            => Task.CompletedTask;

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

        public GetItemsResponse GetItems(string contains = null, int offset = 0, int limit = 10) {

            // response with list of all items
            return new GetItemsResponse {
                Items = new List<Item>(_items.Where(item => (contains == null) || item.Value.Contains(contains)).Skip(offset).Take(limit))
            };
        }

        public GetItemResponse GetItem(string id) {

            // find matching item
            var found = _items.FirstOrDefault(item => item.Id == id);

            // abort if no item was found
            if(found == null) {
                throw AbortNotFound($"Item {id} does not exist");
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
/*
 * MindTouch λ#
 * Copyright (C) 2018-2019 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit mindtouch.com;
 * please review the licensing section.
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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using LambdaSharp;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace ApiInvokeSample.MyFunction {

    public class Function : ALambdaRestApiFunction {

        //--- Types ---

        //--- Fields ---
        private List<Item> _items = new List<Item>();

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

        public GetItemsResponse GetItems() {

            // response with list of all items
            return new GetItemsResponse {
                Items = new List<Item>(_items)
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
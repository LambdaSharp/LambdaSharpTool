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
using System.Collections;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using LambdaSharp.CloudFormation.Template.Serialization;

namespace LambdaSharp.CloudFormation.Template {

    [JsonConverter(typeof(CloudFormationListConverter))]
    public class CloudFormationList : ACloudFormationExpression, IEnumerable, IEnumerable<ACloudFormationExpression> {

        //--- Fields ---
        private readonly List<ACloudFormationExpression> _items;

        //--- Constructors ---
        public CloudFormationList() => _items = new List<ACloudFormationExpression>();

        public CloudFormationList(IEnumerable<ACloudFormationExpression> items)
            => _items = new List<ACloudFormationExpression>(items);

        //--- Properties ---
        public int Count => _items.Count;

        //--- Operators ---
        public ACloudFormationExpression this[int index] {
            get => _items[index];
            set => _items[index] = value ?? throw new ArgumentNullException(nameof(value));
        }

        //--- Methods ---
        public void Add(ACloudFormationExpression expression) => _items.Add(expression);

        //--- IEnumerable Members ---
        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

        //--- IEnumerable<AExpression> Members ---
        IEnumerator<ACloudFormationExpression> IEnumerable<ACloudFormationExpression>.GetEnumerator() => _items.GetEnumerator();
    }
}
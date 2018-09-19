/*
 * MindTouch λ#
 * Copyright (C) 2018 MindTouch, Inc.
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
using System.Collections;
using System.Collections.Generic;

namespace MindTouch.LambdaSharp.Tool.Model {

    public class CustomResource : Humidifier.Resource, IDictionary<string, object> {

        //--- Fields ---
        private readonly string _typeName;
        private readonly IDictionary<string, object> _properties;

        //--- Constructors ---
        public CustomResource(string typeName, IDictionary<string, object> properties) {
            _typeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
            _properties = properties ?? throw new ArgumentNullException(nameof(properties));
        }

        public CustomResource(string typeName) : this(typeName, new Dictionary<string, object>()) { }

        //--- Properties ---
        public override string AWSTypeName => _typeName;

        public object this[string key] {
            get => _properties[key];
            set => _properties[key] = value;
        }

        public ICollection<string> Keys => _properties.Keys;
        public ICollection<object> Values => _properties.Values;
        public int Count => _properties.Count;
        public bool IsReadOnly => _properties.IsReadOnly;

        //--- Methods ---
        public void Add(string key, object value) => _properties.Add(key, value);
        public void Add(KeyValuePair<string, object> item) => _properties.Add(item);
        public void Clear() => _properties.Clear();
        public bool Contains(KeyValuePair<string, object> item) => _properties.Contains(item);
        public bool ContainsKey(string key) => _properties.ContainsKey(key);
        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) => CopyTo(array, arrayIndex);
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _properties.GetEnumerator();
        public bool Remove(string key) => _properties.Remove(key);
        public bool Remove(KeyValuePair<string, object> item) => _properties.Remove(item);
        public bool TryGetValue(string key, out object value) => _properties.TryGetValue(key, out value);

        //--- IEnumerable Members ---
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
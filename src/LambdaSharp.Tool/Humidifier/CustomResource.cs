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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LambdaSharp.Tool;
using LambdaSharp.Tool.Model;

namespace Humidifier {
    using static ModelFunctions;

    public class CustomResource : Resource, IDictionary<string, object>, IDictionary {

        //--- Fields ---
        private readonly string _awsTypeName;
        private readonly string _originalTypeName;
        private readonly IDictionary<string, object> _properties;

        //--- Constructors ---
        public CustomResource(string typeName, IDictionary<string, object> properties) {
            _originalTypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
            _properties = new Dictionary<string, object>();

            // resolve custom resource service token
            if(
                !ResourceMapping.IsCloudFormationType(typeName)
                && !typeName.StartsWith("Custom::", StringComparison.Ordinal)
            ) {
                if(!_properties.ContainsKey("ServiceToken")) {
                    _properties["ServiceToken"] = FnImportValue(FnSub($"${{DeploymentPrefix}}{typeName}"));
                }
                if(!_properties.ContainsKey("ResourceType")) {
                    _properties["ResourceType"] = typeName;
                }
                _awsTypeName = "Custom::" + typeName.Replace("::", "");
            } else {
                _awsTypeName = typeName;
            }
            if(properties != null) {
                foreach(var kv in properties.Where(entry => entry.Value != null)) {
                    _properties.Add(kv.Key, kv.Value);
                }
            }
        }

        public CustomResource(string typeName) : this(typeName, new Dictionary<string, object>()) { }

        //--- Properties ---
        public override string AWSTypeName => _awsTypeName;
        public string OriginalTypeName => _originalTypeName;
        public object this[string key] {
            get => _properties[key];
            set {
                if(value != null) {
                    _properties[key] = value;
                } else {
                    _properties.Remove(key);
                }
            }
        }
        public ICollection<string> Keys => _properties.Keys;
        public ICollection<object> Values => _properties.Values;
        public int Count => _properties.Count;
        public bool IsReadOnly => _properties.IsReadOnly;

        //--- Methods ---
        public void Add(string key, object value) {
            if(value != null) {
                _properties.Add(key, value);
            }
        }

        public void Add(KeyValuePair<string, object> item) => Add(item.Key, item.Value);
        public void Clear() => _properties.Clear();
        public bool Contains(KeyValuePair<string, object> item) => _properties.Contains(item);
        public bool ContainsKey(string key) => _properties.ContainsKey(key);
        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) => CopyTo(array, arrayIndex);
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _properties.GetEnumerator();
        public bool Remove(string key) => _properties.Remove(key);
        public bool Remove(KeyValuePair<string, object> item) => _properties.Remove(item);
        public bool TryGetValue(string key, out object value) => _properties.TryGetValue(key, out value);

        //--- IDictionary Members ---
        bool IDictionary.IsFixedSize => false;
        bool IDictionary.IsReadOnly => _properties.IsReadOnly;
        ICollection IDictionary.Keys => _properties.Keys.ToList();
        ICollection IDictionary.Values => _properties.Values.ToList();
        int ICollection.Count => _properties.Count;
        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => this;
        object IDictionary.this[object key] {
            get {
                _properties.TryGetValue((string)key, out var value);
                return value;
            }
            set => _properties[(string)key] = value;
        }

        //--- IEnumerable Members ---
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        void IDictionary.Add(object key, object value) => Add((string)key, value);
        void IDictionary.Clear() => _properties.Clear();
        bool IDictionary.Contains(object key) => _properties.ContainsKey((string)key);
        IDictionaryEnumerator IDictionary.GetEnumerator()
            => ((IDictionary)_properties.ToDictionary(kv => kv.Key, kv => kv.Value)).GetEnumerator();
        void IDictionary.Remove(object key) => _properties.Remove((string)key);
        void ICollection.CopyTo(Array array, int index)
            => ((IDictionary)_properties.ToDictionary(kv => kv.Key, kv => kv.Value)).CopyTo(array, index);
    }
}
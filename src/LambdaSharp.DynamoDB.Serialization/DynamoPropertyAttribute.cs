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

using System;

namespace LambdaSharp.DynamoDB.Serialization {

    /// <summary>
    /// The <see cref="ADynamoPropertyAttribute"/> is the base class for all <see cref="DynamoSerializer"/> property annotations.
    /// </summary>
    public abstract class ADynamoPropertyAttribute : Attribute { }

    /// <summary>
    /// The <see cref="DynamoPropertyIgnoreAttribute"/> property attribute indicates that a property should be skipped by <see cref="DynamoSerializer"/> during (de)serialization.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class DynamoPropertyIgnoreAttribute : Attribute { }

    /// <summary>
    /// The <see cref="DynamoPropertyNameAttribute"/> property attribute modifies the DynamoDB attrivbute name used by <see cref="DynamoSerializer"/> during (de)serialization.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class DynamoPropertyNameAttribute : Attribute {

        //--- Constructors ---

        /// <summary>
        /// Create a new instance of <see cref="DynamoPropertyNameAttribute"/>.
        /// </summary>
        /// <param name="name">The DynamoDB attribute name.</param>
        public DynamoPropertyNameAttribute(string name) => Name = name ?? throw new ArgumentNullException(nameof(name));

        //--- Properties ---

        /// <summary>
        /// The DynamoDB attribute name to use for this property.
        /// </summary>
        public string Name { get; }
    }
}

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
using LambdaSharp.DynamoDB.Native;

namespace Sample.DynamoDBNative.DataAccess.Models {

    public class CustomerRecord {

        //--- Types ---
        public sealed class PrimaryKey : DynamoPrimaryKey<CustomerRecord> {

            //--- Constants ---
            public const string PK_PATTERN = "CUSTOMER#{0}";
            public const string SK_PATTERN = "INFO";

            //--- Constructors ---
            public PrimaryKey(CustomerRecord record) : this(record.Username) { }
            public PrimaryKey(string username) : base(PK_PATTERN, SK_PATTERN, username) { }
        }

        //--- Properties ---
        public string Username { get; set; }
        public string EmailAddress { get; set; }
        public string Name { get; set; }
        public Dictionary<string, AddressRecord> Addresses { get; set; }
    }

    public class CustomerEmailRecord {

        //--- Types ---
        public sealed class PrimaryKey : DynamoPrimaryKey<CustomerEmailRecord> {

            //--- Constants ---
            public const string PK_PATTERN = "CUSTOMEREMAIL#{0}";
            public const string SK_PATTERN = "INFO";

            //--- Constructors ---
            public PrimaryKey(CustomerEmailRecord record) : this(record.EmailAddress) { }
            public PrimaryKey(string emailAddress) : base(PK_PATTERN, SK_PATTERN, emailAddress) { }
        }

        //--- Properties ---
        public string Username { get; set; }
        public string EmailAddress { get; set; }
    }

    public class AddressRecord {

        //--- Properties ---
        public string Label { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public string State { get; set; }
    }

    public enum OrderStatus {
        Undefined,
        Pending,
        Shipped,
        Delivered,
        Cancelled,
        Returned
    }

    public class OrderRecord {

        //--- Types ---
        public sealed class PrimaryKey : DynamoPrimaryKey<OrderRecord> {

            //--- Constants ---
            public const string PK_PATTERN = "CUSTOMER#{0}";
            public const string SK_PATTERN = "#ORDER#{1}";

            //--- Constructors ---
            public PrimaryKey(OrderRecord record) : this(record.CustomerUsername, record.OrderId) { }
            public PrimaryKey(string customerUsername, string orderId) : base(PK_PATTERN, SK_PATTERN, customerUsername, orderId) { }
        }

        public sealed class GSI1Key : DynamoGlobalIndexKey<OrderRecord> {

            //--- Constants ---
            public const string PK_PATTERN = "ORDER#{0}";
            public const string SK_PATTERN = "INFO";

            //--- Constructors ---
            public GSI1Key(OrderRecord record) : this(record.OrderId) { }
            public GSI1Key(string orderId) : base("GSI1", "GSI1PK", "GSI1SK", PK_PATTERN, SK_PATTERN, orderId) { }
        }

        //--- Properties ---
        public string OrderId { get; set; }
        public string CustomerUsername { get; set; }
        public OrderStatus Status { get; set; }
        public DateTimeOffset CreateAt { get; set; }
        public decimal Amount { get; set; }
        public int NumberOfItems { get; set; }
    }

    public class OrderItemRecord {

        //--- Types ---
        public sealed class PrimaryKey : DynamoPrimaryKey<OrderItemRecord> {

            //--- Constants ---
            public const string PK_PATTERN = "ORDER#{0}#ITEM#{1}";
            public const string SK_PATTERN = "INFO";

            //--- Constructors ---
            public PrimaryKey(OrderItemRecord record) : this(record.OrderId, record.ItemId) { }
            public PrimaryKey(string orderId, string itemId) : base(PK_PATTERN, SK_PATTERN, orderId, itemId) { }
        }

        public sealed class GSI1Key : DynamoGlobalIndexKey<OrderItemRecord> {

            //--- Constants ---
            public const string PK_PATTERN = "ORDER#{0}";
            public const string SK_PATTERN = "ITEM#{1}";

            //--- Constructors ---
            public GSI1Key(OrderItemRecord record) : this(record.OrderId, record.ItemId) { }
            public GSI1Key(string orderId, string itemId) : base("GSI1", "GSI1PK", "GSI1SK", PK_PATTERN, SK_PATTERN, orderId, itemId) { }
        }

        //--- Properties ---
        public string OrderId { get; set; }
        public string ItemId { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }
}

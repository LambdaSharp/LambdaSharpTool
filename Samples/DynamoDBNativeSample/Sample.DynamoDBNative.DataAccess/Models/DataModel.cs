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

using LambdaSharp.DynamoDB.Native;

namespace Sample.DynamoDBNative.DataAccess.Models {

    public static class DataModel {

        //--- Constants ---
        private const string CUSTOMER_PK_PATTERN = "CUSTOMER#{0}";
        private const string CUSTOMER_SK_PATTERN = "INFO";
        private const string CUSTOMER_EMAIL_PK_PATTERN = "CUSTOMEREMAIL#{0}";
        private const string CUSTOMER_EMAIL_SK_PATTERN = "INFO";
        private const string ORDER_PK_PATTERN = "CUSTOMER#{0}";
        private const string ORDER_SK_PATTERN = "#ORDER#{1}";
        private const string ORDER_GSI1_PK_PATTERN = "ORDER#{0}";
        private const string ORDER_GSI1_SK_PATTERN = "INFO";
        private const string ORDER_ITEM_PK_PATTERN = "ORDER#{0}#ITEM#{1}";
        private const string ORDER_ITEM_SK_PATTERN = "INFO";
        private const string ORDER_ITEM_GSI1_PK_PATTERN = "ORDER#{0}";
        private const string ORDER_ITEM_GSI1_SK_PATTERN = "ITEM#{1}";

        //--- Class Methods ---
        public static DynamoPrimaryKey<CustomerRecord> CustomerPrimaryKey(CustomerRecord record) => CustomerPrimaryKey(record.Username);
        public static DynamoPrimaryKey<CustomerRecord> CustomerPrimaryKey(string username) => new DynamoPrimaryKey<CustomerRecord>(CUSTOMER_PK_PATTERN, CUSTOMER_SK_PATTERN, username);
        public static DynamoPrimaryKey<CustomerEmailRecord> CustomerEmailPrimaryKey(CustomerEmailRecord record) => CustomerEmailPrimaryKey(record.EmailAddress);
        public static DynamoPrimaryKey<CustomerEmailRecord> CustomerEmailPrimaryKey(string emailAddress) => new DynamoPrimaryKey<CustomerEmailRecord>(CUSTOMER_EMAIL_PK_PATTERN, CUSTOMER_EMAIL_SK_PATTERN, emailAddress);
        public static DynamoPrimaryKey<OrderRecord> OrderPrimaryKey(OrderRecord record) => OrderPrimaryKey(record.CustomerUsername, record.OrderId);
        public static DynamoPrimaryKey<OrderRecord> OrderPrimaryKey(string customerUsername, string orderId) => new DynamoPrimaryKey<OrderRecord>(ORDER_PK_PATTERN, ORDER_SK_PATTERN, customerUsername, orderId);
        public static ADynamoSecondaryKey[] OrderSecondaryKeys(OrderRecord record) => OrderSecondaryKeys(record.CustomerUsername, record.OrderId);
        public static ADynamoSecondaryKey[] OrderSecondaryKeys(string customerUsername, string orderId)
            => new[] {
                new DynamoGlobalIndexKey("GSI1", "GSI1PK", "GSI1SK", ORDER_GSI1_PK_PATTERN, ORDER_GSI1_SK_PATTERN, orderId)
            };
        public static DynamoPrimaryKey<OrderItemRecord> OrderItemPrimaryKey(OrderItemRecord record) => OrderItemPrimaryKey(record.OrderId, record.ItemId);
        public static DynamoPrimaryKey<OrderItemRecord> OrderItemPrimaryKey(string orderId, string itemId) => new DynamoPrimaryKey<OrderItemRecord>(ORDER_ITEM_PK_PATTERN, ORDER_ITEM_SK_PATTERN, orderId, itemId);
        public static ADynamoSecondaryKey[] OrderItemSecondaryKeys(OrderItemRecord record) => OrderItemSecondaryKeys(record.OrderId, record.ItemId);
        public static ADynamoSecondaryKey[] OrderItemSecondaryKeys(string orderId, string itemId)
            => new[] {
                new DynamoGlobalIndexKey("GSI1", "GSI1PK", "GSI1SK", ORDER_ITEM_GSI1_PK_PATTERN, ORDER_ITEM_GSI1_SK_PATTERN, orderId, itemId)
            };

        // TODO: need a better solution here
        public static DynamoPrimaryKey CustomerAndOrdersQuery(string customerUsername) => new DynamoPrimaryKey(CUSTOMER_PK_PATTERN, "<NOT-USED>", customerUsername);
        public static DynamoGlobalIndexKey OrderAndOrderItemsQuery(string orderId) => new DynamoGlobalIndexKey("GSI1", "GSI1PK", "GSI1SK", ORDER_ITEM_GSI1_PK_PATTERN, "<NOT-USED>", orderId);
        public static DynamoGlobalIndexKey<OrderRecord> OrderQuery(string orderId) => new DynamoGlobalIndexKey<OrderRecord>("GSI1", "GSI1PK", "GSI1SK", ORDER_GSI1_PK_PATTERN, ORDER_GSI1_SK_PATTERN, orderId);
    }
}

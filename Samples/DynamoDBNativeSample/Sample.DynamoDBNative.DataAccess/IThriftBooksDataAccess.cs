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
using System.Threading;
using System.Threading.Tasks;
using Sample.DynamoDBNative.DataAccess.Models;

namespace Sample.DynamoDBNative.DataAccess {

    public interface IThriftBooksDataAccess {

        //--- Methods ---
        Task CreateCustomerAsync(CustomerRecord customer, CancellationToken cancellationToken = default);
        Task AddOrUpdateAddressAsync(string customerUsername, AddressRecord address, CancellationToken cancellationToken = default);
        Task<(CustomerRecord Customer, IEnumerable<OrderRecord> Orders)> GetCustomerWithMostRecentOrdersAsync(string customerUsername, int limit, CancellationToken cancellationToken = default);
        Task SaveOrderAsync(OrderRecord order, IEnumerable<OrderItemRecord> orderItems, CancellationToken cancellationToken = default);
        Task UpdateOrderAsync(string orderId, OrderStatus orderStatus, CancellationToken cancellationToken = default);
        Task<(OrderRecord Order, IEnumerable<OrderItemRecord> Items)> GetOrderWithOrderItemsAsync(string orderId, CancellationToken cancellationToken = default);
    }
}

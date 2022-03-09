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

using LambdaSharp;
using LambdaSharp.ApiGateway;
using Sample.DynamoDBNative.ApiFunction.Models;
using Sample.DynamoDBNative.DataAccess;
using Sample.DynamoDBNative.DataAccess.Models;

namespace Sample.DynamoDBNative.ApiFunction {

    public sealed class Function : ALambdaApiGatewayFunction {

        //--- Fields ---
        private IThriftBooksDataAccess? _dataAccessClient;

        //--- Constructors ---
        public Function() : base(new LambdaSharp.Serialization.LambdaSystemTextJsonSerializer()) { }

        //--- Properties ---
        private IThriftBooksDataAccess DataAccessClient => _dataAccessClient ?? throw new InvalidOperationException();

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {

            // read configuration settings
            var tableName = config.ReadDynamoDBTableName("DataTable");

            // initialize clients
            _dataAccessClient = new ThriftBooksDataAccessClient(tableName);
        }

        public async Task<CreateCustomerResponse> CreateCustomerAsync(CreateCustomerRequest request) {
            var customer = new CustomerRecord {
                EmailAddress = request.EmailAddress,
                Name = request.Name,
                Username = request.Username
            };
            await DataAccessClient.CreateCustomerAsync(customer);
            return new CreateCustomerResponse {
                Customer = customer
            };
        }

        public async Task<AddOrUpdateAddressResponse> AddOrUpdateAddressAsync(string customerUsername, string addressLabel, AddOrUpdateAddressRequest request) {
            var address = new AddressRecord {
                Label = addressLabel,
                City = request.City,
                State = request.State,
                Street = request.Street
            };
            await DataAccessClient.AddOrUpdateAddressAsync(customerUsername, address);
            return new AddOrUpdateAddressResponse();
        }

        public async Task<GetCustomerWithMostRecentOrdersResponse> GetCustomerWithMostRecentOrdersAsync(string customerUsername, int? limit) {
            var (customer, orders) = await DataAccessClient.GetCustomerWithMostRecentOrdersAsync(customerUsername, limit ?? 10);
            return new GetCustomerWithMostRecentOrdersResponse {
                Customer = customer,
                Orders = orders.ToList()
            };
        }

        public async Task<SaveOrderResponse> SaveOrderAsync(SaveOrderRequest request) {
            await DataAccessClient.SaveOrderAsync(request.Order, request.Items);
            return new SaveOrderResponse();
        }

        public async Task<UpdateOrderResponse> UpdateOrderAsync(UpdateOrderRequest request) {
            await DataAccessClient.UpdateOrderAsync(request.OrderId, request.Status);
            return new UpdateOrderResponse();
        }

        public async Task<GetOrderWithOrderItemsResponse> GetOrderWithOrderItemsAsync(string orderId) {
            var (order, orderItems) = await DataAccessClient.GetOrderWithOrderItemsAsync(orderId);
            return new GetOrderWithOrderItemsResponse {
                Order = order,
                Items = orderItems.ToList()
            };
        }
    }
}

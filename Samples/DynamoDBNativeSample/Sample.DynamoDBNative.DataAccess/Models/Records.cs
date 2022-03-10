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

namespace Sample.DynamoDBNative.DataAccess.Models;

using System.Text.Json.Serialization;

public class CustomerRecord {

    //--- Properties ---
    public string? Username { get; set; }
    public string? EmailAddress { get; set; }
    public string? Name { get; set; }
    public Dictionary<string, AddressRecord>? Addresses { get; set; }
}

public class CustomerEmailRecord {

    //--- Properties ---
    public string? Username { get; set; }
    public string? EmailAddress { get; set; }
}

public class AddressRecord {

    //--- Properties ---
    public string? Label { get; set; }
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrderStatus {
    Undefined,
    Pending,
    Shipped,
    Delivered,
    Cancelled,
    Returned
}

public class OrderRecord {

    //--- Properties ---
    public string? OrderId { get; set; }
    public string? CustomerUsername { get; set; }
    public OrderStatus Status { get; set; }
    public DateTimeOffset CreateAt { get; set; }
    public decimal Amount { get; set; }
    public int NumberOfItems { get; set; }
}

public class OrderItemRecord {

    //--- Properties ---
    public string? OrderId { get; set; }
    public string? ItemId { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

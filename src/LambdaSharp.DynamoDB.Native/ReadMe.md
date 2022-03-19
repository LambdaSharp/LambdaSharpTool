# LambdaSharp.DynamoDB.Native

This package contains interfaces and classes used for using [Amazon DynamoDB](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Introduction.html). DynamoDB is an advanced, serverless key-value store that is fast and scales well, but that suffers from a complex API design. To learn more about DynamoDB, visit the [Amazon DynamoDB](https://aws.amazon.com/dynamodb/) webpage or buy the excellent ["The DynamoDB Book" by Alex DeBrie](https://www.dynamodbbook.com/) for in-depth coverage with samples.

_LambdaSharp.DynamoDB.Native_ uses LINQ expression with a fluent interface which drastically simplifies using advanced DynamoDB features in applications.

To optimize simplicity, the `DynamoTable` makes a few design choices.
1. `DynamoTable` is designed for single table access and does not support operations across multiple tables.
1. `DynamoTable` assumes the primary key is made from a partition key and a sort key. Furthermore, the name of partition key is `PK` and the name of the sort key is `SK`.

These are intentional constraints of the `DynamoTable` implementation. If these constraints do not fit your use case, then `DynamoTable` may not be suitable for you.

Visit [LambdaSharp.NET](https://lambdasharp.net/) to learn more about building serverless .NET solutions on AWS.

## Initializing a `DynamoTable` instance

The constructor for `DynamoTable` only requires the table name. Optionally, an `IAmazonDynamoDB` client can be passed as a second parameter. When omitted, the default `AmazonDynamoDBClient` is used.

```csharp
IDynamoTable table = new DynamoTable(tableName);
```

Once the `DynamoTable` instance has been initialized, it is ready for use.


## Primary Keys

Most DynamoDB operations use `DynamoPrimaryKey` to specify the item to act upon. The exception to this rule is the `Query` operation, which selects a collection of items.

The `DynamoPrimaryKey` specifies the partition key and sort key of the item to act upon. The primary key is always a composite key and the names must be `PK` and `SK`, respectively.

```csharp
var primaryKey = new DynamoPrimaryKey<CustomerRecord>(pkValue, skValue);
```

The generic type parameter in `DynamoPrimaryKey<TRecord>` serves two purposes:
* It avoids accidental confusion between primary keys by having a stricter type.
* The generic type is used to determine the type of the record class to instantiate.


## GetItem Operation

The `GetItemAsync()` is the simplest way to retrieve a DynamoDB item. It reads all the attributes and populates a record instance. If the item was not found, the method returns `null` instead.

```csharp
var customer = await table.GetItemAsync(primaryKey, cancellationToken);
```

The `GetItem()` method enables retrieving only some properties of the record instead of all of them. Reducing the amount of data returned does not reduce the DynamoDB read consumption, but it does reduce the bandwidth required to retrieve the wanted data.

The following code only populates the `CustomerId` and `CustomerName` property.
```csharp
var partialRecord = await table.GetItem(primaryKey)
    .Get(record => record.CustomerId)
    .Get(record => record.CustomerName)
    .ExecuteAsync(cancellationToken);
```

The `DynamoTable` instance analyzes the lambda expressions passed into the `Get()` method and determines from them the DynamoDB attributes to request. The lambda expression is typed using the generic parameter from `DynamoPrimaryKey<TRecord>`. The lambda expression can use `.` and index `[]` operations to specify inner attributes of maps and lists as well. This approach makes it both easy and safe to specify the wanted attributes.

A `NotSupportedException` is thrown when the lambda expression is not valid for specifying a DynamoDB attribute.


## PutItem Operation

The `PutItemAsync()` method creates new DynamoDB item or replaces one. When an existing item is replaced, all previous item attributes are removed and replaced with the item attributes. The method returns a boolean indicating success.

```csharp
var customer = new CustomerRecord {
    CustomerId = "123",
    CustomerName = "John Doe",
    CustomerEmail = "john@example.org",
    Addresses = new Dictionary<string, CustomerAddress>()
};
var success = table.PutItemAsync(primaryKey, customer, cancellationToken)
```

The `PutItem()` method allow specifying conditions and also returning the previously stored DynamoDB item.

Use the `ExecuteReturnOldItemAsync()` to retrieve the DynamoDB item attributes that were replaced. If the DynamoDB item doesn't exist, the method returns `null`.

```csharp
var customer = new CustomerRecord {
    CustomerId = "123",
    CustomerName = "John Doe",
    CustomerEmail = "john@example.org",
    Addresses = new Dictionary<string, CustomerAddress>()
};
var previousCustomer = table.PutItem(primaryKey, customer)
    .ExecuteReturnOldItemAsync(cancellationToken)
```

Use `WithCondition()` to specify a condition before performing the _PutItem_ operation. The following code checks that the DynamoDB item does not exist.

```csharp
var customer = new CustomerRecord {
    CustomerId = "123",
    CustomerName = "John Doe",
    CustomerEmail = "john@example.org",
    Addresses = new Dictionary<string, CustomerAddress>()
};
var success = table.PutItem(primaryKey, customer)
    .WithCondition(record => DynamoCondition.Exists(record))
    .ExecuteAsync(cancellationToken)
```

The `DynamoCondition` static class contains static methods representing native DynamoDB condition functions and operators. These methods are only used to analyze the lambda expressions in the `WithCondition()` method. When used directly, the methods throw an `InvalidOperationException`.

See [Comparison Operator and Function Reference](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Expressions.OperatorsAndFunctions.html) for a complete list of condition expressions.

Sometimes, it is necessary to store additional attributes alongside a record. For example, attributes used by either local or global secondary indices. This is possible by using the `Set()` method.

```csharp
var customer = new CustomerRecord {
    CustomerId = "123",
    CustomerName = "John Doe",
    CustomerEmail = "john@example.org",
    Addresses = new Dictionary<string, CustomerAddress>()
};
var success = table.PutItem(primaryKey, customer)

    // allow retrieving customer record using the email address via the global secondary index GSI1
    .Set("GSI1PK", $"EMAIL={record.CustomerEmail}")
    .Set("GSI1SK", "INFO")
    .ExecuteAsync(cancellationToken)
```


## DeleteItem Operation

The `DeleteItemAsync()` method is used to delete a DynamoDB item. The operation is idempotent and succeeds regardless if the item exists or not.

```csharp
var success = await table.DeleteItemAsync(primaryKey, cancellationToken);
```

The `DeleteItem()` method has similar capabilities as the `PutItem()` method and allows specifying a condition for the operation and returning the previously stored DynamoDB item.

Use the `ExecuteReturnOldItemAsync()` to retrieve the DynamoDB item attributes that were deleted. The method returns `null` if the DynamoDB item doesn't exist or the operation could not be performed.

```csharp
var previousCustomer = table.DeleteItem(primaryKey)
    .ExecuteReturnOldItemAsync(cancellationToken)
```

Use `WithCondition()` to specify a condition before performing the _DeleteItem_ operation. The following code checks the state of the DynamoDB item before deleting it.

```csharp
var success = table.DeleteItem(primaryKey)
    .WithCondition(record => record.Status == CustomerStatus.Deactivated)
    .ExecuteAsync(cancellationToken)
```

The `DynamoCondition` static class contains static methods representing native DynamoDB condition functions and operators. These methods are only used to analyze the lambda expressions in the `WithCondition()` method. When used directly, the methods throw an `InvalidOperationException`.

See [Comparison Operator and Function Reference](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Expressions.OperatorsAndFunctions.html) for a complete list of condition expressions.


## UpdateItem Operation

The _UpdateItem_ operation is the most powerful and versatile operation that DynamoDB supports. It allows updating specific attributes of a DynamoDB item without affecting others. Furthermore, it can use the previous value of the item and atomically modify it to a new value. Similar to the _PutItem_ operation, the _UpdateItem_ operation can either create a new DynamoDB item or update an existing one.

The `UpdateItem()` method allows tapping into the rich set of capabilities of the _UpdateItem_ operation, which include:
* The `Set()` method to modify a DynamoDB item attribute to a specific value or to compute a new value from an existing one. See [SET—Modifying or Adding Item Attributes](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Expressions.UpdateExpressions.html#Expressions.UpdateExpressions.SET)).
* The `Remove()` method to delete a top-level or nested attribute. See [REMOVE—Deleting Attributes from an Item](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Expressions.UpdateExpressions.html#Expressions.UpdateExpressions.REMOVE).
* The `Add()` method to add a number to a numeric attribute or add an item to a set attribute. See [ADD—Updating Numbers and Sets](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Expressions.UpdateExpressions.html#Expressions.UpdateExpressions.ADD).
* The `Delete()` method to remove an item from a set attribute. See [DELETE—Removing Elements from a Set](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Expressions.UpdateExpressions.html#Expressions.UpdateExpressions.DELETE).


Use the `ExecuteReturnOldItemAsync()` to retrieve the DynamoDB item attributes before they were updated. The method returns `null` if the DynamoDB item doesn't exist or the operation could not be performed.

```csharp
var previousCustomer = table.UpdateItem(primaryKey)
    .Set(record => record.CustomerName, "John Doe")
    .ExecuteReturnOldItemAsync(cancellationToken)
```

Use the `ExecuteReturnNewItemAsync()` to retrieve the DynamoDB item attributes after they were updated. The method returns `null` if the DynamoDB item doesn't exist or the operation could not be performed.

```csharp
var updatedCustomer = table.UpdateItem(primaryKey)
    .Set(record => record.CustomerName, "John Doe")
    .ExecuteReturnNewItemAsync(cancellationToken)
```

Use `WithCondition()` to specify a condition before performing the _UpdateItem_ operation. The following code checks that the DynamoDB item exists.

```csharp
var success = table.UpdateItem(primaryKey)
    .WithCondition(record => DynamoCondition.Exists(record))
    .Set(record => record.PendingOrders, record => record.PendingOrders + 1)
    .ExecuteAsync(cancellationToken)
```

The `DynamoCondition` static class contains static methods representing native DynamoDB condition functions and operators. These methods are only used to analyze the lambda expressions in the `WithCondition()` method. When used directly, the methods throw an `InvalidOperationException`.

See [Comparison Operator and Function Reference](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Expressions.OperatorsAndFunctions.html) for a complete list of condition expressions.

Some of the `Set()` methods take a lambda expression as their second argument to compute a new value based on an existing DynamoDB item attribute. Although, this capability is extremely powerful, the kind of operations that can be performed is very limited. See [SET—Modifying or Adding Item Attributes](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Expressions.UpdateExpressions.html#Expressions.UpdateExpressions.DELETE) for a list of allowed operations and functions. The `DynamoUpdate` static class contains static methods representing native DynamoDB update functions. These methods are only used to analyze the lambda expressions in the `Set()` method. When used directly, the methods throw an `InvalidOperationException`.

Sometimes, it is necessary to store additional attributes alongside a record. For example, attributes used by either local or global secondary indices. This is possible by using the `Set()` method.

```csharp
var success = table.UpdateItem(primaryKey)
    .WithCondition(record => DynamoCondition.Exists(record))

    // update customer email
    .Set(record => record.CustomerEmail, "john@example.org")

    // also update global secondary index GSI1 to allow retrieving customer record using the email address
    .Set("GSI1PK", "EMAIL=john@example.org")
    .ExecuteAsync(cancellationToken)
```

## Query Operation

The _Query_ operation allows finding multiple items in the main index or in a local/global secondary index.

The _Query_ operation is anchored by the partition key (PK). Optionally, constraints on the sort key (SK) can be specified to narrow down the collection of DynamoDB items. However, the list of constraints is very limited.
* `SK` is equal to some value
* `SK` is less than some value
* `SK` is less than or equal to some value
* `SK` is greater than some value
* `SK` is greater than or equal to some value
* `SK` is between two bounds (inclusively)
* `SK` begins with a sub-string

Those are all the possible constraints. They cannot be combined into more complex constraints either. This list is literally it. See [Key Condition Expression](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Query.html#Query.KeyConditionExpressions).

In _LambdaSharp.DynamoDB.Native_, the combination of the PK value and the SK constraint is referred to as the _query clause_. It is one of the more complex concepts in DynamoDB to model, but the _query clause_ seems to work well in practice.

The following operation finds all WebSocket connections belonging to a given user.
```csharp
var clause = DynamoQuery.FromIndex("GSI1")
    .SelectPK("USER=123")
    .WhereSKBeginsWith("WS=");
```

The `Query()` method can specify an operation that returns a list of DynamoDB items that all have the same type or they can be mixed. For the latter case, the `WithTypeFilter<TRecord>()` method is used to specify what types to deserialize. Items returned by the DynamoDB _Query_ operation that do not match the type filter are discarded.

The results of the DynamoDB _Query_ operation can either be retrieved as a complete list using `ExecuteAsync()` or streamed incrementally using `ExecuteAsyncEnumerable()`.

```csharp
// receive all items at once
var list = await table.Query(clause, limit: 100).ExecuteAsync(cancellationToken);

// receive items as they come in
var asyncEnumerable = table.Query(clause, limit: 100).ExecuteAsyncEnumerable(cancellationToken);
await foreach(var item in asyncEnumerable.WithCancellation(cancellationToken)) {

    // process item incrementally
}
```

## License

> Copyright (c) 2018-2022 LambdaSharp (λ#)
>
> Licensed under the Apache License, Version 2.0 (the "License");
> you may not use this file except in compliance with the License.
> You may obtain a copy of the License at
>
> http://www.apache.org/licenses/LICENSE-2.0
>
> Unless required by applicable law or agreed to in writing, software
> distributed under the License is distributed on an "AS IS" BASIS,
> WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
> See the License for the specific language governing permissions and
> limitations under the License.

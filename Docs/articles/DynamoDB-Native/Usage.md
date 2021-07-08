---
title: Tutorial for DynamoDB for .NET - LambdaSharp
description: Using LambdaSharp.DynamoDB.Native
keywords: tutorial, api, dynamodb, aws, amazon
---

# Tutorial for DynamoDB for .NET

## Overview

> TODO: walk reader through the Thriftstore example

## Access Patterns

* Direct Access
* Query Access

## Direct Access

```csharp
class Customer {

    //--- Properties ---
    public string Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public Dictionary<string, Address> Addresses { get; set; }
}

class Address {

    //--- Properties ---
    public string Street { get; set; }
    public string ZipCode { get; set; }
    public string Country { get; set; }
}

static DynamoPrimaryKey<Customer> GetPrimaryKeyForCustomer(string customerId)
    => new DynamoPrimaryKey<Customer>($"CUSTOMER#{customerId}", "INFO");

static DynamoPrimaryKey<Customer> GetPrimaryKeyForCustomer(Customer customer)
    => new DynamoPrimaryKey<Customer>($"CUSTOMER#{customer.Id}", "INFO");
```


### GetItem

```csharp
var primaryKey = GetPrimaryKeyForCustomer("123");

// fetch all properties for the record
var customer = await Table.GetItem(primaryKey)
    .ExecuteAsync();
```

```csharp
var primaryKey = GetPrimaryKeyForCustomer("123");

// fetch all properties for the record
var customer = await Table.GetItemAsync(primaryKey);
```

```csharp
var primaryKey = GetPrimaryKeyForCustomer("123");

// fetch only the `FirstName` and `LastName` properties for the record
var customer = await Table.GetItem(primaryKey)
    .Get(record => record.FirstName)
    .Get(record => record.LastName)
    .ExecuteAsync();
```


### PutItem

```
CONDITION EXPRESSION OPERATORS AND FUNCTIONS
https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Expressions.OperatorsAndFunctions.html
condition-expression ::=
    operand comparator operand
    | operand BETWEEN operand AND operand
    | operand IN ( operand (',' operand (, ...) ))
    | function
    | condition AND condition
    | condition OR condition
    | NOT condition
    | ( condition )
comparator ::=
    =
    | <>
    | <
    | <=
    | >
    | >=
function ::=
    attribute_exists (path)
    | attribute_not_exists (path)
    | attribute_type (path, type)
    | begins_with (path, substr)
    | contains (path, operand)
    | size (path)
```

```csharp
var record = new Customer {
    Id = "123",
    FirstName = "John",
    LastName = "Doe",
    Addresses = new Dictionary<string, object>()
};

var primaryKey = GetPrimaryKeyForCustomer(record);

// write new customer record regardless if it already existed or not
var success = await Table.PutItem(primaryKey, record)
    .ExecuteAsync();
```

```csharp
var record = new Customer {
    Id = "123",
    FirstName = "John",
    LastName = "Doe",
    Addresses = new Dictionary<string, object>()
};

var primaryKey = GetPrimaryKeyForCustomer(record);

// write new customer record regardless if it already existed or not; and return previous record values
var overwrittenCustomerRecord = await Table.PutItem(primaryKey, record)
    .ExecuteReturnOldRecordAsync();
```

```csharp
var record = new Customer {
    Id = "123",
    FirstName = "John",
    LastName = "Doe",
    Addresses = new Dictionary<string, object>()
};

var primaryKey = GetPrimaryKeyForCustomer(record);

// write new customer record regardless if it already existed or not; and write additional, arbitrary item attributes
var success = await Table.PutItem(primaryKey, record)
    .Set("AnyAttribute", "Value")
    .ExecuteAsync();
```

```csharp
var record = new Customer {
    Id = "123",
    FirstName = "John",
    LastName = "Doe",
    Addresses = new Dictionary<string, object>()
};

var primaryKey = GetPrimaryKeyForCustomer(record);

// write new customer record, but only if it does not yet exist
var success = await Table.PutItem(primaryKey, record)
    .WithCondition(record => DynamoCondition.DoesNotExist(record))
    .ExecuteAsync();
```

```csharp
var record = new Customer {
    Id = "123",
    FirstName = "John",
    LastName = "Doe",
    Addresses = new Dictionary<string, object>()
};

var primaryKey = GetPrimaryKeyForCustomer(record);

// write new customer record regardless if it already existed or not (short form)
var success = await Table.PutItemAsync(primaryKey, record);
```


### DeleteItem


```
CONDITION EXPRESSION OPERATORS AND FUNCTIONS
https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Expressions.OperatorsAndFunctions.html
condition-expression ::=
    operand comparator operand
    | operand BETWEEN operand AND operand
    | operand IN ( operand (',' operand (, ...) ))
    | function
    | condition AND condition
    | condition OR condition
    | NOT condition
    | ( condition )
comparator ::=
    =
    | <>
    | <
    | <=
    | >
    | >=
function ::=
    attribute_exists (path)
    | attribute_not_exists (path)
    | attribute_type (path, type)
    | begins_with (path, substr)
    | contains (path, operand)
    | size (path)
```

```csharp
var primaryKey = GetPrimaryKeyForCustomer("123");

// delete record; no-op if row doesn't exist
var success = await Table.DeleteItem(primaryKey, record)
    .ExecuteAsync();
```

```csharp
var primaryKey = GetPrimaryKeyForCustomer("123");

// delete record and return previous record values
var overwrittenCustomerRecord = await Table.DeleteItem(primaryKey, record)
    .ExecuteReturnOldRecordAsync();
```

```csharp
var primaryKey = GetPrimaryKeyForCustomer("123");

// delete item only if condition matches
var success = await Table.DeleteItem(primaryKey, record)
    .WithCondition(record => record.Name == "Doe")
    .ExecuteAsync();
```

```csharp
var primaryKey = GetPrimaryKeyForCustomer("123");

// delete record; no-op if row doesn't exist (short form)
var success = await Table.DeleteItemAsync(primaryKey, record);
```


### UpdateItem

> TODO

### BatchGetItem

> TODO

### BatchWriteItem

> TODO

## Query Access

> TODO

### Query

> TODO

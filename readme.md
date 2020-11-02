EfCoreTriggers is the library to write triggers for EFCore using OOP style. Triggers are translating into sql and automatically adding to migrations.

## Available providers

- PostgreSQL

### Configuring DB to use triggers

Call UseTriggers() to use the library.

```cs
var options = new DbContextOptionsBuilder<TestDbContext>()
    .UseNpgsql("User ID=test;Password=test;Host=localhost;Port=5432;Database=test;")
    .UseTriggers() // Initialize necessary dependencies
    .Options;

var dbContext = new TestDbContext(options);
```

After Update trigger example.

```cs
modelBuilder.Entity<Transaction>()
    .AfterUpdate(trigger => trigger
        .Action(action => action
            .Condition((oldTransaction, newTransaction) => oldTransaction.IsVeryfied && newTransaction.IsVeryfied)
            .UpdateAnotherEntity<UserBalance>(
                (oldTransaction, newTransaction, userBalances) => userBalances.UserId == oldTransaction.UserId,
                (oldTransaction, newTransaction, oldBalance) => new UserBalance { Balance = oldBalance.Balance + newTransaction.Value - oldTransaction.Value })));
```

After Delete trigger example.

```cs
modelBuilder.Entity<Transaction>()
    .AfterDelete(trigger => trigger
        .Action(action => action
            .Condition(deletedTransaction => deletedTransaction.IsVeryfied)
            .UpdateAnotherEntity<UserBalance>(
                (deletedTransaction, userBalances) => userBalances.UserId == deletedTransaction.UserId,
                (deletedTransaction, oldUser) => new UserBalance { Balance = oldUser.Balance - deletedTransaction.Value })));));
```

After Insert trigger example.

```cs
modelBuilder.Entity<Transaction>()
    .AfterInsert(trigger => trigger
        .Action(action => action
            .Condition(insertedTransaction => insertedTransaction.IsVeryfied)
            .UpdateAnotherEntity<UserBalance>(
                (insertedTransaction, userBalances) => userBalances.UserId == insertedTransaction.UserId,
                (insertedTransaction, oldUser) => new UserBalance { Balance = oldUser.Balance + insertedTransaction.Value })));));
```
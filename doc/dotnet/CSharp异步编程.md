# C#异步编程

managed thread pool

worker thread/foreground thread/background thread

## hot task vs cold task


## do not block, await


## Exception




## Under the hood



# Cancellation

## Write cancelable method

Pass token as parameter. And then inside the method, observe that token in some manner, the following code do the observation by polling. 

```csharp
private int MethodCanBeCanceled(CancellationToken token = default(CancellationToken))
{
    for (i=0; i<1000; i++)
    {
        Thread.Sleep(100) // simulate cpu bound work
        if (i % 100 == 0)  // control pooling frequency
            token.ThrowIfCancellationRequested();
    }
    return 42;
}
```

There’s another member on CancellationToken called IsCancellationRequested, which starts returning true when the token is canceled. Some people use this member to respond to cancellation, usually by returning a default or null value. I do not recommend this approach for most code. The standard cancellation pattern is to raise an OperationCanceledException, which is taken care of by ThrowIfCancellationRequested. 

## Pass CancellationToken

When calling a cancellable method, just pass the toke.

```csharp
var cts = new CancellationTokenSource();
var result = MethodCanBeCanceled(cts.Token);
```

If it is a task issued use Task.Run(), do this:

```csharp
var cts = new CancellationTokenSource();
Task.Run(() => ,MethodCanBeCanceled(cts.Token), cts.token);
```

NOTE: The token passed into the method is observed by that method, and can do cancel even when task is started, yet the token pass to Task.Run() method only have ability to cancel the task before the task start to run, which in most case is useless.

## Issue cancel

```csharp
// pass in cts token, and issue cancel from cts.
// Normally, setting up the CancellationTokenSource and issuing the cancellation are in separate methods.
var cts = new CancellationTokenSource();
var task = CancelableMethodAsync(cts.Token);
// some work here
cts.Cancel();
```


## resources

[async-tips-tricks](https://cpratt.co/async-tips-tricks/)
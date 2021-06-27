# C#异步编程

异步编程属于并发编程的一种。C#5引入async/await关键字后，异步变得简单起来。

## 从编程模型说起

**线程**

线程（Thread）是OS为应用程序提供的抽象，它程序执行任务的基本单元，在一个线程中，任务顺序执行。

```
ONE thread: task1 -> task2 -> task3
```

**单线程模型VS多线程模型**

既然一个线程可以承载任务顺序执行，那么多个线程就可以实现多个任务同时执行。在前面的例子中，如果task1, task2, task3之间没有依赖关系，那么他们在多线程模型中可以是这样的：

```
Thread1 : task1
Thread2 : task2
Thread3 : task3
```

多线程模型是并发（Concurrency）编程的基础。需要指出的是，线程是并发编程最底层的抽象，直接使用它们就要考虑同步与锁的问题，让编程变得复杂。因此，现代编程语言与框架通常会提供一些更高层次的抽象，是得程序员不需要直接与线程打交道，但是，你必须知道，无论框架提供了何种抽象，只要是并发，就基于多线程。

## 异步编程

有了前面的准备，我么就可以来看看异步编程了。C#中，异步编程有两个主要元素：

* `Task`抽象
* `async/await`关键字

Task是需要完成任务的抽象。任务体现在method中，分为两种：`Task`代表没有返回值的方法，`Task<T>`代表返回类型为T的方法，例如:

```csharp
static async Task WorkHaveNoReturnAsync()
{
    await Task.Delay(TimeSpan.FromSeconds(1));
}

static async Task<int> WorkHaveReturnAsync()
{
    await Task.Delay(TimeSpan.FromSeconds(1));
    return 0;
}
```

上面的例子中，第一个方法代表没有返回值的任务，这样的任务作为异步方法时，它的返回类型是`Task`. 第二个方法代表有返回值的任务，这样的任务作为异步方法时，它的返回值就是`Task<T>`.

调用异步方法时，使用`await`，并在该方法的声明中使用`async Task/Task<T>`。所有的magic都发生在这个`await`上。

C#维护了一个[线程池](https://docs.microsoft.com/en-us/dotnet/api/system.threading.threadpool?view=net-5.0)，线程池里的线程都是background thread。当一个Task被await后，它就进入到线程池的待执行任务的队列中。线程池中有多个线程，只有有闲置的线程，就会到任务队列中获取任务并执行。当线程池任务不是大量CPU密集型任务时，进入线程池就可看做时并行的。

Task是被线程池线程执行，完成后通知调用线程，这样调用线程就可以接着执行了。

## 不阻塞UI

让我们来看看异步编程的第一个作用：不阻塞UI.看下面的代码：

```csharp
static async Task<int> WorkHaveReturnAsync()
{
    await Task.Delay(TimeSpan.FromSeconds(1));
    return 0;
}

static int WorkHaveReturn()
{
    Thread.Sleep(TimeSpan.FromSeconds(1));
    return 0;
}
```

两个方法执行时，效果几乎一样：async方法，延迟1s后返回结果；sync的方法，睡眠1s后返回结果。

但是，当它们在UI线程执行时，效果就非常不一样：异步方法，由于把延时task交给了后台线程池执行，因此UI主线程仍然是可以响应外来事件的，典型的就是用户的交互事件，如点击按钮。而同步方法则会使主线程不再响应，也叫主线程被阻塞了，造成用户体验不佳。其实，你大概率遇到过这种情形，一个APP突然不响应你的操作了，或者直接白屏了，这些都是主线程阻塞的表现。因此，异步方法在UI相关的编程非常重要。

## 写线性代码，并发执行任务

异步编程的第二个作用是，可以让多线程编程像单线程编程一样简单。

```csharp
// three tasks to be done
static async Task FirstTaskAsync()
{
    await Task.Delay(TimeSpan.FromSeconds(1));  // simulate task take 1s
}

static async Task SecondTaskAsync()
{
    await Task.Delay(TimeSpan.FromSeconds(2)); // simulate task take 2s
}

static async Task ThirdTaskAsync()
{
    await Task.Delay(TimeSpan.FromSeconds(3)); // simulate task take 3s
}
```

假定这三个任务互不相关，我们希望同时开始这三个任务，这样就可在3s完成任务，使用异步编程就很容易实现：

```csharp
static async Task<int> Main(string[] args)
{
    var stopWatch = Stopwatch.StartNew();
    stopWatch.Start();

    var task1 = FirstTaskAsync();  // start task 1
    var task2 = SecondTaskAsync();  // start task 2
    var task3 = ThirdTaskAsync();  // start task 3
    await Task.WhenAll(task1, task2, task3);  // wait three task to be done
    Console.WriteLine("All tasks down!");

    stopWatch.Stop();
    Console.WriteLine($"Time take: {stopWatch.ElapsedMilliseconds / 1000}s"); // output: Time take: 3s
    return 0;
}
```

让任务并发执行，就这么简单！

## 细节

细节是魔鬼。由于异步编程是并发的一种，因此使用时有一些需要注意的点，让我们逐一分析。

### Hot task vs cold task

任务如果进入到线程池的任务队列，就是hot task（有的定义说，任务开始执行时时hot task，这是不对的，因为任务进入到线程池的任务队列后，无法知道task在何时执行），如果任务还没有进入队列，就是cold task。在前面的并行例子中，如果将代码改为：

```csharp
var stopWatch = Stopwatch.StartNew();
stopWatch.Start();

await FirstTaskAsync();
await SecondTaskAsync();
await ThirdTaskAsync();
Console.WriteLine("All tasks down!");

stopWatch.Stop();
Console.WriteLine($"Time take in RunTaskFakeParallel: {stopWatch.ElapsedMilliseconds / 1000}s");
```

则执行时间会变为6s，而不是之前的3s。这是因为：await后的task，是cold task，只有代码执行到该await segment时，才会进入线程池任务队列，变成hot task。上面的例子中，程序会先把FirstTaskAsync任务加入到线程池队列，等待这个任务执行完后，再把SecondTaskAsync加入到线程池队列，然后等待它完成，接着处理第三个任务。

而在最初的例子中，我们把每一个任务的返回对象存在了一个临时变量中，这个过程就已经把任务都加到了线程池任务队列。因此当`await Task.WhenAll`时，是在等待线程池完成已经任务队列中的三个任务。由于任务量小，且都不是cup密集任务，因此三个任务几乎可看做并行执行。

总之，每看到一个`await`，你就要意识到，程序会在这里暂停并等待线程池完成任务，await是程序的暂停等待点。

### Exception

任何程序，exception的处理都极为重要，异步程序也不例外。

**抛出异常**

在异步方法中，抛出异常与一般方法并无二致。

```csharp
static async Task TaskThrowException()
{
    await Task.Delay(TimeSpan.FromSeconds(1));
    throw new Exception("Something is wrong");
}
```

Exception抛出后，Task的行为特点是：

* [Task的状态](https://docs.microsoft.com/zh-cn/dotnet/api/system.threading.tasks.taskstatus?view=net-5.0)会变成`Faulted`，线程池会认为处于该状态的任务已经完成，不再继续处理。
* Task的[Exception属性](https://docs.microsoft.com/zh-cn/dotnet/api/system.threading.tasks.task.exception?view=net-5.0#System_Threading_Tasks_Task_Exception)是一个AggregateException对象，表示在执行这个任务是发生的一个或多个错误。当对Task使用`await`时，如果task的Exception属性不为空，这会抛出Task.Exception的第一个异常，如果没有对Task使用`await`，则异常不会被抛出，就好比同步编程中，捕获了异常但不做任何处理！

**捕获异常**

知道异常抛出后task的行为特点后，捕获异常就很简单了：

```csharp
try
{
    await TaskThrowException();
}
catch (Exception ex)
{
    Console.WriteLine(ex);
    Console.WriteLine("Exception handled!");
}    
```

如果，你忘记`await` task,那么这个函数中的异常就会消失于无形，你的程序就引入了最难调试的bug之一：不被察觉的异常！

```csharp
try
{
    TaskThrowException();  // the exception in this task will not be thrown!
}
catch (Exception ex)
{
    // cold will never go in here
    Console.WriteLine(ex);
    Console.WriteLine("Exception handled!");
} 
```

所以，一定要`await` Task!

### 取消任务

已经开始的任务可以被取消是异步编程的strength之一。这个过程包含是三个步骤：
* 写可以被取消的方法
* 传递令牌（token）
* 发起取消

**Step 1: 写可以被取消的方法**

将令牌（CancellationToken）作为参数传递。然后在方法内部，以某种方式观察该令牌，例如轮询。

```csharp
private async Task MethodCanBeCanceled(CancellationToken token = default)
{
    for (int i = 0; i < 1000; i++)
    {
        Thread.Sleep(100); // simulate cpu bound work
        if (i % 100 == 0)  // control pooling frequency
            token.ThrowIfCancellationRequested();
    }
}
```

上面代码的情形虽然有，但是并不常见。你通常只需要在调用其他库时把CancellationToken传下去，例如：

```csharp
private async Task MethodCanBeCanceledAsync(CancellationToken token = default)
{
    await Task.Delay(TimeSpan.FromSeconds(5), token);
}
```

**Step 2: 传递令牌**

调用可cancel的方法时，传入token即可：

```csharp
var cts = new CancellationTokenSource();
var result = MethodCanBeCanceledAsync(cts.Token);
```

如果任务是通过Task.Run()运行的，方法稍有不同：

```csharp
var cts = new CancellationTokenSource();
Task.Run(() => ,MethodCanBeCanceledAsync(cts.Token), cts.token);
```

注意：直接传递给方法的token由该方法观察处理，在任务启动时也可以取消。但传递给Task.Run()方法的token只能在任务开始运行之前取消任务，这大多数情况下是没用的。

**Step 3: 发起取消**

```csharp
// pass in cts token, and issue cancel from cts.
// Normally, setting up the CancellationTokenSource and issuing the cancellation are in separate methods.
var cts = new CancellationTokenSource();
var task = CancelableMethodAsync(cts.Token);
// some work here
cts.Cancel();  // this will issue the cancel
```

## 总结

1. `Task`抽象是c#异步编程的主要抽象。结合`async/await`关键字以及系统内建的`ThreadPool`，task就可以很方便的被安排到线程池中执行。
2. `await`的使用时关键所在。它代表程序将在这个点等待线程池执行任务，它会抛出说等待的任务的异常。只要理解了`await`所起的作用，异步编程就将变得跟同步编程一样简单。
3. 本文的[示例代码](../../src/dotnet/AsyncProgramming/).
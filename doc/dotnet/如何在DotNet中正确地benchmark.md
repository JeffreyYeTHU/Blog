# 如何在DotNet中正确地benchmark

Benchmark也即基准测试，是性能调优基础。过早的优化是万恶之源，没有基准比较的的优化则是自欺欺人：只有相同的比较基准，才能判断是否完成优化。本文就讲一讲如何正确的benchmark。

## 错误的做法：DateTime

你可能写过这样的代码来计算代码运行所需的时间：
```csharp
private static void WrongWayToTiming()
{
    var before = DateTime.UtcNow;
    DoWork();
    var after = DateTime.UtcNow;
    var timeTake = after - before;
    Console.WriteLine($"Compute Time take to DoWork using DateTime.UtcNow: {timeTake.TotalMilliseconds}ms");
}

// definition of do work
// Warning: this is the wrong way to do large chunk of string manipulation, use StringBuilder instead.
private static void DoWork()
{
    string res = string.Empty;
    for (int i = 0; i < 10000; i++)
    {
        res += i;
    }
}
```

用datetime似乎可以得到函数运行时间。但这只是表象：[MSDN](https://docs.microsoft.com/en-us/dotnet/api/system.datetime.utcnow?view=net-5.0)中明确了，datetime的精度取决于所在操作系统的system timer，因此测量的精度就取决于OS.

## 稍好一点：StopWatch

Ok,怎么可以精确一点？在System.Diagnostics程序集中，有一个Stopwatch类，可以通过计时器刻度来度量时间。

```csharp
private static void SlightlyBetterWayToTiming()
{
    var stopWatch = Stopwatch.StartNew();
    stopWatch.Start();
    DoWork();
    stopWatch.Stop();
    Console.WriteLine($"Compute Time take to DoWork using StopWatch: {stopWatch.ElapsedMilliseconds}ms");
}
```


Stopwatch通过计算基础计时器机制中的计时器刻度来度量经过的时间。如果安装的硬件和操作系统支持高分辨率性能计数器，则Stopwatch类将使用该计数器来测量运行时间。否则，Stopwatch 类将使用系统计时器来测量运行时间。

也就是说，Stopwatch可能比Datetime精确一点，但是情况糟糕的时候，效果是一样的。

## 最佳：Benchmark框架

其实，纠结于单次运行的计时精度会把人误入歧途。在非实时OS上，CPU同时执行很多任务，一个程序只能享有CPU的时间片。因此，不应简单去提高单次计时的精度，而应该以统计的思路：多次测量，然后计算运行时间的均值、方差、百分位等数据，并以此作为运行时间的标准。

你可以自己通过多次运行来实现上述计量效果，但已经有现成的轮子：BenchmarkDotNet.

我们来看一个简单的例子。

```csharp
// Define class to be benchmarked
public class MyStringContactor
{
    private int count = 10000;

    [Benchmark]
    public string UsingPlus()
    {
        string res = string.Empty;
        for (int i = 0; i < count; i++)
        {
            res += i;
        }
        return res;
    }

    [Benchmark]
    public string UsingBuilder()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < count; i++)
        {
            sb.Append(i);
        }
        return sb.ToString();
    }
}

// using runner to run a benchmark
BenchmarkRunner.Run<MyStringContactor>();
```

运行程序，你会发现，Runner会做一系列准备工作，包括：WorkloadPilot、OverheadWarmup、OverheadActual、WorkloadWarmup，然后才是WorkloadActual，默认情况下，WorkloadActual会run15回合，每回合4096次。

完成后，结果会以表格的形式显示出来，非常直观：

```text
| Method       |        Mean |     Error |    StdDev |
| ------------ | ----------: | --------: | --------: |
| UsingPlus    | 21,844.3 us | 421.49 us | 373.64 us |
| UsingBuilder |    135.5 us |   0.73 us |   0.65 us |
```

## 总结

总之，当你需要做benchmark时，最好使用相关的类库。如果是.NET项目，使用BenchmarkDotNet就是很好的选择。
# C#的相等性重写

在Object基类中，有一个虚函数Equals,用于判断本对象与其他对象的相等性，它的签名如下：
```csharp
public virtual bool Equals (object? obj);
```

由于Object类是所有C#类的最终基类，因此，所有对象都会继承这个虚函数。在讨论重载它之前，我们先来看看什么是相等。

## 相等性有什么用？




## 引用相等于值相等
相等分两种，引用相等和值相等。

两个对象，如果它们实际指向同一个内存地址，就称引用相等，可用Object.ReferenceEquals()函数判断。
public class Persion
{
    public string FristName { get; set; }
    public string LastName { get; set; }
}

var p1 = new Persion{ FristName = "Jeffrey", LastName = "Ye"};
var p2 = p1;  // shadow copy, reference to the same object
var p3 = new Persion{ FristName = "Jeffrey", LastName = "Ye"};

// Reference equal result
Console.WriteLine($"Object.ReferenceEquals(p2, p1): {Object.ReferenceEquals(p2, p1)}");
Console.WriteLine($"Object.ReferenceEquals(p3, p1): {Object.ReferenceEquals(p3, p1)}");

// when not override Equals, it is default to be RefferenceEqual.
Console.WriteLine($"p2.Equals(p1): {p2.Equals(p1)}");
Console.WriteLine($"p3.Equals(p1): {p3.Equals(p1)}");
上面的代码中，persion2和persion3浅拷贝persion1，因此它们都指向persion1所在内存，即引用相等。

persion4与persion1有相同的属性，但是persion4是新建的另一个对象，在内存中分别存在因此引用不相等。但由于它们的属性包含的值都相同，因此这两个对象是值相等。
## 如何重载相等

既然要值相等，我们只需要判断两个对象的属性都是否相等即可。
public class PersionA
{
    public string FristName { get; set; }
    public string LastName { get; set; }

    public override bool Equals(Object obj)
    {
        var other = obj as PersionA;
        return this.FristName == other.FristName
            && this.LastName == other.LastName;
    }
}

var pa1 = new PersionA { FristName = "Jeffrey", LastName = "Ye"};
var pa2 = new PersionA { FristName = "Jeffrey", LastName = "Ye"};
Console.WriteLine($"pa1.Equals(pa2): {pa1.Equals(pa2)}");
一切看起来都很完美，我们再试试其他值。
Console.WriteLine($"pa1.Equals(pa2): {pa1.Equals(p1)}");
咦,我们怎么会有一个**NullReferenceException**? 分析我们的实现不难发现，在将传入的对象强制转换为**PersionA**类型时，由于**Persion**类与**PersionA**类不能转换，因此转换的结果是null，从而在接下来的判断中，引发**NullReferenceException**。我们修改实现如下。
public class PersionB
{
    public string FristName { get; set; }
    public string LastName { get; set; }

    public override bool Equals(Object obj)
    {
        var other = obj as PersionB;
        return other is Object
            && this.FristName == other.FristName
            && this.LastName == other.LastName;
    }
}

var pb1 = new PersionB { FristName = "Jeffrey", LastName = "Ye"};
var pb2 = new PersionB { FristName = "Jeffrey", LastName = "Ye"};

Console.WriteLine($"pb1.Equals(pb2): {pb1.Equals(pb2)}");
Console.WriteLine($"pb1.Equals(p1): {pb1.Equals(p1)}");
问题解决，但上面的实现还不完美，这其中有两点：
1. 后面我们会看到，操作符`==`也是可以被重载的，因此，为了保险起见，在重写值相等时,最好避免使用该操作符。
2. 传入的`obj`参数，在逐一比较它们的property之前，我们可以做一些特定的判断来提高性能。

改进后的代码如下。
public class PersionC
{
    public string FristName { get; set; }
    public string LastName { get; set; }

    public override bool Equals(Object obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        var other = obj as PersionC;
        return this.FristName.Equals(other.FristName)
            && this.LastName.Equals(other.LastName);
    }
}
## 重载==及!=操作符

为什么要重载`==`及`!=`呢？为了保持类相等含义的一致性。考虑上一小节实现类，如果不重载这两个操作符，会发生什么？
var pc1 = new PersionC{ FristName = "Jeffrey", LastName = "Ye" };
var pc2 = new PersionC{ FristName = "Jeffrey", LastName = "Ye" };
Console.WriteLine($"pc1.Equals(pc2): {pc1.Equals(pc2)}");
Console.WriteLine($"pc1 == pc2: {pc1 == pc2}");
从代码中可以看到，问题很严重！`==`和Equals一样，当不重载时，默认使用ReferenceEqual，当重载一个而不重载另一个时，就会造成它们的不一致，为使用这个类的人造成困惑，因此，Equals和这两个操作符通常是一起重载的。
public class PersionD
{
    public string FristName { get; set; }
    public string LastName { get; set; }

    public override bool Equals(Object obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        var other = obj as PersionD;
        return this.FristName.Equals(other.FristName)
            && this.LastName.Equals(other.LastName);
    }

    public static bool operator ==(PersionD left, PersionD right)
    {
        return left == right;
    }

    public static bool operator !=(PersionD left, PersionD right)
    {
        return left != right;
    }
}
简单吧！但是我们无意中却引入了一个重大的bug！我们通过测试用例来看一看这个bug。
var pd1 = new PersionD{ FristName = "Jeffrey", LastName = "Ye" };
var pd2 = new PersionD{ FristName = "Jeffrey", LastName = "Ye" };
Console.WriteLine($"pc1 == pc2: {pd1 == pd2}");  // this will be a infinent loop
运行上一个cell将会进入一个死循环，这是因为我们在操作符实现中调用了操作符自己，形成了一个递归调用的死循环！这是实现`==`操作符最需要注意的地方。
public class PersionE
{
    public string FristName { get; set; }
    public string LastName { get; set; }

    public override bool Equals(Object obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        var other = obj as PersionE;
        return this.FristName.Equals(other.FristName)
            && this.LastName.Equals(other.LastName);
    }

    public static bool operator ==(PersionE left, PersionE right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(PersionE left, PersionE right)
    {
        return !(left == right);
    }
}

var pe1 = new PersionE{ FristName = "Jeffrey", LastName = "Ye" };
var pe2 = new PersionE{ FristName = "Jeffrey", LastName = "Ye" };
Console.WriteLine($"pe1.Equals(pe2): {pe1.Equals(pe2)}");
Console.WriteLine($"pc1 == pc2: {pe1 == pe2}");
OK，现在Equals和`==`的结果一样了，我们是否完成任务了呢？
## Override GetHashcode()

当然没有。其实，编译器已经在向我们发出警告：你实现了Equals， 但是没有实现GetHashcode！为什么编译器要发出这个警告呢？

假设现在程序需要组织以Persion为Key的键值对数据结构，我们来看看会发生什么。
Dictionary<PersionE, string> persionPositionDic = new();
persionPositionDic.Add(pe1, "Software Engineer");
persionPositionDic.Add(pe2, "Mechanical Engineer");
上面的代码没有报duplication key error，这是个问题！
string position = persionPositionDic[new PersionE{ FristName = "Jeffrey", LastName = "Ye" }];
用值相等的key无法从字典中取出数据！只有在传入原来的object才能取出数据：
string position = persionPositionDic[pe1];
Console.WriteLine(position);
### 如何重载GetHashcode？

让我们通过重载GetHashcode来解决上面的问题。下面的实现参考了：[Stackoverflow gethashcode question](http://stackoverflow.com/questions/263400/what-is-the-best-algorithm-for-an-overridden-system-object-gethashcode)
public class PersionF
{
    public string FristName { get; set; }
    public string LastName { get; set; }

    public override bool Equals(Object obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        var other = obj as PersionF;
        return this.FristName.Equals(other.FristName)
            && this.LastName.Equals(other.LastName);
    }

    public static bool operator ==(PersionF left, PersionF right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(PersionF left, PersionF right)
    {
        return !(left == right);
    }

    public override int GetHashCode()
    {
        int HashingBase = 13;
        int HashingMultiplier = 7;

        int hash = HashingBase;
        hash = (hash * HashingMultiplier) + (FristName is Object ? FristName.GetHashCode() : 0);
        hash = (hash * HashingMultiplier) + (LastName is Object ? LastName.GetHashCode() : 0);
        return hash;
    }
}
现在使用字典就没有问题了：
Dictionary<PersionF, string> persionPositionDic = new();
persionPositionDic.Add(new PersionF{ FristName = "Jeffrey", LastName = "Ye" }, "Software Engineer");
persionPositionDic.Add(new PersionF{ FristName = "Jeffrey", LastName = "Ye" }, "Mechanical Engineer");
string position = persionPositionDic[new PersionF{ FristName = "Jeffrey", LastName = "Ye" }];
Console.WriteLine(position);
一切看起来都很完美，但事实并非如此。

首先，在hashcode的实现中，整数在被放大，因此有可能溢出。
Console.WriteLine(Int32.MaxValue);
Console.WriteLine(Int32.MaxValue + 10);
我们可以告诉编译器，不用在这里检查，因为hashcode并不关心这个check，放弃check相当于取mod。
```csharp
public override int GetHashCode()
{
    unchecked
    {
        int HashingBase = 13;
        int HashingMultiplier = 7;

        int hash = HashingBase;
        hash = (hash * HashingMultiplier) + (FristName is Object ? FristName.GetHashCode() : 0);
        hash = (hash * HashingMultiplier) + (LastName is Object ? LastName.GetHashCode() : 0);
        return hash;
    }
}
```
其次，我们可以通过把hash base与multiplier增大来降低hash冲突，并通过XOR运算提高效率，改进后的代码：
public class PersionF
{
    public string FristName { get; set; }
    public string LastName { get; set; }

    public override bool Equals(Object obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        var other = obj as PersionF;
        return this.FristName.Equals(other.FristName)
            && this.LastName.Equals(other.LastName);
    }

    public static bool operator ==(PersionF left, PersionF right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(PersionF left, PersionF right)
    {
        return !(left == right);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            // Choose large primes to avoid hashing collisions
            int HashingBase = (int) 2166136261;
            int HashingMultiplier = 16777619;

            // use ^ replace +, to speed up
            int hash = HashingBase;
            hash = (hash * HashingMultiplier) ^ (FristName is null ? 0: FristName.GetHashCode());
            hash = (hash * HashingMultiplier) ^ (LastName is null ? 0: LastName.GetHashCode());
            return hash;
        }
    }
}
如果能使用.NET Core 2.1+, 那么System.HashCode struct可以让事情变得简单。
```csharp
public override int GetHashCode()
{
    var hash = new System.HashCode();
    hash.Add(FirstName);
    hash.Add(LastName);
    return hash.ToHashCode();
}
```
or:
```csharp
public override int GetHashCode()
{
    return System.HashCode.Combine(FirstName, LastName);
}
```

property为null的情形，这个struct也能处理，因此不用担心。
## 结束了吗？

没有。从上面的实现可以发现，要正确重载相等不是一件容易的事。这就要回到最基本的问题，何时应该要重写相等呢？通常是在你需要对象表现出纯数据特征时，也被非正式的成为数据对象。C#9引入了一个叫Record的新类型，专为数据对象而生。
public record PersionG
{
    public string FristName { get; set; }
    public string LastName { get; set; }
}
任务完成！让我们来测试一下：
var pg1 = new PersionG{ FristName = "Jeffrey", LastName = "Ye" };
var pg2 = new PersionG{ FristName = "Jeffrey", LastName = "Ye" };
Console.WriteLine($"pg1.Equals(pg2): {pg1.Equals(pg2)}");
Console.WriteLine($"pg1 == pg2: {pg1 == pg2}");
Dictionary<PersionG, string> persionPositionDic = new();
persionPositionDic.Add(pg1, "Software Engineer");
persionPositionDic.Add(pg2, "Mechanical Engineer");
Console.WriteLine(persionPositionDic[new PersionG{ FristName = "Jeffrey", LastName = "Ye" }]);
## 总结

1. 如果你需要定义的类，概念上更趋向于数据对象（data objects），那么可以考虑使用record，这样就不用操心相等的重载。
2. 如果不适合或者不能使用record，同时还要重载相等，那么你可以参考上面的方法来做。需要记住重载Equals时，通常都需要同时重载GetHashCode，操作符`==`，操作符`!=`.
# C#的相等性重写

C#中，Object类是所有类的最终基类，所有对象都会从Object继承一个虚函数`Equals`,用于判断其他对象与本对象是否相等，其签名如下：
```csharp
public virtual bool Equals (object? obj);
```

也许你并没有显示调用过这个函数，但是它的正确重写对类的正确工作至关重要。

## 相等性有什么用？

其实你已经直接或间接的用过相等性判断，但也许并没有意识到。例如下面的代码：

```csharp
// What is the use of Equals?
string name = "Jeffrey";
if (name == "Jeffrey")  // Direct use: `==` check if two object is equal
    Console.WriteLine($"Hello {name}");

var strList = name.ToList();
int idx = strList.IndexOf('f');  // In-direct use: the process to find the index of char 'f' calls this.Equals(Object)
Console.WriteLine($"Index of 'f' is {idx}");
```

上面的代码分钟：
* 第一个例子中，操作符`==`判断两个对象是否相等，代码编译后悔使用`Equals`方法，可看做直接使用相等性。
* 第二个例子中，首先构建了一个列表，然后在这个表里寻找字符`y`的索引。寻找的过程，实际上就是遍历列表并比较元素是否与待查找的元素相同，若相同就返回当前元素的索引。这是间接使用相等性。因此，相等性在不经意间就会对你的代码运行发生实质性影响。

## 引用相等与值相等

对象相等分两种，引用相等和值相等。

对象存储在内存中，从程序的角度看，对象是指向对象所在的内存地址：在C++里，这就是指针；在C#的世界中，内存由GC管理，指针变成了引用，可看做是类型安全的指针。

两个对象，如果它们实际指向同一个内存地址，实际就是同一个对象，称为引用相等，可用`Object.ReferenceEquals`函数判断。
在没有被重写的情况下，`Equals`会调用`Object.ReferenceEquals`。重写时，通常会查看对象的重要属性是否一致，若重要属性都一致，则是值相等。哪些是重要属性是程序员在重写时决定的。

引用相等实质是同一性（Identity），值相等意在属性比较。打个比方，同卵双生的双胞胎兄弟，他们的Identity不同，但是DNA相同，因此它们不是引用相等；如果`Equals`约定为两个人DNA相等，则这对兄弟就是值相等。

话有些绕，来看看代码。

```csharp
public class Person
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

// Reference equal, it's about identity
var p1 = new Person { FirstName = "Jeffrey", LastName = "Ye" };
var p2 = p1;  // Shadow copy, reference to the same object
var p3 = new Person { FirstName = "Jeffrey", LastName = "Ye" };  // A different person with the same name
Console.WriteLine($"Object.ReferenceEquals(p2, p1): {Object.ReferenceEquals(p2, p1)}");  // true
Console.WriteLine($"Object.ReferenceEquals(p3, p1): {Object.ReferenceEquals(p3, p1)}");  // false

// when not override Equals, it is default to be ReferenceEqual.
Console.WriteLine($"p2.Equals(p1): {p2.Equals(p1)}");  // true
Console.WriteLine($"p3.Equals(p1): {p3.Equals(p1)}");  // false
```

上面的代码中，p2浅拷贝p1，与p1指向同一对象，因此引用相等。p3虽然与p1属性值相同，但是不同的对象，因此引用不相等。

## 如何重写相等

在前述例子中，p1与p3的所有属性都是一样的，我们希望在调用`Equals`时返回`true`，看起来很简单，只要重写方法时比较对象的属性即可。

```csharp
public sealed class PersonA
{
    public string FirstName { get; set; }
    public string LastName { get; set; }

    public override bool Equals(Object obj)
    {
        var other = obj as PersonA;
        return this.FirstName == other.FirstName
            && this.LastName == other.LastName;
    }
}

var pa1 = new PersonA { FirstName = "Jeffrey", LastName = "Ye" };
var pa2 = new PersonA { FirstName = "Jeffrey", LastName = "Ye" };
Console.WriteLine($"pa1.Equals(pa2): {pa1.Equals(pa2)}"); // true
```

一切看起来都很完美，我们再试试其他值。

```csharp
Console.WriteLine($"pa1.Equals(pa2): {pa1.Equals(p1)}");  // p1 is not type of PersonA, will throw NullReferenceException
```

咦,我们怎么会有一个**NullReferenceException**? 分析我们的实现不难发现，在将传入的对象强制转换为**PersonA**类型时，由于**Person**类与**PersonA**类不能转换，因此转换的结果是null，从而在接下来的判断中，引发**NullReferenceException**。这很好修改：

```csharp
public class PersonB
{
    public string FirstName { get; set; }
    public string LastName { get; set; }

    public override bool Equals(Object obj)
    {
        var other = obj as PersonB;
        return other is Object
            && this.FirstName == other.FirstName
            && this.LastName == other.LastName;
    }
}

var pb1 = new PersonB { FirstName = "Jeffrey", LastName = "Ye"};
var pb2 = new PersonB { FirstName = "Jeffrey", LastName = "Ye"};

Console.WriteLine($"pb1.Equals(pb2): {pb1.Equals(pb2)}");
Console.WriteLine($"pb1.Equals(p1): {pb1.Equals(p1)}");
```

问题解决，但上面的实现还不完美，这其中有两点：
1. 后面我们会看到，操作符`==`也是可以被重载的，因此，为了保险起见，在重写值相等时,最好避免使用该操作符。
2. 传入的`obj`参数，在逐一比较它们的property之前，我们可以做一些特定的判断来提高性能。

改进后的代码如下。

```csharp
public class PersonC
{
    public string FirstName { get; set; }
    public string LastName { get; set; }

    public override bool Equals(Object obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        var other = obj as PersonC;
        return this.FirstName.Equals(other.FirstName)
            && this.LastName.Equals(other.LastName);
    }
}
```

## 重载==及!=操作符

为什么要重载`==`及`!=`呢？为了保持类相等含义的一致性。考虑上一小节实现类，如果不重载这两个操作符，会发生什么？

```csharp
var pc1 = new PersonC{ FirstName = "Jeffrey", LastName = "Ye" };
var pc2 = new PersonC{ FirstName = "Jeffrey", LastName = "Ye" };
Console.WriteLine($"pc1.Equals(pc2): {pc1.Equals(pc2)}");
Console.WriteLine($"pc1 == pc2: {pc1 == pc2}");
```

问题很严重：`==`和Equals结果不一样！当不重载时，`==`将使用ReferenceEqual，当`Equals`与`==`、`!=`不同时重载时，就会造成它们的不一致。使用这个类就会造成困惑，因此，Equals和这两个操作符通常是一起重写的。

```csharp
public class PersonD
{
    public string FirstName { get; set; }
    public string LastName { get; set; }

    public override bool Equals(Object obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        var other = obj as PersonD;
        return this.FirstName.Equals(other.FirstName)
            && this.LastName.Equals(other.LastName);
    }

    public static bool operator ==(PersonD left, PersonD right)
    {
        return left == right;
    }

    public static bool operator !=(PersonD left, PersonD right)
    {
        return left != right;
    }
}
```

简单吧！但是我们无意中却引入了一个重大的bug！我们通过测试用例来看一看这个bug。

```csharp
var pd1 = new PersonD{ FirstName = "Jeffrey", LastName = "Ye" };
var pd2 = new PersonD{ FirstName = "Jeffrey", LastName = "Ye" };
Console.WriteLine($"pc1 == pc2: {pd1 == pd2}");  // this will be a infinite loop
```

运行上面的代码将会进入一个死循环，这是因为我们在操作符实现中调用了操作符自己，形成了一个递归调用的死循环！这是实现`==`操作符最需要注意的地方。这个死循环最终会以经典的Stack Overflow错误表现出来:

```
Stack overflow.
Repeat 19270 times:
--------------------------------
   at OverrideEqualsInCSharp.PersonD.op_Equality(OverrideEqualsInCSharp.PersonD, OverrideEqualsInCSharp.PersonD)
--------------------------------
   at OverrideEqualsInCSharp.Program.Main(System.String[])
```

Fix：

```csharp
public class PersonE
{
    public string FirstName { get; set; }
    public string LastName { get; set; }

    public override bool Equals(Object obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        var other = obj as PersonE;
        return this.FirstName.Equals(other.FirstName)
            && this.LastName.Equals(other.LastName);
    }

    public static bool operator ==(PersonE left, PersonE right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(PersonE left, PersonE right)
    {
        return !(left == right);
    }
}

var pe1 = new PersonE{ FirstName = "Jeffrey", LastName = "Ye" };
var pe2 = new PersonE{ FirstName = "Jeffrey", LastName = "Ye" };
Console.WriteLine($"pe1.Equals(pe2): {pe1.Equals(pe2)}");
Console.WriteLine($"pc1 == pc2: {pe1 == pe2}");
```

OK，现在Equals和`==`的结果一样了，我们是否完成任务了呢？

## 重写GetHashcode()

当然没有。其实，编译器已经在向我们大声疾呼：你实现了Equals， 但是没有实现GetHashcode!

```csharp
Severity	Code	Description	Project	File	Line	Suppression State
Warning	CS0659	'PersonE' overrides Object.Equals(object o) but does not override Object.GetHashCode()	OverrideEqualsInCSharp	D:\Jeffery\Github\Blog\src\dotnet\OverrideEqualsInCSharp\Person.cs	11	Active
```

编译器为什么要发出这个警告呢？

假设现在程序需要组织以Person为Key的键值对数据结构，我们来看看会发生什么。

```csharp
Dictionary<PersonE, string> PersonPositionDic = new();
PersonPositionDic.Add(pe1, "Software Engineer");
PersonPositionDic.Add(pe2, "Mechanical Engineer");  // NOT report duplicate key error!

string position = PersonPositionDic[new PersonE{ FirstName = "Jeffrey", LastName = "Ye" }];
string position = PersonPositionDic[pe1];  // Will throw KeyNotFoundException
Console.WriteLine(position);
```

上例中，在从Dic中读取

### 如何重载GetHashcode？

让我们通过重载GetHashcode来解决上面的问题。下面的实现参考了：[Stackoverflow gethashcode question](http://stackoverflow.com/questions/263400/what-is-the-best-algorithm-for-an-overridden-system-object-gethashcode)
public class PersonF
{
    public string FirstName { get; set; }
    public string LastName { get; set; }

    public override bool Equals(Object obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        var other = obj as PersonF;
        return this.FirstName.Equals(other.FirstName)
            && this.LastName.Equals(other.LastName);
    }

    public static bool operator ==(PersonF left, PersonF right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(PersonF left, PersonF right)
    {
        return !(left == right);
    }

    public override int GetHashCode()
    {
        int HashingBase = 13;
        int HashingMultiplier = 7;

        int hash = HashingBase;
        hash = (hash * HashingMultiplier) + (FirstName is Object ? FirstName.GetHashCode() : 0);
        hash = (hash * HashingMultiplier) + (LastName is Object ? LastName.GetHashCode() : 0);
        return hash;
    }
}
现在使用字典就没有问题了：
Dictionary<PersonF, string> PersonPositionDic = new();
PersonPositionDic.Add(new PersonF{ FirstName = "Jeffrey", LastName = "Ye" }, "Software Engineer");
PersonPositionDic.Add(new PersonF{ FirstName = "Jeffrey", LastName = "Ye" }, "Mechanical Engineer");
string position = PersonPositionDic[new PersonF{ FirstName = "Jeffrey", LastName = "Ye" }];
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
        hash = (hash * HashingMultiplier) + (FirstName is Object ? FirstName.GetHashCode() : 0);
        hash = (hash * HashingMultiplier) + (LastName is Object ? LastName.GetHashCode() : 0);
        return hash;
    }
}
```
其次，我们可以通过把hash base与multiplier增大来降低hash冲突，并通过XOR运算提高效率，改进后的代码：
public class PersonF
{
    public string FirstName { get; set; }
    public string LastName { get; set; }

    public override bool Equals(Object obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        var other = obj as PersonF;
        return this.FirstName.Equals(other.FirstName)
            && this.LastName.Equals(other.LastName);
    }

    public static bool operator ==(PersonF left, PersonF right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(PersonF left, PersonF right)
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
            hash = (hash * HashingMultiplier) ^ (FirstName is null ? 0: FirstName.GetHashCode());
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
public record PersonG
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}
任务完成！让我们来测试一下：
var pg1 = new PersonG{ FirstName = "Jeffrey", LastName = "Ye" };
var pg2 = new PersonG{ FirstName = "Jeffrey", LastName = "Ye" };
Console.WriteLine($"pg1.Equals(pg2): {pg1.Equals(pg2)}");
Console.WriteLine($"pg1 == pg2: {pg1 == pg2}");
Dictionary<PersonG, string> PersonPositionDic = new();
PersonPositionDic.Add(pg1, "Software Engineer");
PersonPositionDic.Add(pg2, "Mechanical Engineer");
Console.WriteLine(PersonPositionDic[new PersonG{ FirstName = "Jeffrey", LastName = "Ye" }]);
## 总结

1. 如果你需要定义的类，概念上更趋向于数据对象（data objects），那么可以考虑使用record，这样就不用操心相等的重载。
2. 如果不适合或者不能使用record，同时还要重载相等，那么你可以参考上面的方法来做。需要记住重载Equals时，通常都需要同时重载GetHashCode，操作符`==`，操作符`!=`.
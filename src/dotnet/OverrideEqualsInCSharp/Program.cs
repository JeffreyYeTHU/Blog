using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace OverrideEqualsInCSharp
{
    class Program
    {
        static void Main(string[] args)
        {
            // What is the use of Equals?
            string name = "Jeffrey";
            if (name == "Jeffrey")  // Direct use: `==` check if two object is equal
                Console.WriteLine($"Hello {name}");

            var strList = name.ToList();
            int idx = strList.IndexOf('f');  // In-direct use: the process to find the index of char 'f' calls this.Equals(Object)
            Console.WriteLine($"Index of 'f' is {idx}");
            Console.WriteLine();


            // Reference equal, it's about identity
            var p1 = new Person { FirstName = "Jeffrey", LastName = "Ye" };
            var p2 = p1;  // Shadow copy, reference to the same object
            var p3 = new Person { FirstName = "Jeffrey", LastName = "Ye" };  // A different person with the same name
            Console.WriteLine($"Object.ReferenceEquals(p2, p1): {Object.ReferenceEquals(p2, p1)}");  // true
            Console.WriteLine($"Object.ReferenceEquals(p3, p1): {Object.ReferenceEquals(p3, p1)}");  // false

            // when not override Equals, it is default to be ReferenceEqual.
            Console.WriteLine($"p2.Equals(p1): {p2.Equals(p1)}");  // true
            Console.WriteLine($"p3.Equals(p1): {p3.Equals(p1)}");  // false
            Console.WriteLine();


            // First attempt to override Equals
            var pa1 = new PersonA { FirstName = "Jeffrey", LastName = "Ye" };
            var pa2 = new PersonA { FirstName = "Jeffrey", LastName = "Ye" };
            Console.WriteLine($"pa1.Equals(pa2): {pa1.Equals(pa2)}"); // true
            //Console.WriteLine($"pa1.Equals(pa2): {pa1.Equals(p1)}");  // p1 is not type of PersonA, will throw NullReferenceException
            Console.WriteLine();

            // Second attempt
            var pb1 = new PersonB { FirstName = "Jeffrey", LastName = "Ye" };
            var pb2 = new PersonB { FirstName = "Jeffrey", LastName = "Ye" };
            Console.WriteLine($"pb1.Equals(pb2): {pb1.Equals(pb2)}");
            Console.WriteLine($"pb1.Equals(p1): {pb1.Equals(p1)}");
            Console.WriteLine();


            // How == will act when not override
            var pc1 = new PersonC { FirstName = "Jeffrey", LastName = "Ye" };
            var pc2 = new PersonC { FirstName = "Jeffrey", LastName = "Ye" };
            Console.WriteLine($"pc1.Equals(pc2): {pc1.Equals(pc2)}");
            Console.WriteLine($"pc1 == pc2: {pc1 == pc2}");
            Console.WriteLine();


            // Wrong implementation lead to infinite loop
            var pd1 = new PersonD { FirstName = "Jeffrey", LastName = "Ye" };
            var pd2 = new PersonD { FirstName = "Jeffrey", LastName = "Ye" };
            //Console.WriteLine($"pc1 == pc2: {pd1 == pd2}");  // this will be a infinite loop
            Console.WriteLine();


            // Correct implementation
            var pe1 = new PersonE { FirstName = "Jeffrey", LastName = "Ye" };
            var pe2 = new PersonE { FirstName = "Jeffrey", LastName = "Ye" };
            Console.WriteLine($"pe1.Equals(pe2): {pe1.Equals(pe2)}");
            Console.WriteLine($"pc1 == pc2: {pe1 == pe2}");
            Console.WriteLine();


            // Problem when not override GetHashcode
            Dictionary<PersonE, string> PersonPositionDic = new();
            PersonPositionDic.Add(pe1, "Software Engineer");
            PersonPositionDic.Add(pe2, "Mechanical Engineer");  // NOT report duplicate key error!

            string position = PersonPositionDic[new PersonE { FirstName = "Jeffrey", LastName = "Ye" }];  // Will throw KeyNotFoundException
            string pos = PersonPositionDic[pe1];
            Console.WriteLine(position);
            Console.WriteLine();
        }
    }
}

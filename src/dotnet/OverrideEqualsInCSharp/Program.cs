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
        }
    }
}

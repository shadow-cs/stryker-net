using System;
using System.Collections.Generic;
using System.Text;

namespace StrykerNet.UnitTest.Mutants.TestResources
{
    public class TestClass
    {
        string TestMethod()
        {
            int test = 10;
            test = test + i;
            int testPlusTest = test + test;
            int b = a += 1 + 2;
            var (one, two) = (1 + 1, "");
            int Add(int x, int y) => x + y;
            Action act = () => Console.WriteLine(1 + 1, 1 + 1);

            string someString = "";
            return someString.Replace("ab", "cd")
                .Replace("12", "34");
        }
    }
}

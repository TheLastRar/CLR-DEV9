using System;
using System.Windows.Forms;

namespace MonoTest
{
    public enum TestEnum
    {
        Foo,
        Bar,
    }

    public class TestEnumCtor
    {
        TestEnum value;
        public TestEnumCtor(TestEnum par)
        {
            Console.WriteLine("Ctor Called with " + par.ToString());
            value = par;
        }

        public void PrintValue()
        {
            Console.WriteLine("Stored Value is " + value.ToString());
        }
    }

    public class TestForms
    {
        public static void Test()
        {
            MessageBox.Show("Hello World");
        }
    }
}

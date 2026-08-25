using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChurchSignsLib
{
    public class Class1
    {
        private int _a; private int _b;
        public Class1(int a, int b)
        {
            _a = a; _b = b;
        }

        public int Sum { get { return _a + _b; } }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SignLib
{
    public class Class1
    {
        private int _a; private int _b;
        public Class1(int a, int b)
        {
            _a = a; _b = b;
        }

        public int Sum() { return _a + _b; }
    }
}

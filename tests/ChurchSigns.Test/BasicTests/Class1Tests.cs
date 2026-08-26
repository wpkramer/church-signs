
using SignLib;
namespace ChurchSigns.Test.BasicTests
{
    public class Class1Tests
    {
        [Fact]
        public void Test1()
        {
            Class1 class1 = new Class1(1, 2);
            Assert.True(class1.Sum == 3);
        }
    }
}

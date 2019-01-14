using Perfusion;
using Xunit;

namespace PerfusionTest
{
    public class ContainerTests
    {
        class GuessableType
        {
            public GuessableType() { }
        }
        [Fact]
        public void OnTypeNotFoundTest()
        {
            Container c = new Container();
            c.OnTypeNotFound = (t) => false;
            Assert.Throws<PerfusionException>(() => c.GetInstance<GuessableType>());
        }
    }
}
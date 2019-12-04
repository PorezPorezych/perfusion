using Perfusion;
using Xunit;

namespace PerfusionTest
{
    public class InjectAttributeTests
    {
        class GuessableType : AGuessableType, IGuessableType
        {
            public GuessableType() { }
        }
        interface IGuessableType { }
        abstract class AGuessableType { }
        [Fact]
        public void OptionalInjectTest()
        {
            Container c = new Container();
            object o = c.GetInstance(typeof(GuessableType), false);
            Assert.IsType<GuessableType>(o);
            Assert.NotNull(o);
            object anothero = c.GetInstance(typeof(string), false);
            Assert.Null(anothero);
        }
        [Fact]
        public void RequiredInjectTest()
        {
            Container c = new Container();
            object o = c.GetInstance(typeof(GuessableType), false);
            Assert.IsType<GuessableType>(o);
            Assert.NotNull(o);
            Assert.Throws<PerfusionException>(() => c.GetInstance(typeof(string), true));
        }
    }
}
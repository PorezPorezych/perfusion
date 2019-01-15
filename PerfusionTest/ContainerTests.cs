using Perfusion;
using Xunit;

namespace PerfusionTest
{
    public class ContainerTests
    {
        interface IGuessableType { }
        class GuessableType : IGuessableType { }
        class GuessableType2 : IGuessableType { }
        class AGuessableType : IGuessableType { }
        [Fact]
        public void OnTypeNotFoundTest()
        {
            Container c = new Container();
            c.OnTypeNotFound = (t) => false;
            Assert.Throws<PerfusionException>(() => c.GetInstance<GuessableType>());
        }
        [Fact]
        public void OnManyImplementersTest()
        {
            Container c = new Container();
            c.OnManyImplementers = (t) => typeof(AGuessableType);
            c.Add<GuessableType>();
            c.Add<GuessableType2>();
            IGuessableType i = c.GetInstance<IGuessableType>();
            Assert.NotNull(i);
            Assert.IsType<AGuessableType>(i);
        }
    }
}
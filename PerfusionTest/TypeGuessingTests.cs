using Perfusion;
using Xunit;

namespace PerfusionTest
{
    public class TypeGuessingTests
    {
        [Fact]
        public void GuessType()
        {
            Container c = new Container();
            object gt = c.GetInstance(typeof(GuessableType));
            Assert.NotNull(gt);
            Assert.IsType<GuessableType>(gt);
        }
        [Fact]
        public void GuessTypeWithConstructor()
        {
            Container c = new Container();
            object gt = c.GetInstance(typeof(GuessableTypeWithConstructor));
            Assert.NotNull(gt);
            Assert.IsType<GuessableTypeWithConstructor>(gt);
        }
        [Fact]
        public void GuessInjectionTypeTest()
        {
            Container c = new Container();
            object o = c.GetInstance(typeof(TransientGuessableType));
            Assert.IsType<TransientGuessableType>(o);
            Assert.NotNull(o);
            object anothero = c.GetInstance(typeof(TransientGuessableType));
            Assert.IsType<TransientGuessableType>(anothero);
            Assert.NotNull(anothero);
            Assert.NotSame(o, anothero);
        }
        class GuessableType : AGuessableType, IGuessableType
        {
            public GuessableType() { }
        }
        [Transient]
        class TransientGuessableType : GuessableType
        {
            public TransientGuessableType() { }
        }
        interface IGuessableType { }
        abstract class AGuessableType { }
        class GuessableTypeWithConstructor
        {
            [Inject]
            public GuessableTypeWithConstructor(GuessableType gt)
            {
                Assert.NotNull(gt);
            }
        }
        abstract class AbstractType
        {

        }
        [Fact]
        public void GuessAbstractTest()
        {
            Container c = new Container();
            Assert.Throws<PerfusionException>(() => c.GetInstance<AbstractType>());
        }
        interface InterfaceType
        {

        }
        [Fact]
        public void GuessInterfaceTest()
        {
            Container c = new Container();
            Assert.Throws<PerfusionException>(() => c.GetInstance<InterfaceType>());
        }
    }
}
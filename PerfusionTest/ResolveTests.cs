using Perfusion;
using Xunit;

namespace PerfusionTest
{
    public class ResolveTests
    {
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
        class TypeWithMethod
        {
            public bool gotCalled = false;
            [Inject]
            public void InjectMethod(GuessableType gt)
            {
                gotCalled = true;
                Assert.NotNull(gt);
            }
        }
        [Fact]
        public void ResolveMethodTest()
        {
            Container c = new Container();
            c.AddSingleton(() => new TypeWithMethod());
            c.AddSingleton(() => new GuessableType());
            TypeWithMethod t = c.GetInstance<TypeWithMethod>();
            Assert.True(t.gotCalled);
        }
        class TypeWithField
        {
            [Inject]
            public GuessableType gt;
        }
        [Fact]
        public void ResolveFieldTest()
        {
            Container c = new Container();
            c.AddSingleton(() => new TypeWithField());
            c.AddSingleton(() => new GuessableType());
            TypeWithField t = c.GetInstance<TypeWithField>();
            Assert.NotNull(t.gt);
        }
        class TypeWithProperty
        {
            public bool gotCalled = false;
            [Inject]
            private GuessableType property
            {
                set
                {
                    gotCalled = true;
                }
            }
        }
        [Fact]
        public void ResolvePropertyTest()
        {
            Container c = new Container();
            c.AddSingleton(() => new TypeWithProperty());
            c.AddSingleton(() => new GuessableType());
            TypeWithProperty t = c.GetInstance<TypeWithProperty>();
            Assert.True(t.gotCalled);
        }
    }
}
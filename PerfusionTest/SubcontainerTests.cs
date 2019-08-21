using System;
using Perfusion;
using Xunit;

namespace PerfusionTest
{
    public class SubcontainerTests
    {
        class GuessableType { }
        public class TestInfo : ObjectInfo
        {
            public bool cloned = false;
            public override ObjectInfo Clone()
            {
                cloned = true;
                return this;
            }

            public override object GetInstance(IContainer c, Type requester = null) => throw new NotImplementedException("What");
        }
        [Fact]
        public void CloneObjectInfosTest()
        {
            Container c = new Container();
            TestInfo ti = new TestInfo();
            c.AddInfo<GuessableType>(ti);
            Container subc = c.Subcontainer();
            Assert.True(ti.cloned);
        }
        [Fact]
        public void SingletonDeepTest()
        {
            Container c = new Container();
            SingletonInfo si = new SingletonInfo(() => new GuessableType());
            c.AddInfo<GuessableType>(si);
            Container subc = c.Subcontainer();
            GuessableType inner = subc.GetInstance<GuessableType>();
            GuessableType outer = c.GetInstance<GuessableType>();
            Assert.NotSame(inner, outer);
        }
        [Fact]
        public void SingletonCloneTest()
        {
            Container c = new Container();
            SingletonInfo si = new SingletonInfo(() => new GuessableType());
            c.AddInfo<GuessableType>(si);
            GuessableType outer = c.GetInstance<GuessableType>();
            Container subc = c.Subcontainer();
            GuessableType inner = subc.GetInstance<GuessableType>();
            Assert.Same(inner, outer);
        }
    }
}
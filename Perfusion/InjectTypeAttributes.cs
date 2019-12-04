namespace Perfusion
{
    [System.AttributeUsage(System.AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class SingletonAttribute : System.Attribute
    {
        public SingletonAttribute() { }
    }
    [System.AttributeUsage(System.AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class ScopedAttribute : System.Attribute
    {
        public ScopedAttribute() { }
    }
    [System.AttributeUsage(System.AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class TransientAttribute : System.Attribute
    {
        public TransientAttribute() { }
    }
    [System.AttributeUsage(System.AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class PoolableAttribute : System.Attribute
    {
        public PoolableAttribute(int poolsize = 10) { PoolSize = 10; }

        public int PoolSize { get; }
    }
}
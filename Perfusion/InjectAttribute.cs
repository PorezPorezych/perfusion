namespace Perfusion
{
    [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Method | System.AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class InjectAttribute : System.Attribute
    {
        public InjectAttribute(bool required = true) { Required = required; }

        public bool Required { get; }
    }
    [System.AttributeUsage(System.AttributeTargets.Constructor, Inherited = true, AllowMultiple = false)]
    public sealed class NoInjectAttribute : System.Attribute
    {
        public NoInjectAttribute() { }
    }
}
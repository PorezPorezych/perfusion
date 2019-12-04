using System;
using System.Linq;
using System.Reflection;

namespace Perfusion
{
    public static class ConstructUtils
    {
        public static Func<object> MakeFactoryFor(Type t, IContainer container)
        {
            if (t.GetTypeInfo().DeclaredConstructors.Any(x => x.GetParameters().Length == 0))
                return () => Activator.CreateInstance(t);
            else
            {
                foreach (ConstructorInfo ci in t.GetTypeInfo().DeclaredConstructors)
                {
                    if (ci.CustomAttributes.All(x => x.AttributeType != typeof(NoInjectAttribute)))
                    {
                        return () => ConstructInstanceUsing(ci, container);
                    }
                }
                throw new PerfusionException("No matching constructor");
            }
        }

        public static ObjectInfo MakeInfoFor(Type t, IContainer container) => GuessObjectInfoFor(t, MakeFactoryFor(t, container));

        private static object ConstructInstanceUsing(ConstructorInfo ci, IContainer container)
        {
            ParameterInfo[] parameters = ci.GetParameters();
            object[] paramlist = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo p = parameters[i];
                if (p.ParameterType == ci.DeclaringType)
                    throw new PerfusionException("Dependency loop in " + ci.DeclaringType.GetType());
                paramlist[i] = container.GetInstance(p.ParameterType, true, ci.DeclaringType);
            }
            return ci.Invoke(paramlist);
        }

        public static ObjectInfo GuessObjectInfoFor(Type t, Func<object> factory)
        {
            if (t.CustomAttributes.Any(x => x.AttributeType == typeof(SingletonAttribute)))
                return new SingletonInfo(factory);
            if (t.CustomAttributes.Any(x => x.AttributeType == typeof(ScopedAttribute)))
                return new ScopedInfo(factory);
            else if (t.CustomAttributes.Any(x => x.AttributeType == typeof(TransientAttribute)))
                return new TransientInfo(factory);
            else if (t.CustomAttributes.Any(x => x.AttributeType == typeof(PoolableAttribute)))
                return new PoolableInfo(factory, (int)(t.CustomAttributes.First(x => x.AttributeType == typeof(PoolableAttribute)).ConstructorArguments[0].Value));
            else return new SingletonInfo(factory);
        }
    }
}
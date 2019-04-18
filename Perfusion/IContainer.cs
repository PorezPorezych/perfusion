using System;
using System.Collections.Generic;

namespace Perfusion
{
    public interface IContainer
    {
        T ResolveObject<T>(T obj);
        object GetInstance(Type t, bool required = true, Type requester = null);
        IReadOnlyDictionary<Type, ObjectInfo> RegisteredObjects { get; }

        void Add(Type t);
        void AddInfo(Type t, ObjectInfo i);
    }
    public static class ContainerExtensions
    {
        public static void AddSingleton<TContract>(this IContainer c, Func<TContract> F) where TContract : class
        {
            c.AddInfo(typeof(TContract), new SingletonInfo(F));
        }
        public static void AddSingleton(this IContainer c, Type t, Func<object> F)
        {
            c.AddInfo(t, new SingletonInfo(F));
        }
        public static void AddTransient<TContract>(this IContainer c, Func<TContract> F) where TContract : class
        {
            c.AddInfo(typeof(TContract), new TransientInfo(F));
        }
        public static void AddTransient(this IContainer c, Type t, Func<object> F)
        {
            c.AddInfo(t, new TransientInfo(F));
        }
        public static void AddPoolable<TContract>(this IContainer c, Func<TContract> F, int poolsize) where TContract : class
        {
            c.AddInfo(typeof(TContract), new PoolableInfo(F, poolsize));
        }
        public static void AddPoolable(this IContainer c, Type t, Func<object> F, int poolsize)
        {
            c.AddInfo(t, new PoolableInfo(F, poolsize));
        }
        public static void Add<T>(this IContainer c)
        {
            c.Add(typeof(T));
        }
        public static void AddInfo<T>(this IContainer c, ObjectInfo o)
        {
            c.AddInfo(typeof(T), o);
        }
        public static void AddInstance<TContract>(this IContainer c, TContract f) where TContract : class
        {
            c.AddInfo(typeof(TContract), new SingletonInfo(() => f));
        }
        public static void AddInstance(this IContainer c, Type t, object f)
        {
            c.AddInfo(t, new SingletonInfo(() => f));
        }
        public static T GetInstance<T>(this IContainer c, bool required = true, Type requester = null) where T : class
        {
            return (T)c.GetInstance(typeof(T), required, requester);
        }
    }
    public abstract class ObjectInfo
    {
        public Type Type;
        public abstract object GetInstance(IContainer c, Type requester = null);
    }
}

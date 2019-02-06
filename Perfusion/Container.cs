using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Perfusion
{
    public delegate bool TypeNotFoundHandler(Type t);
    public delegate Type ManyImplementersHandler(ObjectInfo[] i);
    public class Container
    {
        public const BindingFlags ALL_INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        Dictionary<Type, ObjectInfo> objects = new Dictionary<Type, ObjectInfo>();
        public IReadOnlyDictionary<Type, ObjectInfo> RegisteredObjects => objects;

        public TypeNotFoundHandler OnTypeNotFound { get; set; }
        public ManyImplementersHandler OnManyImplementers { get; set; }

        #region AddX
        public void AddSingleton<TContract>(Func<TContract> F) where TContract : class
        {
            AddSingleton(typeof(TContract), F);
        }
        public void AddSingleton(Type t, Func<object> F)
        {
            AddInfo(t, new SingletonInfo(F));
        }
        public void AddTransient<TContract>(Func<TContract> F) where TContract : class
        {
            AddTransient(typeof(TContract), F);
        }
        public void AddTransient(Type t, Func<object> F)
        {
            AddInfo(t, new TransientInfo(F));
        }
        public void AddPoolable<TContract>(Func<TContract> F, int poolsize) where TContract : class
        {
            AddPoolable(typeof(TContract), F, poolsize);
        }
        public void AddPoolable(Type t, Func<object> F, int poolsize)
        {
            AddInfo(t, new PoolableInfo(F, poolsize));
        }
        public void Add(Type t)
        {
            if (!tryAddGuessing(t))
            {
                throw new PerfusionException("Type not able to be guessed: " + t);
            }
        }
        public void Add<T>() => Add(typeof(T));

        public void AddInstance<TContract>(TContract f) where TContract : class => AddSingleton(() => f);

        public void AddInfo<T>(ObjectInfo i)
        {
            AddInfo(typeof(T), i);
        }
        public void AddInfo(Type t, ObjectInfo i)
        {
            i.Type = t;
            objects[t] = i;
        }
        #endregion

        public T ResolveObject<T>(T o)
        {
            Type t = o.GetType();
            foreach (FieldInfo f in t.GetFields(ALL_INSTANCE))
            {
                if (f.CustomAttributes.Any(x => x.AttributeType == typeof(InjectAttribute)))
                {
                    if (f.FieldType == t)
                        throw new PerfusionException("Dependency loop in " + o.GetType());
                    bool required = (bool)f.CustomAttributes.First(x => x.AttributeType == typeof(InjectAttribute)).ConstructorArguments[0].Value;
                    f.SetValue(o, GetInstance(f.FieldType, required, t));
                }
            }
            foreach (PropertyInfo p in t.GetProperties(ALL_INSTANCE))
            {
                if (p.CustomAttributes.Any(x => x.AttributeType == typeof(InjectAttribute)))
                {
                    if (p.PropertyType == t)
                        throw new PerfusionException("Dependency loop in " + o.GetType());
                    bool required = (bool)p.CustomAttributes.First(x => x.AttributeType == typeof(InjectAttribute)).ConstructorArguments[0].Value;
                    p.SetValue(o, GetInstance(p.PropertyType, required, t));
                }
            }
            foreach (MethodInfo m in t.GetMethods(ALL_INSTANCE))
            {
                if (m.CustomAttributes.Any(x => x.AttributeType == typeof(InjectAttribute)))
                {
                    object[] param = new object[m.GetParameters().Count()];
                    int i = 0;
                    foreach (ParameterInfo v in m.GetParameters())
                    {
                        if (v.ParameterType == t)
                            throw new PerfusionException("Dependency loop in " + o.GetType());
                        bool required = (bool)m.CustomAttributes.First(x => x.AttributeType == typeof(InjectAttribute)).ConstructorArguments[0].Value;
                        param[i] = GetInstance(v.ParameterType, required, t);
                        i++;
                    }
                    m.Invoke(o, param);
                }
            }
            return o;
        }


        private object buildWithConstructor(Type t, ConstructorInfo c)
        {
            object[] paramlist = new object[c.GetParameters().Length];
            for (int i = 0; i < paramlist.Length; i++)
            {
                ParameterInfo p = c.GetParameters()[i];
                if (p.ParameterType == t)
                    throw new PerfusionException("Dependency loop in " + t.GetType());
                paramlist[i] = GetInstance(p.ParameterType, true, t);
            }
            object created = Activator.CreateInstance(t, paramlist);
            ResolveObject(created);
            return created;
        }

        public T GetInstance<T>(Type requester = null) where T : class => (T)GetInstance(typeof(T), requester: requester);
        private ObjectInfo guessInjectionType(Type t, Func<object> factory)
        {
            if (t.CustomAttributes.Any(x => x.AttributeType == typeof(SingletonAttribute)))
                return new SingletonInfo(factory);
            else if (t.CustomAttributes.Any(x => x.AttributeType == typeof(TransientAttribute)))
                return new TransientInfo(factory);
            else if (t.CustomAttributes.Any(x => x.AttributeType == typeof(PoolableAttribute)))
                return new PoolableInfo(factory, (int)(t.CustomAttributes.First(x => x.AttributeType == typeof(PoolableAttribute)).ConstructorArguments[0].Value));
            else return new SingletonInfo(factory);
        }
        private bool tryAddGuessing(Type t)
        {
            if (t.GetTypeInfo().DeclaredConstructors.Any(x => x.GetParameters().Length == 0))
            {
                AddInfo(t, guessInjectionType(t, () => Activator.CreateInstance(t)));
                return true;
            }
            else
            {
                foreach (ConstructorInfo ci in t.GetTypeInfo().DeclaredConstructors)
                {
                    if (ci.CustomAttributes.Any(x => x.AttributeType == typeof(InjectAttribute)))
                    {
                        AddInfo(t, guessInjectionType(t, () => buildWithConstructor(t, ci)));
                        return true;
                    }
                }
                return false;
            }
        }
        public object GetInstance(Type t, bool required = true, Type requester = null)
        {
            if (!required)
            {
                try
                {
                    return GetInstance(t);
                }
                catch (PerfusionException)
                {
                    return null;
                }
            }
            KeyValuePair<Type, ObjectInfo>[] possibleImplementors = objects.Where(x => x.Key.GetInterfaces().Concat(GetHierarchy(x.Key)).Contains(t)).ToArray();

            if (possibleImplementors.Length > 1)
            {
                Type te = OnManyImplementers(possibleImplementors.Select(x => x.Value).ToArray());
                if (te == null)
                    throw new PerfusionException("Many possible implementors: " + string.Join(", ", possibleImplementors));
                else
                    return GetInstance(te);
            }
            if (possibleImplementors.Length == 0)
            {
                if (!t.IsAbstract && !t.IsInterface)
                {
                    if (!OnTypeNotFound(t))
                    {
                        throw new PerfusionException("Type not found: " + t);
                    }
                    else
                    {
                        return GetInstance(t, requester: requester); //use recursion
                    }
                }
                else
                {
                    throw new PerfusionException("Object implementing " + t.FullName + " not found");
                }
            }
            return ResolveObject(possibleImplementors[0].Value.GetInstance(requester));
        }

        public Container()
        {
            OnTypeNotFound = tryAddGuessing;
            OnManyImplementers = (t) => null;
            AddInstance(this);
        }

        #region service 

        IEnumerable<Type> GetHierarchy(Type T)
        {
            for (; T != null; T = T.BaseType)
                yield return T;
        }

        #endregion
    }

    public abstract class ObjectInfo
    {
        public Type Type;
        public abstract object GetInstance(Type requester = null);
    }
    public class SingletonInfo : ObjectInfo
    {
        public Func<object> Factory;
        public bool IsInstantiated = false;
        public object Value;
        public override object GetInstance(Type requester = null)
        {
            if (!IsInstantiated)
            {
                IsInstantiated = true;
                return Value = Factory();
            }
            else
                return Value;
        }
        public SingletonInfo(Func<Object> factory)
        {
            Factory = factory;
        }
        public SingletonInfo() { }
    }
    public class TransientInfo : ObjectInfo
    {
        public Func<object> Factory;
        public override object GetInstance(Type requester = null) => Factory();
        public TransientInfo(Func<Object> factory)
        {
            Factory = factory;
        }
        public TransientInfo() { }
    }
    public class PoolableInfo : ObjectInfo
    {
        public Func<object> Factory;
        public override object GetInstance(Type requester = null)
        {
            if (pool.Count < PoolSize)
            {
                object o = Factory();
                pool[o] = 1;
                return o;
            }
            else
            {
                object least = pool.OrderBy(x => x.Value).First().Key;
                pool[least] = pool[least] + 1;
                return least;
            }
        }
        private Dictionary<object, int> pool;
        public int PoolSize { get; }
        public PoolableInfo(Func<Object> factory, int poolsize)
        {
            Factory = factory;
            PoolSize = poolsize;
            pool = new Dictionary<object, int>(poolsize);
        }
        public PoolableInfo(int poolsize)
        {
            PoolSize = poolsize;
            pool = new Dictionary<object, int>(poolsize);
        }
    }
}

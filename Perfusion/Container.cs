using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Perfusion
{
    public delegate bool TypeNotFoundHandler(Type t);
    public delegate Type ManyImplementersHandler(ObjectInfo[] i);
    public class Container : IContainer
    {
        public const BindingFlags ALL_INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        Dictionary<Type, ObjectInfo> objects = new Dictionary<Type, ObjectInfo>();
        private readonly object objectsLock = new object();
        public IReadOnlyDictionary<Type, ObjectInfo> RegisteredObjects => objects;

        public TypeNotFoundHandler OnTypeNotFound { get; set; }
        public ManyImplementersHandler OnManyImplementers { get; set; }

        #region AddX
        public void Add(Type t)
        {
            AddInfo(t, ConstructUtils.MakeInfoFor(t, this));
        }
        public void AddInfo(Type t, ObjectInfo i)
        {
            i.Type = t;
            lock (objectsLock)
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
            KeyValuePair<Type, ObjectInfo>[] possibleImplementors;
            lock (objectsLock)
                possibleImplementors = objects.Where(x => x.Key.GetInterfaces().Concat(GetHierarchy(x.Key)).Contains(t)).ToArray();

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
            return possibleImplementors[0].Value.GetInstance(this, requester);
        }

        public IEnumerable<object> GetInstances(Type t, Type requester = null)
        {
            KeyValuePair<Type, ObjectInfo>[] possibleImplementors;
            lock (objectsLock)
                possibleImplementors = objects.Where(x => x.Key.GetInterfaces().Concat(GetHierarchy(x.Key)).Contains(t)).ToArray();

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
                        return GetInstances(t, requester: requester); //use recursion
                    }
                }
                else
                {
                    throw new PerfusionException("Object implementing " + t.FullName + " not found");
                }
            }
            return possibleImplementors.Select(x => x.Value.GetInstance(this, requester));
        }

        public Container Subcontainer()
        {
            Container c = new Container();

            foreach (ObjectInfo oi in RegisteredObjects.Values)
            {
                if (oi.Type == typeof(Container))
                    continue; // Autocreated
                c.AddInfo(oi.Type, oi.Clone());
            }

            return c;
        }

        public Container()
        {
            OnTypeNotFound = (t) =>
            {
                try
                {
                    AddInfo(t, ConstructUtils.MakeInfoFor(t, this));
                    return true;
                }
                catch (PerfusionException)
                {
                    return false;
                }
            };
            OnManyImplementers = (t) => null;
            AddInfo(typeof(Container), new SingletonInfo(() => this));
        }

        #region service 

        IEnumerable<Type> GetHierarchy(Type T)
        {
            for (; T != null; T = T.BaseType)
                yield return T;
        }

        public object GetService(Type serviceType) => GetInstance(serviceType);

        #endregion
    }
    public class SingletonInfo : ObjectInfo
    {
        public Func<object> Factory;
        public bool IsInstantiated = false;
        public object Value;
        private readonly object valueLock = new object();
        public override object GetInstance(IContainer c, Type requester = null)
        {
            lock (valueLock)
                if (!IsInstantiated)
                {
                    IsInstantiated = true;
                    return Value = c.ResolveObject(Factory());
                }
                else
                    return Value;
        }

        public override ObjectInfo Clone() => this; //Pass self to share state

        public SingletonInfo(Func<Object> factory)
        {
            Factory = factory;
        }
        public SingletonInfo() { }
    }
    public class ScopedInfo : ObjectInfo
    {
        public Func<object> Factory;
        public bool IsInstantiated = false;
        public object Value;
        private readonly object valueLock = new object();
        public override object GetInstance(IContainer c, Type requester = null)
        {
            lock (valueLock)
                if (!IsInstantiated)
                {
                    IsInstantiated = true;
                    return Value = c.ResolveObject(Factory());
                }
                else
                    return Value;
        }

        public override ObjectInfo Clone() => new ScopedInfo(Factory);
        public ScopedInfo(Func<Object> factory)
        {
            Factory = factory;
        }
        public ScopedInfo() { }
    }
    public class TransientInfo : ObjectInfo
    {
        public Func<object> Factory;
        public override object GetInstance(IContainer c, Type requester = null) => c.ResolveObject(Factory());

        public override ObjectInfo Clone() => new TransientInfo(Factory);

        public TransientInfo(Func<Object> factory)
        {
            Factory = factory;
        }
        public TransientInfo() { }
    }
    public class PoolableInfo : ObjectInfo
    {
        public Func<object> Factory;
        private readonly object poolLock = new object();
        public override object GetInstance(IContainer c, Type requester = null)
        {
            lock (poolLock)
                if (pool.Count < PoolSize)
                {
                    object o = c.ResolveObject(Factory());
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

        public override ObjectInfo Clone() => new PoolableInfo(Factory, PoolSize, pool);

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
        protected PoolableInfo(Func<Object> factory, int poolsize, Dictionary<object, int> pool)
        {
            Factory = factory;
            PoolSize = poolsize;
            pool = pool.ToDictionary(x => x.Key, x => x.Value);
        }
    }
}

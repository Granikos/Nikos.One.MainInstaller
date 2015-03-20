using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DeploymentTools
{
internal abstract class ReflectionWrapper
{
    private readonly object _innerObject;

    protected ReflectionWrapper(object innerObject)
    {
        _innerObject = innerObject;
    }

    protected Func<T> GetPropertyGetter<T>(string name)
    {
        if (_innerObject == null)
        {
            return () => default(T);
        }

        var property = _innerObject.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
        var mi = property.GetMethod;
        var result = (Func<T>)mi.CreateDelegate(typeof(Func<T>), _innerObject);
        return result;
    }

    /// <summary>
    /// Adds the event handler.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="name">The name.</param>
    /// <param name="handler">The handler.</param>
    /// <param name="wrap">if set to <c>true</c> [wrap].</param>
    protected void AddEventHandler<T>(string name, EventHandler<T> handler, bool wrap = false)
        where T : EventArgs
    {
        var evt = _innerObject.GetType().GetEvent(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

        if (wrap)
        {
            var argsType = evt.EventHandlerType.GetGenericArguments().Last();
            var paramSender = Expression.Parameter(typeof(object));
            var paramArgs = Expression.Parameter(argsType);
            Expression<EventHandler<T>> bind = (s, e) => handler(s, e);

            var invoke = Expression.Invoke(bind, paramSender, paramArgs);
            var lambda = Expression.Lambda(evt.EventHandlerType, invoke, paramSender, paramArgs);
            var wrappingHandler = lambda.Compile();
            evt.AddMethod.Invoke(_innerObject, new object[] { wrappingHandler });
        }
        else
        {
            evt.AddMethod.Invoke(_innerObject, new object[] { handler });
        }
    }
}
}
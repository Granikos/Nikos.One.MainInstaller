using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Web.PlatformInstaller.UI;

namespace WpiWrapper
{
    static class ReflectionExtensions
    {
        public static Type GetWpiUiType(string name)
        {
            return typeof(ManagementFrameFull).Assembly.GetType(name);
        }

        public static object GetService(this IServiceProvider serviceProvider, string serviceName)
        {
            var type = GetWpiUiType("Microsoft.Web.PlatformInstaller.UI." + serviceName);
            var service = serviceProvider.GetService(type);
            return service;
        }

        public static void SetField(this object o, string memberName, object value)
        {
            if (o == null) return;
            var member = o.GetType().GetField(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy);

            if (member == null) return;
            member.SetValue(member.IsStatic ? null : o, value);
        }

        public static object GetField(this object o, string memberName)
        {
            if (o == null) return null;
            var member = o.GetType().GetField(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy);

            return member == null ? null : member.GetValue(member.IsStatic ? null : o);
        }

        public static void SetProperty(this object o, string memberName, object value)
        {
            if (o == null) return;
            var member = o.GetType().GetProperty(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy);

            if (member == null) return;
            member.SetValue(o, value);
        }

        public static object GetProperty(this object o, string memberName)
        {
            if (o == null) return null;
            var member = o.GetType().GetProperty(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy);

            return member == null ? null : member.GetValue(o);
        }

        public static T GetMethod<T>(this object o, string memberName) where T : class
        {
            if (o == null) return null;
            var member = o.GetType().GetMethod(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy);

            return member == null ? null : member.CreateDelegate(typeof(T), member.IsStatic ? null : o) as T;
        }

        public static void AddEventHandler<T>(this object o, string name, EventHandler<T> handler, bool wrap = false)
            where T : EventArgs
        {
            var evt = o.GetType().GetEvent(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

            if (wrap)
            {
                var argsType = evt.EventHandlerType.GetGenericArguments().Last();
                var paramSender = Expression.Parameter(typeof(object));
                var paramArgs = Expression.Parameter(argsType);
                Expression<EventHandler<T>> bind = (s, e) => handler(s, e);

                var invoke = Expression.Invoke(bind, paramSender, paramArgs);
                var lambda = Expression.Lambda(evt.EventHandlerType, invoke, paramSender, paramArgs);
                var wrappingHandler = lambda.Compile();
                evt.AddMethod.Invoke(o, new object[] { wrappingHandler });
            }
            else
            {
                evt.AddMethod.Invoke(o, new object[] { handler });
            }
        }
    }
}

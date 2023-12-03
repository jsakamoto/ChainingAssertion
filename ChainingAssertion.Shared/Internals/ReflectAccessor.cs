using System;
using System.Reflection;

namespace ChainingAssertion.Shared.Internals
{
    internal class ReflectAccessor<T>
    {
        public Func<object> GetValue { get; private set; }
        public Action<object> SetValue { get; private set; }

        public ReflectAccessor(T target, string name)
        {
            var field = typeof(T).GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
            {
                this.GetValue = () => field.GetValue(target);
                this.SetValue = value => field.SetValue(target, value);
                return;
            }

            var prop = typeof(T).GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null)
            {
                this.GetValue = () => prop.GetValue(target, null);
                this.SetValue = value => prop.SetValue(target, value, null);
                return;
            }

            throw new ArgumentException(string.Format("\"{0}\" not found : Type <{1}>", name, typeof(T).Name));
        }
    }
}
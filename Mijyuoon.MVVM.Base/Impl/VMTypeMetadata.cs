using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Mijyuoon.MVVM.Impl {
    class VMTypeMetadata {
        public struct PropertyInfo {
            public string Name;
            public Func<object, object> Getter;
            public Action<object, object> Setter;
        }

        public Dictionary<string, PropertyInfo> ModelProps;

        private VMTypeMetadata(Type modelType, Type selfType) {
            ModelProps = new Dictionary<string, PropertyInfo>();

            var props = modelType.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            foreach(var prop in props) {
                var pInfo = new PropertyInfo { Name = prop.Name };

                if(prop.CanRead) {
                    var oParam = Expression.Parameter(typeof(object), "obj");
                    var propRef = Expression.Property(Expression.Convert(oParam, modelType), prop);
                    var retval = Expression.Convert(propRef, typeof(object));
                    var func = Expression.Lambda<Func<object, object>>(retval, new[] { oParam });

                    pInfo.Getter = func.Compile();
                }

                if(prop.CanWrite) {
                    var oParam = Expression.Parameter(typeof(object), "obj");
                    var vParam = Expression.Parameter(typeof(object), "value");
                    var propRef = Expression.Property(Expression.Convert(oParam, modelType), prop);
                    var assign = Expression.Assign(propRef, Expression.Convert(vParam, prop.PropertyType));
                    var func = Expression.Lambda<Action<object, object>>(assign, new[] { oParam, vParam });

                    pInfo.Setter = func.Compile();
                }

                ModelProps.Add(prop.Name, pInfo);
            }
        }

        private static Dictionary<Type, VMTypeMetadata> Cache =
            new Dictionary<Type, VMTypeMetadata>();

        public static VMTypeMetadata Get(Type modelType, Type selfType) {
            if(!Cache.TryGetValue(selfType, out var metadata)) {
                metadata = new VMTypeMetadata(modelType, selfType);
                Cache.Add(selfType, metadata);
            }

            return metadata;
        }
    }
}

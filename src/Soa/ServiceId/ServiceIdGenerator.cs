﻿using Abp.Dependency;
using System.Linq;
using System.Reflection;

namespace Soa.ServiceId
{
    public class ServiceIdGenerator : IServiceIdGenerator, ISingletonDependency
    {
        public string GenerateServiceId(MethodInfo method)
        {
            if (method.DeclaringType == null) return null;
            var id = $"{method.DeclaringType.FullName}.{method.Name}";
            var paras = method.GetParameters();
            if (paras.Any()) id += "(" + string.Join(",", paras.Select(i => i.Name)) + ")";
            return id;

        }
    }
}
using System.Reflection;
using Tracker.AspNet.Attributes;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services.Contracts;

namespace Tracker.AspNet.Services;

public class DefaultActionsDescriptorProvider : IActionsDescriptorProvider
{
    public virtual IEnumerable<ActionDescriptor> GetActionsDescriptors(params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.DefinedTypes)
            {
                if (!IsSuitControllerType(type))
                    continue;

                foreach (var method in type.DeclaredMethods)
                {
                    if (!IsSuitActionMethod(method, type))
                        continue;

                    var trackAttr = method.GetCustomAttribute<TrackAttribute>();
                    if (trackAttr is null)
                        continue;

                    yield return new ActionDescriptor
                    {
                        Route = trackAttr.Route,
                        Tables = trackAttr.Tables
                    };
                }
            }
        }
    }

    private static bool IsSuitControllerType(TypeInfo typeInfo) =>
        typeInfo.IsClass && !typeInfo.IsAbstract && !typeInfo.ContainsGenericParameters;

    private static bool IsSuitActionMethod(MethodInfo methodInfo, Type controllerType) =>
        methodInfo.IsPublic && methodInfo.DeclaringType == controllerType && !methodInfo.IsSpecialName && !methodInfo.IsStatic;
}

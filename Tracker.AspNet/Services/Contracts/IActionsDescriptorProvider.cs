using System.Reflection;
using Tracker.AspNet.Models;

namespace Tracker.AspNet.Services.Contracts;

public interface IActionsDescriptorProvider
{
    IEnumerable<ActionDescriptor> GetActionsDescriptors(params Assembly[] assemblies);
}

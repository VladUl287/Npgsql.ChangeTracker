using Microsoft.AspNetCore.Http;
using System.Runtime.CompilerServices;

namespace Tracker.AspNet.Extensions;

public static class HttpContextExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsGetRequest(this HttpContext context) => context.Request.Method == HttpMethod.Get.Method;
}

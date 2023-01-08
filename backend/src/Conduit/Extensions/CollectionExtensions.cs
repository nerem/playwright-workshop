using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Conduit.Extensions;

public static class CollectionExtensions
{
    public static void Do<T>(this IEnumerable<T> self, Action<T> action)
    {
        foreach (var element in self)
        {
            action.Invoke(element);
        }
    }

    public static async Task DoAsync<T>(this IEnumerable<T> self, Func<T, Task> action)
    {
        foreach (var element in self)
        {
            await action.Invoke(element);
        }
    }
}

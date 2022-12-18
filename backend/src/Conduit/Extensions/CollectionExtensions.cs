using System;
using System.Collections.Generic;

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
}

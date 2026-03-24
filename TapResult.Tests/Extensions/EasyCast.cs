using System.Diagnostics;

namespace TapResult.Tests.Extensions;

internal static class EasyCast
{
    public static TOut Expect<TOut>(this object inp)
    {
        if (inp is TOut o)
        {
            return o;
        }
        Assert.Fail($"Expected {typeof(TOut).FullName} but got {inp.GetType().FullName}");
        throw new UnreachableException();
    }
}
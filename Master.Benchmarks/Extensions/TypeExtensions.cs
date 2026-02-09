namespace Master.Benchmarks.Extensions;

public static class TypeExtensions
{
    public static Type GetUnderlyingNullableType(this Type type)
    { 
        return Nullable.GetUnderlyingType(type) ?? type;
    }
}
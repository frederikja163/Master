namespace Master.Extensions;

internal static class TypeExtensions
{
    public static Type GetUnderlyingNullableType(this Type type)
    { 
        return Nullable.GetUnderlyingType(type) ?? type;
    }

    public static bool IsNullable(this Type type)
    {
        return Nullable.GetUnderlyingType(type) is not null;
    }
}
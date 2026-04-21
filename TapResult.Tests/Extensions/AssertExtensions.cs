namespace TapResult.Tests.Extensions;

internal static class AssertExtensions
{
    extension(Assert)
    {
        public static T InstanceOf<T>(object value)
        {
            Assert.That(value, Is.InstanceOf<T>());
            return (T)value;
        }
    }
}
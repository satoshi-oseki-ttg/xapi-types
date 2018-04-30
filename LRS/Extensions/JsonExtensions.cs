using System;

namespace bracken_lrs.JsonExtensions
{
    public static class JsonExtensions
    {
        public static bool IsNumberType(this Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            return type == typeof(long)
                || type == typeof(ulong)
                || type == typeof(int)
                || type == typeof(uint)
                || type == typeof(short)
                || type == typeof(ushort)
                || type == typeof(byte)
                || type == typeof(sbyte)
                || type == typeof(System.Numerics.BigInteger);
        }

        public static bool IsBoolType(this Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            return type == typeof(bool);
        }        
    }
}
    
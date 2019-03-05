using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace NEL.Simple.SDK.Helper
{
    public static class Helper
    {
        public static byte[] ToBytes(this byte b)
        {
            return new byte[1] {b };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe public static uint ToUInt32(this byte[] value, int startIndex)
        {
            fixed (byte* pbyte = &value[startIndex])
            {
                return *((uint*)pbyte);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe internal static ushort ToUInt16(this byte[] value, int startIndex)
        {
            fixed (byte* pbyte = &value[startIndex])
            {
                return *((ushort*)pbyte);
            }
        }
    }
}

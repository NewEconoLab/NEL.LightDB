using System;
using System.Collections.Generic;
using System.Text;

namespace NEL.Simple.SDK.Helper
{
    public static class Helper
    {
        public static byte[] ToBytes(this byte b)
        {
            return new byte[1] {b };
        }
    }
}

using System;
using System.Collections.Generic;

namespace SubCTools.Droid.Extensions
{
    public static class ByteExtentions
    {
        public static IEnumerable<byte[]> SplitByteArray(this byte[] source, int size)
        {
            for (int i = 0; i < source.Length; i += size)
            {
                byte[] buffer = new byte[size];

                if (source.Length - i < size)
                {
                    Buffer.BlockCopy(source, i, buffer, 0, source.Length - i);
                }
                else
                {
                    Buffer.BlockCopy(source, i, buffer, 0, size);
                }

                yield return buffer;
            }
        }

    }
}
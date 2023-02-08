
using System;
using System.Collections.Generic;
using System.Text;

namespace MedFasee.Utils
{
    public static class ArrayUtils
    {
        public static bool SmallerThan<T>(T[] index, T[] limit) where T : struct
        {
            if (index.Length != limit.Length)
                throw new ArgumentException("Index array must have same length as limit array");

            for(int i = 0; i < index.Length; i++)
            {
                int c = Comparer<T>.Default.Compare(index[i], limit[i]);
                if (c >= 0)
                    return false;
            }
            return true;
        }
    }
}

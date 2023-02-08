
using System;
using System.Collections.Generic;
using System.Text;

namespace MedFasee.Utils
{
    public static class NumberUtils
    {
        public static T Max<T>(params T[] numbers) where T : struct
        {
            if (numbers.Length == 0)
                return default(T);

            T max = numbers[0];
            for(int i = 1; i < numbers.Length; i++)
            {
                int c = Comparer<T>.Default.Compare(max, numbers[i]);
                if (c < 0)
                    max = numbers[i];
            }
            return max;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sitecore.SharedSource.Dataset.Extensions
{
    public static class IEnumerableExtensions
    {
        public static IEnumerable<T> Random<T>(this IEnumerable<T> source, int n)
        {
            Random rnd = new Random((int)DateTime.Now.Ticks);
            IEnumerable<T> result = source.OrderBy(r => rnd.Next());

            if (n < result.Count())
                result = result.Take(n);
                
            return result;
        }
    }
}

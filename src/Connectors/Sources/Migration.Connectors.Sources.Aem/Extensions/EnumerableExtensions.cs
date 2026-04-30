using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Sources.Aem.Extensions
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<List<T>> Batch<T>(this IEnumerable<T> source, int size)
        {
            List<T> bucket = new(size);
            foreach (var item in source)
            {
                bucket.Add(item);
                if (bucket.Count == size)
                {
                    yield return new List<T>(bucket);
                    bucket.Clear();
                }
            }

            if (bucket.Count > 0)
            {
                yield return bucket;
            }
        }
    }
}

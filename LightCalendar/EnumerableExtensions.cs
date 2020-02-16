 using System;
 using System.Collections.Generic;
 using System.Linq;
 using System.Threading;
 
[assembly: CLSCompliant(true)]

namespace LightCalendar
{
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Perform zip over unequal length collections padding with default values
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<TResult> Zip<TFirst, TSecond, TResult>(
            this IEnumerable<TFirst> first,
            IEnumerable<TSecond> second,
            Func<TFirst, TSecond, TResult> resultSelector, TSecond padValue) 
            => first.Zip(second.Concat(Enumerable.Repeat(padValue, int.MaxValue)), resultSelector);

        public static Lazy<T> Lazy<T>(Func<T> f) => new Lazy<T>(f, LazyThreadSafetyMode.ExecutionAndPublication);
        
    }
}

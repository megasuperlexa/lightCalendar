using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using static LightCalendar.DateTimeExtensions;
using static LightCalendar.EnumerableExtensions;

namespace LightCalendar
{
    /// <summary>
    /// Period represents an interval in time that spans across multiple dates.  
    /// It is possible to get Days, Months, Years from a period, as well as overlap, intersect, exclude and combine them.
    /// </summary>
    public readonly struct Period
    {
        private readonly Lazy<ImmutableArray<DateTime>> _getSchedule;
        
        public Period(DateTime begin, DateTime end)
        {
            Begin = begin;
            End = end;
            IsEmpty = begin > end;
            _getSchedule = Lazy(()
                => begin > end ? ImmutableArray<DateTime>.Empty :
                    Enumerable.Range(0, int.MaxValue)
                    .Select(e => begin.AddDays(e))
                    .TakeWhile(e => !(begin > end) && e <= end)
                    .ToImmutableArray());
        }

        public DateTime Begin { get; }

        public DateTime End { get; }

        public static Period FromMonth(DateTime month) => new Period(month.GetMonthStart(), month.GetMonthEnd());

        public static Period OneYearAhead(DateTime date)
        {
            var monthAndYear = date.GetMonthAndYear();
            return new Period(monthAndYear, monthAndYear.AddYears(1).AddDays(-1));
        }

        public override string ToString()
         => Begin.ToShortDateString() + " - " + End.ToShortDateString();

        /// <summary>
        /// Empty period that cannot be reasonably used in calculations
        /// </summary>
        public bool IsEmpty { get; }

        /// <summary>
        /// Daily schedule
        /// </summary>
        /// <returns></returns>
        public IReadOnlyCollection<DateTime> GetDailySchedule() => _getSchedule.Value;

        public static implicit operator Period(in (DateTime, DateTime) p) => new Period(p.Item1, p.Item2);

        public static bool operator ==(in Period p1, in Period p2) => p1.Begin == p2.Begin && p1.End == p2.End;

        public static bool operator !=(in Period p1, in Period p2) => !(p1 == p2);

        public override bool Equals(object obj) => obj is Period period && this == period;

        public bool Equals(in Period other) => Begin.Equals(other.Begin) && End.Equals(other.End);

        public override int GetHashCode()
        {
            unchecked
            {
                return (Begin.GetHashCode() * 397) ^ End.GetHashCode();
            }
        }
    }

    /// <summary>
    /// Period utility functions
    /// </summary>
    public static class PeriodExtensions
    {
        /// <summary>
        /// Monthly schedule
        /// </summary>
        /// <param name="frequency"></param>
        /// <returns></returns>
        public static IEnumerable<DateTime> GetMonthlySchedule(in this Period p, int frequency)
        {
            var beg = p.Begin.GetMonthAndYear();
            var end = p.End.GetMonthAndYear();
            return GetSchedule(frequency, beg, end);
        }

        /// <summary>
        /// Gets month-length periods from given period
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static IEnumerable<Period> AsMonths(in this Period p)
            => p.GetMonthlySchedule(1).Select(Period.FromMonth);

        /// <summary>
        /// Yearly schedule
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<DateTime> GetYearlySchedule(this Period p)
        {
            var beg = p.Begin.GetMonthAndYear();
            var end = p.End.GetMonthAndYear();
            var my = Enumerable.Range(0, int.MaxValue)
                               .Select(e => beg.AddYears(e))
                               .TakeWhile(e => !p.IsEmpty && e <= end);
            return my;
        }

        /// <summary>
        /// Months in period. Cannot be 0, 1 is minimum
        /// </summary>
        /// <returns></returns>
        public static int GetMonthCount(in this Period p)
        {
            var endMonth = p.End == p.End.GetMonthEnd() ? p.End.Month + 1 : p.End.Month; // count 31st of the same month as next month because technically the month has passed
            return Math.Max(1, (p.End.Year * 12) - (p.Begin.Year * 12) + endMonth - p.Begin.Month);
        }

        /// <summary>
        /// Days in period. Can be 0
        /// </summary>
        /// <returns>0 if period is a single date (1 day) because it is considered empty and not a proper period</returns>
        public static int GetDayCount(in this Period p)
            => p.IsEmpty ? 0 : (p.End - p.Begin).Days + 1;

        private static IEnumerable<DateTime> GetSchedule(int frequency, DateTime beg, DateTime end)
            => Enumerable.Range(0, int.MaxValue)
                .Select(e => beg.AddMonths(e * frequency))
                .TakeWhile(e => e <= end);

        public static bool Contains(in this Period p, DateTime d)
            => p.Begin <= d && d <= p.End;

        public static Period Overlap(in this Period p1, in Period p2)
            => (Max(p1.Begin, p2.Begin), Min(p1.End, p2.End));

        public static Period Exclude(in this Period p1, in Period p2)
        {
            var over = p1.Overlap(p2);
            return over.IsEmpty ? p1
                : p1.Begin == over.Begin
                    ? new Period(over.End.AddDays(1), p1.End)
                    : new Period(p1.Begin, over.Begin);
        }

        public static bool Contains(in this Period p1, in Period p2) => p1.Begin <= p2.Begin && p1.End >= p2.End;

        public static bool Overlaps(in this Period p1, in Period p2) => !p1.Overlap(p2).IsEmpty;

        public static Period Combine(in this Period p1, in Period p2)
            => new Period(Min(p1.Begin, p2.Begin), Max(p1.End, p2.End));

        public static Period Shift(in this Period p, TimeSpan shift)
            => new Period(p.Begin.Add(shift), p.End.Add(shift));

        /// <summary>
        /// Transforms date - value stream into respective periods with values
        /// </summary>
        /// <param name="period"></param>
        /// <param name="dateValues"></param>
        /// <returns>Rates and their respective ranges inside period. Can be empty</returns>
        public static Dictionary<Period, T> GetPeriodValues<T>(this Period period, IEnumerable<(DateTime date, T val)> dateValues)
        {
            // partial periods logic
            var defaultValue = new { rateDate = DateTime.MaxValue, value = default(T) };
            var enumerable = dateValues.OrderBy(_ => _.date).Select(_ => new { rateDate = _.date, value = _.val }).ToList();
            var periodRates = (from datePair in enumerable.Zip(enumerable.Skip(1), Tuple.Create, defaultValue)
                               let ratePeriod = new Period(datePair.Item1.rateDate, datePair.Item2.rateDate.AddDays(-1)).Overlap(period) // use only common portion of periods
                               where !ratePeriod.IsEmpty && !datePair.Item1.value.Equals(default)
                               select (ratePeriod, rate: datePair.Item1.value))
                               .ToDictionary(_ => _.ratePeriod, _ => _.rate);
            return periodRates;
        }

        /// <summary>
        /// Creates empty period from given asOfDate
        /// </summary>
        /// <param name="endDate"></param>
        /// <returns></returns>
        public static Period Empty(DateTime endDate)
            => new Period(endDate.AddDays(1), endDate);

        /// <summary>
        /// Find min and max business days inside periods
        /// </summary>
        /// <param name="periods"></param>
        /// <param name="holidaysCalendar">Holidays</param>
        /// <returns></returns>
        public static Period MinMax(this IEnumerable<Period> periods, IReadOnlyCollection<DateTime> holidaysCalendar)
        {
            // get period min and max boundaries
            var (minDate, maxDate) = periods.Aggregate(
                (mindate: DateTime.MaxValue, maxdate: DateTime.MinValue),
                (a, b) => (Min(a.mindate, b.Begin), Max(a.maxdate, b.End)));
            // these dates might be holidaysCalendar. Expand period if needed
            minDate = Min(minDate, holidaysCalendar.GetPrevBusinessDay(minDate));
            maxDate = Max(maxDate, holidaysCalendar.GetNextBusinessDay(maxDate));
            return (minDate, maxDate);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace LightCalendar
{
    using static DayOfWeek;

    public static class DateTimeExtensions
    {
        private static Calendar calendar_ = CultureInfo.InvariantCulture.Calendar;
        
        /// <summary>
        /// Compare only month and year portion
        /// </summary>
        /// <param name="d1"></param>
        /// <param name="d2"></param>
        /// <returns></returns>
        public static int CompareMonthYear(this DateTime d1, DateTime d2) => DateTime.Compare(d1.GetMonthStart(), d2.GetMonthStart());

        public static DateTime GetMonthAndYear(this DateTime date) => new DateTime(date.Year, date.Month, 1);

        public static DateTime GetMonthStart(this DateTime d) => d.AddDays(-d.Day + 1);

        public static DateTime GetMonthEnd(this DateTime d) => new DateTime(d.Year, d.Month, calendar_.GetDaysInMonth(d.Year, d.Month));

        /// <summary>
        /// Gets portion of the year that is past to asOfDate
        /// </summary>
        /// <param name="d"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public static int GetApplicableDaycount(this DateTime year, DateTime asOfDate)
        {
            if (year > asOfDate)
            {
                return -1;
            }

            return year.Year == asOfDate.Year ? (asOfDate - year).Days + 1 : year.GetYearDaycount();
        }

        public static int GetYearDaycount(this DateTime year) => calendar_.GetDaysInYear(year.Year);

        public static int GetMonthDaycount(this DateTime month) => calendar_.GetDaysInMonth(month.Year, month.Month);

        public static bool IsInPeriod(this DateTime d, Period p) => p.Contains(d);

        public static DateTime Max(params DateTime[] dates) => dates.Max();

        public static DateTime Min(params DateTime[] dates) => dates.Min();

        public static DateTime GetLastWeekDay(this DateTime date) => date.IsWeekend() ? GetLastWeekDay(date.AddDays(-1)) : date;

        public static bool IsWeekend(this DateTime date) => date.DayOfWeek == Saturday || date.DayOfWeek == Sunday;

        /// <summary>
        /// Get year DayCount ahead
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static int GetYearDayCountAhead(this DateTime date) => (calendar_.AddYears(date, 1) - date).Days;

        public static DateTime ChangeYear(this DateTime date, DateTime year) => calendar_.AddYears(date, year.Year - date.Year);

        public static int DaysToMonthEnd(this DateTime date) => calendar_.GetDaysInMonth(date.Year, date.Month) - calendar_.GetDayOfMonth(date) + 1;

        public static int DaysToMonthBegin(this DateTime date) => calendar_.GetDayOfMonth(date);

        public static int DaysToYearEnd(this DateTime date) => calendar_.GetDaysInYear(date.Year) - calendar_.GetDayOfYear(date) + 1;
        
        public static bool IsHoliday(this IReadOnlyCollection<DateTime> holidaysCalendar, DateTime date) => date.IsWeekend() || holidaysCalendar.Contains(date);

        public static bool IsBusinessDay(this IReadOnlyCollection<DateTime> holidaysCalendar, DateTime date) => !IsHoliday(holidaysCalendar, date);

        /// <summary>
        /// Jump to previous working day
        /// </summary>
        /// <param name="holidaysCalendar"></param>
        /// <param name="date"></param>
        /// <param name="rigidMonth">Month cannot be changed. If month changes when going backwards - jump to next business date</param>
        /// <returns></returns>
        public static DateTime GetPrevBusinessDay(this IReadOnlyCollection<DateTime> holidaysCalendar, DateTime date, bool rigidMonth = false)
        {
            if (date == DateTime.MinValue)
            {
                return date;
            }

            var newDate = date.AddDays(-1);
            return newDate.Month != date.Month && rigidMonth ? holidaysCalendar.GetNextBusinessDay(date) : // month crossed and split month logic is effective
                   holidaysCalendar.IsHoliday(newDate) ?
                   holidaysCalendar.GetPrevBusinessDay(newDate, rigidMonth) :
                   newDate;
        }

        /// <summary>
        /// Jump to next working day
        /// </summary>
        /// <param name="holidaysCalendar"></param>
        /// <param name="date"></param>
        /// <param name="rigidMonth">Month cannot be changed. If month changes going forwards - jump to previous business date</param>
        /// <returns></returns>
        public static DateTime GetNextBusinessDay(this IReadOnlyCollection<DateTime> holidaysCalendar, DateTime date, bool rigidMonth = false)
        {
            if (date == DateTime.MaxValue)
            {
                return date;
            }

            var newDate = date.AddDays(1);
            return newDate.Month != date.Month && rigidMonth ? holidaysCalendar.GetPrevBusinessDay(date) :
                holidaysCalendar.IsHoliday(newDate) ?
                holidaysCalendar.GetNextBusinessDay(newDate, rigidMonth) :
                newDate;
        }

        /// <summary>
        /// Gets previous and next business dates for a given date
        /// </summary>
        public static (DateTime prevBusinessDate, DateTime nextBusinessDate) GetBusinessDays(this IReadOnlyCollection<DateTime> holidaysCalendar, DateTime date)
            => (holidaysCalendar.GetPrevBusinessDay(date),
                holidaysCalendar.GetNextBusinessDay(date));
        
    }
}

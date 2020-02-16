 using System;

 namespace LightCalendar
{
    /// <summary>
    /// FiscalYear is a Period for year that start and end differently from calendar years.
    /// Can be created using current date and known year end date.
    /// Takes into account leap years.
    /// </summary>
    public static class FiscalYear
    {
        public static Period GetFiscalYear(DateTime fiscalYearEnd, DateTime asOfDate)
        {
            DateTime begin, end;
            if (asOfDate.Month > fiscalYearEnd.Month)
            {
                begin = CreateFiscalYearEndDateTime(asOfDate.Year, fiscalYearEnd.Month, fiscalYearEnd.Day).AddDays(1);
                end = begin.AddYears(1).AddDays(-1);
            }
            else
            {
                end = CreateFiscalYearEndDateTime(asOfDate.Year, fiscalYearEnd.Month, fiscalYearEnd.Day);
                begin = FixLeapFebruary(end.AddYears(-1)).AddDays(1);
            }

            return (begin, end);
        }

        private static DateTime FixLeapFebruary(DateTime date)
        {
            if (date.Month == 2 && DateTime.IsLeapYear(date.Year))
            {
                date = new DateTime(date.Year, 2, 29);
            }

            return date;
        }

        private static DateTime CreateFiscalYearEndDateTime(int year, int month, int day)
        {
          if (month == 2)
          {
            day = DateTime.IsLeapYear(year) ? 29 : 28;
          }

          return new DateTime(year, month, day);
        }
    }
}

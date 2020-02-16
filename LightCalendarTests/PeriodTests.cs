using ApprovalTests;
using ApprovalTests.Reporters;
using LightCalendar;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using Xunit;

[assembly:UseReporter(typeof(DiffReporter))]

namespace LightCalendarTests
{
    public class PeriodTests
    {
        public PeriodTests()
        {
            CultureInfo newCulture = (CultureInfo)Thread.CurrentThread.CurrentCulture.Clone();
            newCulture.DateTimeFormat.ShortDatePattern = "MM/dd/yyyy";
            Thread.CurrentThread.CurrentCulture = newCulture;
        }
        
        [Fact]
        public void DayCountTest()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var today = new Period(now, now);
            var todayTomorrow = new Period(now, now.AddDays(1));
            var todayYesterday = new Period(now, now.AddDays(-1));
            // Act
            // Assert
            Assert.False(today.IsEmpty);
            Assert.Equal(1, today.GetDayCount());
            
            Assert.Equal(2, todayTomorrow.GetDayCount()); // count inclusive
            
            Assert.True(todayYesterday.IsEmpty); // Empty period that ended before beginning
            Assert.Equal(0, todayYesterday.GetDayCount());
            
            Assert.Empty(todayYesterday.GetDailySchedule());
        }

        [Fact]
        public void MonthCountTest()
        {
            // Arrange
            var p = new Period(new DateTime(2015, 10, 7), new DateTime(2016, 10, 7));
            var p2 = new Period(new DateTime(2015, 10, 1), new DateTime(2015, 11, 1));
            var p3 = new Period(new DateTime(2015, 10, 1), new DateTime(2015, 10, 31));
            // Assert
            Assert.Equal(12, p.GetMonthCount());
            Assert.Equal(1, p2.GetMonthCount());
            Assert.Equal(1, p3.GetMonthCount());
        }

        [Fact]
        public void EqualityTest()
        {
            var p1 = new Period(new DateTime(2017, 10, 2), new DateTime(2017, 10, 20));
            var p2 = new Period(new DateTime(2017, 10, 2), new DateTime(2017, 10, 20));
            var p3 = (new DateTime(2017, 10, 2), new DateTime(2017, 10, 20));
            var p4 = (new DateTime(2016, 10, 2), new DateTime(2017, 10, 20));

            Assert.Equal(p1, p2);
            Assert.Equal(p1, p3);
            Assert.NotEqual(p1, p4);

            var f1 = FiscalYear.GetFiscalYear(new DateTime(2017, 10, 2), new DateTime(2017, 10, 3));
            // for fiscal year, only month and day are relevant for fiscal year end
            var f2 = FiscalYear.GetFiscalYear(new DateTime(2010, 10, 2), new DateTime(2017, 10, 3));

            Assert.Equal(f1, f2);
        }
        
        [Fact]
        public void OverlapTest()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var today = new Period(now, now);
            var todayTomorrow = new Period(now, now.AddDays(1));
            var todayYesterday = new Period(now, now.AddDays(-1));
            var yesterdayTomorrow = new Period(now.AddDays(-1), now.AddDays(1));
            var monthAheadPlusDay = new Period(now.AddMonths(1), now.AddMonths(1).AddDays(1));
            var monthBackwardsAndDay = new Period(now.AddMonths(-1), now.AddMonths(-1).AddDays(1));
            // Act
            var overlap1 = today.Overlap(todayTomorrow);
            var overlapWithEmpty = todayTomorrow.Overlap(todayYesterday);
            var overlap2 = todayTomorrow.Overlap(monthAheadPlusDay);
            var overlap3 = todayTomorrow.Overlap(monthBackwardsAndDay);
            var overlap4 = yesterdayTomorrow.Overlap(todayTomorrow);
            // Assert
            Assert.False(overlap1.IsEmpty);
            Assert.Equal(1, overlap1.GetDayCount());
            Assert.True(overlapWithEmpty.IsEmpty); // overlaping with empty
            Assert.True(overlap2.IsEmpty); // overlaping separate periods
            Assert.True(overlap3.IsEmpty);
            Assert.Equal(2, overlap4.GetDayCount());
            Assert.True(yesterdayTomorrow.Contains(todayTomorrow));
        }
        
        [Fact]
        public void CombineTest()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var todayTomorrow = new Period(now, now.AddDays(1));
            var fourDays = new Period(now.AddDays(1), now.AddDays(4));
            // Act
            var combine1 = todayTomorrow.Combine(fourDays);
            // Assert
            Assert.Equal(todayTomorrow.Begin, combine1.Begin);
            Assert.Equal(fourDays.End, combine1.End);
            Assert.Equal(5, combine1.GetDayCount());
        }
        
        [Fact]
        public void ExcludeTest()
        {
            // Arrange
            var now = new DateTime(2000, 06, 15);
            var todayTomorrow = new Period(now, now.AddDays(1));
            var fourDays = new Period(now, now.AddDays(4));
            // Act
            var excluded = fourDays.Exclude(todayTomorrow);
            // Assert
            Approvals.VerifyAll(excluded.GetDailySchedule(), "Day");
            
        }
        
        [Fact]
        public void FiscalYearTest()
        {
            // Arrange
            var fy = FiscalYear.GetFiscalYear(new DateTime(2000,06,15), new DateTime(2020,11,1));
            
            Approvals.VerifyAll(fy.GetDailySchedule(), "Day");
        }

        [Fact]
        public void PeriodRatesTest()
        {
            var fy = FiscalYear.GetFiscalYear(new DateTime(2000,01,01), new DateTime(2020,11,1));
            var periods = fy.GetPeriodValues(new[]
            {
                (new DateTime(2020, 01, 01), .01),
                (new DateTime(2020, 05, 01), .015),
                (new DateTime(2020, 08, 15), .007),
            });
            // Assert
            Approvals.VerifyAll(periods.ToArray(), "Rate");
        }
        
        [Fact]
        public void FederalFyTest()
        {
            //FY 2020 is the budget for October 1, 2019 through September 30, 2020. 
            var fy = FiscalYear.GetFiscalYear(new DateTime(2000,09,30), new DateTime(2020,1,1));
            // Assert
            Assert.Equal(new DateTime(2019, 10, 01),fy.Begin);
            Assert.Equal(new DateTime(2020, 09, 30),fy.End);
        }
        
        [Fact]
        public void SeasonsTest()
        {
            var fy = FiscalYear.GetFiscalYear(new DateTime(2000,01,01), new DateTime(2020,11,1));
            var periods = fy.GetPeriodValues(new[]     
            {
                (new DateTime(2020, 01, 01), "Winter"),
                (new DateTime(2020, 03, 01), "Spring"),
                (new DateTime(2020, 06, 01), "Summer"),
                (new DateTime(2020, 09, 01), "Winter again"),
            });
            
            Approvals.VerifyAll(periods.ToArray(), "Season");
        }
    }
}

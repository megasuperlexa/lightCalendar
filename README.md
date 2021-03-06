# lightCalendar
[![Build status](https://ci.appveyor.com/api/projects/status/has18vp1j5lgdgqo?svg=true)](https://ci.appveyor.com/project/megasuperlexa/lightcalendar)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](https://opensource.org/licenses/Apache-2.0)
[![NuGet Status](http://img.shields.io/nuget/v/LightCalendar.svg?style=flat)](https://www.nuget.org/packages/LightCalendar/)

Lightweight .Net library for time intervals.

It is a frequent need for projects to have ability to operate time periods that span across several days. 
This library aims to fix the lack of standard means of doing that.
## Period
The main building block is Period struct. It has Start and End:
```cs
public readonly struct Period
{
    public Period(DateTime begin, DateTime end);
        
    public DateTime Begin { get; }
    public DateTime End { get; }
}
```
You create Periods like this:
```cs
var p = new Period(new DateTime(2020, 01, 07), new DateTime(2020, 10, 17));
```
Or using utility function:
```cs
var currentMonth = Period.FromMonth(DateTime.UtcNow);
```
or
```cs
var aYearFromNow = Period.OneYearAhead(DateTime.UtcNow);
```
It is possible to get Days, Months, Years from a period, as well as overlap, intersect, exclude and combine them.
```cs
var now = DateTime.UtcNow;
var todayTomorrow = new Period(now, now.AddDays(1));
var yesterdayTomorrow = new Period(now.AddDays(-1), now.AddDays(1));
var overlap = yesterdayTomorrow.Overlap(todayTomorrow);
Console.WriteLine(overlap.GetDayCount()); // 2
var days = overlap.GetDailySchedule(); // [0] = {DateTime} "2/16/2020 2:34:50 PM"
                                       // [1] = {DateTime} "2/17/2020 2:34:50 PM"
```
or check if a date falls whitin given period:
```cs
overlap.Contains(new DateTime(2006, 1, 1)); //false
```
or check if a period entirely falls into another period:
```cs
yesterdayTomorrow.Contains(todayTomorrow); // true
```
There are more examples in unit tests, please check them out.

## Fiscal year
Another useful abstraction is FiscalYear. A fiscal year is a 12-month period that an organization uses to report its finances. 
It does not necesserily starts or ends with calendar year. Leap year is taken into account, so if a Fiscal year ends on February there will not be a jump to March on non-leap years.
The most important fiscal year for the economy is the federal government's fiscal year. It defines the U.S. government's budget. It runs from October 1 of the budget's prior year through September 30 of the year being described. 
```cs
// Create currently running Federal Fiscal Year
var federalFiscalYear = FiscalYear.GetFiscalYear(new DateTime(2000,09,30), DateTime.UtcNow);
Console.WriteLine(federalFiscalYear); // 10/1/2019 - 9/30/2020
```
Because fiscal year is a Period, you can do all usual stuff with it: overlap, intersect, exclude and combine with other Periods.
Refer to unit tests in this project for more ideas.

## Weeks
Version 1.1.0 adds weeks creator. Weeks can start on different week days depending on standard. You can create several week factories and use them (e.g. via IoC injection) dynamically to create Period for any date's running week.
```cs
// Create factories for week period that differ by week beginning day
var weekCreator = Period.FromWeekFactory(DayOfWeek.Monday);
var weekCreator2 = Period.FromWeekFactory(DayOfWeek.Sunday);
var weekCreator3 = Period.FromWeekFactory(DayOfWeek.Saturday);

var currentIsoWeek = weekCreator(DateTime.UtcNow);
var currentAmericanWeek = weekCreator2(DateTime.UtcNow);
var currentMiddleEasternWeek = weekCreator3(DateTime.UtcNow);

Console.WriteLine(currentIsoWeek); // 03/02/2020 - 03/08/2020
Console.WriteLine(currentIsoWeek.Begin.DayOfWeek); // Monday
Console.WriteLine(currentIsoWeek.GetDayCount()); // 7
Console.WriteLine(currentIsoWeek.GetDailySchedule());
//[0] = {DateTime} "03/02/2020 5:12:50 PM"
//[1] = {DateTime} "03/03/2020 5:12:50 PM"
//[2] = {DateTime} "03/04/2020 5:12:50 PM"
//[3] = {DateTime} "03/05/2020 5:12:50 PM"
//[4] = {DateTime} "03/06/2020 5:12:50 PM"
//[5] = {DateTime} "03/07/2020 5:12:50 PM"
//[6] = {DateTime} "03/08/2020 5:12:50 PM"

Console.WriteLine(currentAmericanWeek.GetDailySchedule());
//[0] = {DateTime} "03/01/2020 5:20:38 PM"
//[1] = {DateTime} "03/02/2020 5:20:38 PM"
//[2] = {DateTime} "03/03/2020 5:20:38 PM"
//[3] = {DateTime} "03/04/2020 5:20:38 PM"
//[4] = {DateTime} "03/05/2020 5:20:38 PM"
//[5] = {DateTime} "03/06/2020 5:20:38 PM"
//[6] = {DateTime} "03/07/2020 5:20:38 PM"

```




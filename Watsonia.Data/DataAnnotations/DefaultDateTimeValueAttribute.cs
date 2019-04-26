using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Watsonia.Data.DataAnnotations
{
	public sealed class DefaultDateTimeValueAttribute : DefaultValueAttribute
	{
		/// <summary>
		/// Initializes a new instance of a DefaultDateTimeAttribute with a specified
		/// number of ticks.
		/// </summary>
		/// <param name="ticks">A date and time expressed in 100-nanosecond units.</param>
		/// <exception cref="ArgumentOutOfRangeException">ticks is less than System.DateTime.MinValue or greater than System.DateTime.MaxValue.</exception>
		public DefaultDateTimeValueAttribute(long ticks)
			: base(new DateTime(ticks))
		{
		}

		/// <summary>
		/// Initializes a new instance of a DefaultDateTimeAttribute with a specified
		/// number of ticks and to Coordinated Universal Time (UTC) or local time.
		/// </summary>
		/// <param name="ticks">A date and time expressed in 100-nanosecond units.</param>
		/// <param name="kind">One of the enumeration values that indicates whether ticks specifies a local
		/// time, Coordinated Universal Time (UTC), or neither.</param>
		/// <exception cref="ArgumentOutOfRangeException">ticks is less than System.DateTime.MinValue or greater than System.DateTime.MaxValue.</exception>
		/// <exception cref="ArgumentException">kind is not one of the System.DateTimeKind values.</exception>
		public DefaultDateTimeValueAttribute(long ticks, DateTimeKind kind)
			: base(new DateTime(ticks, kind))
		{
		}

		/// <summary>
		/// Initializes a new instance of a DefaultDateTimeAttribute with the specified
		/// year, month, and day.
		/// </summary>
		/// <param name="year">The year (1 through 9999).</param>
		/// <param name="month">The month (1 through 12).</param>
		/// <param name="day">The day (1 thrtough the number of days in month).</param>
		/// <exception cref="ArgumentOutOfRangeException">year is less than 1 or greater than 9999.-or- month is less than 1 or greater
		/// than 12.-or- day is less than 1 or greater than the number of days in month.</exception>
		/// <exception cref="ArgumentException">The specified parameters evaluate to less than System.DateTime.MinValue or
		/// more than System.DateTime.MaxValue.</exception>
		public DefaultDateTimeValueAttribute(int year, int month, int day)
			: base(new DateTime(year, month, day))
		{
		}

		/// <summary>
		/// Initializes a new instance of a DefaultDateTimeAttribute with the specified
		/// year, month, and day for the specified calendar.
		/// </summary>
		/// <param name="year">The year (1 through the number of years in calendar).</param>
		/// <param name="month">The month (1 through the number of months in calendar).</param>
		/// <param name="day">The day (1 through the number of days in month).</param>
		/// <param name="calendar">The calendar that is used to interpret year, month, and day.</param>
		/// <exception cref="ArgumentNullException">calendar is null.</exception>
		/// <exception cref="ArgumentOutOfRangeException">year is not in the range supported by calendar.-or- month is less than 1
		/// or greater than the number of months in calendar.-or- day is less than 1
		/// or greater than the number of days in month.</exception>
		/// <exception cref="ArgumentException">The specified parameters evaluate to less than System.DateTime.MinValue or
		/// more than System.DateTime.MaxValue.</exception>
		public DefaultDateTimeValueAttribute(int year, int month, int day, Calendar calendar)
			: base(new DateTime(year, month, day, calendar))
		{
		}

		/// <summary>
		/// Initializes a new instance of a DefaultDateTimeAttribute with the specified
		/// year, month, day, hour, minute, and second.
		/// </summary>
		/// <param name="year">The year (1 through 9999).</param>
		/// <param name="month">The month (1 through 12).</param>
		/// <param name="day">The day (1 through the number of days in month).</param>
		/// <param name="hour">The hours (0 through 23).</param>
		/// <param name="minute">The minutes (0 through 59).</param>
		/// <param name="second">The seconds (0 through 59).</param>
		/// <exception cref="ArgumentOutOfRangeException">year is less than 1 or greater than 9999. -or- month is less than 1 or greater
		/// than 12. -or- day is less than 1 or greater than the number of days in month.-or-
		/// hour is less than 0 or greater than 23. -or- minute is less than 0 or greater
		/// than 59. -or- second is less than 0 or greater than 59.</exception>
		/// <exception cref="ArgumentException">The specified parameters evaluate to less than System.DateTime.MinValue or
		/// more than System.DateTime.MaxValue.</exception>
		public DefaultDateTimeValueAttribute(int year, int month, int day, int hour, int minute, int second)
			: base(new DateTime(year, month, day, hour, minute, second))
		{
		}

		/// <summary>
		/// Initializes a new instance of a DefaultDateTimeAttribute with the specified
		/// year, month, day, hour, minute, and second for the specified calendar.
		/// </summary>
		/// <param name="year">The year (1 through the number of years in calendar).</param>
		/// <param name="month">The month (1 through the number of months in calendar).</param>
		/// <param name="day">The day (1 through the number of days in month).</param>
		/// <param name="hour">The hours (0 through 23).</param>
		/// <param name="minute">The minutes (0 through 59).</param>
		/// <param name="second">The seconds (0 through 59).</param>
		/// <param name="calendar">The calendar that is used to interpret year, month, and day.</param>
		/// <exception cref="ArgumentNullException">calendar is null.</exception>
		/// <exception cref="ArgumentOutOfRangeException">year is not in the range supported by calendar.-or- month is less than 1
		/// or greater than the number of months in calendar.-or- day is less than 1
		/// or greater than the number of days in month.-or- hour is less than 0 or greater
		/// than 23 -or- minute is less than 0 or greater than 59. -or- second is less
		/// than 0 or greater than 59.</exception>
		/// <exception cref="ArgumentException">The specified parameters evaluate to less than System.DateTime.MinValue or
		/// more than System.DateTime.MaxValue.</exception>
		public DefaultDateTimeValueAttribute(int year, int month, int day, int hour, int minute, int second, Calendar calendar)
			: base(new DateTime(year, month, day, hour, minute, second, calendar))
		{
		}

		/// <summary>
		/// Initializes a new instance of a DefaultDateTimeAttribute with the specified
		/// year, month, day, hour, minute, second, and Coordinated Universal Time (UTC)
		/// or local time.
		/// </summary>
		/// <param name="year">The year (1 through 9999).</param>
		/// <param name="month">The month (1 through 12).</param>
		/// <param name="day">The day (1 through the number of days in month).</param>
		/// <param name="hour">The hours (0 through 23).</param>
		/// <param name="minute">The minutes (0 through 59).</param>
		/// <param name="second">The seconds (0 through 59).</param>
		/// <param name="kind">One of the enumeration values that indicates whether year, month, day, hour,
		/// minute and second specify a local time, Coordinated Universal Time (UTC),
		/// or neither.</param>
		/// <exception cref="ArgumentOutOfRangeException">year is less than 1 or greater than 9999. -or- month is less than 1 or greater
		/// than 12. -or- day is less than 1 or greater than the number of days in month.-or-
		/// hour is less than 0 or greater than 23. -or- minute is less than 0 or greater
		/// than 59. -or- second is less than 0 or greater than 59.</exception>
		/// <exception cref="ArgumentException">The specified time parameters evaluate to less than System.DateTime.MinValue
		/// or more than System.DateTime.MaxValue. -or-kind is not one of the System.DateTimeKind
		/// values.</exception>
		public DefaultDateTimeValueAttribute(int year, int month, int day, int hour, int minute, int second, DateTimeKind kind)
			: base(new DateTime(year, month, day, hour, minute, second, kind))
		{
		}

		/// <summary>
		/// Initializes a new instance of a DefaultDateTimeAttribute with the specified
		/// year, month, day, hour, minute, second, and millisecond.
		/// </summary>
		/// <param name="year">The year (1 through 9999).</param>
		/// <param name="month">The month (1 through 12).</param>
		/// <param name="day">The day (1 through the number of days in month).</param>
		/// <param name="hour">The hours (0 through 23).</param>
		/// <param name="minute">The minutes (0 through 59).</param>
		/// <param name="second">The seconds (0 through 59).</param>
		/// <param name="millisecond">The milliseconds (0 through 999).</param>
		/// <exception cref="ArgumentOutOfRangeException">year is less than 1 or greater than 9999.-or- month is less than 1 or greater
		/// than 12.-or- day is less than 1 or greater than the number of days in month.-or-
		/// hour is less than 0 or greater than 23.-or- minute is less than 0 or greater
		/// than 59.-or- second is less than 0 or greater than 59.-or- millisecond is
		/// less than 0 or greater than 999.</exception>
		/// <exception cref="ArgumentException">The specified parameters evaluate to less than System.DateTime.MinValue or
		/// more than System.DateTime.MaxValue.</exception>
		public DefaultDateTimeValueAttribute(int year, int month, int day, int hour, int minute, int second, int millisecond)
			: base(new DateTime(year, month, day, hour, minute, second, millisecond))
		{
		}

		/// <summary>
		/// Initializes a new instance of a DefaultDateTimeAttribute with the specified
		/// year, month, day, hour, minute, second, and millisecond for the specified
		/// calendar.
		/// </summary>
		/// <param name="year">The year (1 through the number of years in calendar).</param>
		/// <param name="month">The month (1 through the number of months in calendar).</param>
		/// <param name="day">The day (1 through the number of days in month).</param>
		/// <param name="hour">The hours (0 through 23).</param>
		/// <param name="minute">The minutes (0 through 59).</param>
		/// <param name="second">The seconds (0 through 59).</param>
		/// <param name="millisecond">The milliseconds (0 through 999).</param>
		/// <param name="calendar">The calendar that is used to interpret year, month, and day.</param>
		/// <exception cref="ArgumentNullException">calendar is null.</exception>
		/// <exception cref="ArgumentOutOfRangeException">year is not in the range supported by calendar.-or- month is less than 1
		/// or greater than the number of months in calendar.-or- day is less than 1
		/// or greater than the number of days in month.-or- hour is less than 0 or greater
		/// than 23.-or- minute is less than 0 or greater than 59.-or- second is less
		/// than 0 or greater than 59.-or- millisecond is less than 0 or greater than
		/// 999.</exception>
		/// <exception cref="ArgumentException">The specified parameters evaluate to less than System.DateTime.MinValue or
		/// more than System.DateTime.MaxValue.</exception>
		public DefaultDateTimeValueAttribute(int year, int month, int day, int hour, int minute, int second, int millisecond, Calendar calendar)
			: base(new DateTime(year, month, day, hour, minute, second, millisecond, calendar))
		{
		}

		/// <summary>
		/// Initializes a new instance of a DefaultDateTimeAttribute with the specified
		/// year, month, day, hour, minute, second, millisecond, and Coordinated Universal
		/// Time (UTC) or local time.
		/// </summary>
		/// <param name="year">The year (1 through 9999).</param>
		/// <param name="month">The month (1 through 12).</param>
		/// <param name="day">The day (1 through the number of days in month).</param>
		/// <param name="hour">The hours (0 through 23).</param>
		/// <param name="minute">The minutes (0 through 59).</param>
		/// <param name="second">The seconds (0 through 59).</param>
		/// <param name="millisecond">The milliseconds (0 through 999).</param>
		/// <param name="kind">One of the enumeration values that indicates whether year, month, day, hour,
		/// minute, second, and millisecond specify a local time, Coordinated Universal
		/// Time (UTC), or neither.</param>
		/// <exception cref="ArgumentOutOfRangeException">year is less than 1 or greater than 9999.-or- month is less than 1 or greater
		/// than 12.-or- day is less than 1 or greater than the number of days in month.-or-
		/// hour is less than 0 or greater than 23.-or- minute is less than 0 or greater
		/// than 59.-or- second is less than 0 or greater than 59.-or- millisecond is
		/// less than 0 or greater than 999.</exception>
		/// <exception cref="ArgumentException">The specified time parameters evaluate to less than System.DateTime.MinValue
		/// or more than System.DateTime.MaxValue. -or-kind is not one of the System.DateTimeKind
		/// values.</exception>
		public DefaultDateTimeValueAttribute(int year, int month, int day, int hour, int minute, int second, int millisecond, DateTimeKind kind)
			: base(new DateTime(year, month, day, hour, minute, second, millisecond, kind))
		{
		}

		/// <summary>
		/// Initializes a new instance of a DefaultDateTimeAttribute with the specified
		/// year, month, day, hour, minute, second, millisecond, and Coordinated Universal
		/// Time (UTC) or local time for the specified calendar.
		/// </summary>
		/// <param name="year">The year (1 through the number of years in calendar).</param>
		/// <param name="month">The month (1 through the number of months in calendar).</param>
		/// <param name="day">The day (1 through the number of days in month).</param>
		/// <param name="hour">The hours (0 through 23).</param>
		/// <param name="minute">The minutes (0 through 59).</param>
		/// <param name="second">The seconds (0 through 59).</param>
		/// <param name="millisecond">The milliseconds (0 through 999).</param>
		/// <param name="calendar">The calendar that is used to interpret year, month, and day.</param>
		/// <param name="kind">One of the enumeration values that indicates whether year, month, day, hour,
		/// minute, second, and millisecond specify a local time, Coordinated Universal
		/// Time (UTC), or neither.</param>
		/// <exception cref="ArgumentNullException">calendar is null.</exception>
		/// <exception cref="ArgumentOutOfRangeException">year is not in the range supported by calendar.-or- month is less than 1
		/// or greater than the number of months in calendar.-or- day is less than 1
		/// or greater than the number of days in month.-or- hour is less than 0 or greater
		/// than 23.-or- minute is less than 0 or greater than 59.-or- second is less
		/// than 0 or greater than 59.-or- millisecond is less than 0 or greater than
		/// 999.</exception>
		/// <exception cref="ArgumentException">The specified time parameters evaluate to less than System.DateTime.MinValue
		/// or more than System.DateTime.MaxValue. -or-kind is not one of the System.DateTimeKind
		/// values.</exception>
		public DefaultDateTimeValueAttribute(int year, int month, int day, int hour, int minute, int second, int millisecond, Calendar calendar, DateTimeKind kind)
			: base(new DateTime(year, month, day, hour, minute, second, millisecond, calendar, kind))
		{
		}
	}
}

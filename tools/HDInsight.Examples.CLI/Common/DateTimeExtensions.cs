using System;

namespace HDInsight.Examples.CLI
{
    /// <summary>
    /// Extensions for DateTime class - we mainly use the floor for event aggregation window
    /// </summary>
    public static class DateTimeExtensions
    {
        public static DateTime Round(this DateTime date)
        {
            return date.Round(TimeSpan.FromMinutes(1));
        }

        public static DateTime Round(this DateTime date, TimeSpan timeSpan)
        {
            long ticks = (date.Ticks + (timeSpan.Ticks / 2) + 1) / timeSpan.Ticks;
            return new DateTime(ticks * timeSpan.Ticks);
        }

        public static DateTime Floor(this DateTime date)
        {
            return date.Floor(TimeSpan.FromMinutes(1));
        }

        public static DateTime Floor(this DateTime date, TimeSpan timeSpan)
        {
            long ticks = (date.Ticks / timeSpan.Ticks);
            return new DateTime(ticks * timeSpan.Ticks);
        }

        public static DateTime Ceil(this DateTime date)
        {
            return date.Ceil(TimeSpan.FromMinutes(1));
        }

        public static DateTime Ceil(this DateTime date, TimeSpan timeSpan)
        {
            long ticks = (date.Ticks + timeSpan.Ticks - 1) / timeSpan.Ticks;
            return new DateTime(ticks * timeSpan.Ticks);
        }
    }
}

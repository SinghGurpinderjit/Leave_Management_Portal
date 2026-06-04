namespace Shared.Common.Extensions
{
    public static class DatetimeHelperExtensions
    {
        private static readonly TimeZoneInfo IndiaTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");

        public static string UtcToLocalDatetimeString(this DateTime utc)
        {
            var local = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(utc, DateTimeKind.Utc),
                IndiaTimeZone);
            return local.ToString("yyyy-MM-dd hh:mm:ss tt");
        }

        public static string UtcToLocalDateOnlyString(this DateTime utc)
        {
            return utc.ToString("yyyy-MM-dd");
        }
    }
}

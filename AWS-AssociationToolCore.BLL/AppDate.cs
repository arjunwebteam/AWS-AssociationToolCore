using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AWS_AssociationToolCore.BLL
{

    public static class AppDate
    {
        public const string Fallback = "MM/DD/YYYY";        // your current default
        public static string TimeFormat = "hh:mm tt";        // your existing time format

        public static readonly string[] Keys = {
            "MM/DD/YYYY","MM-DD-YYYY","MM/DD/YY","MM-DD-YY",
            "DD/MM/YYYY","DD-MM-YYYY","DD/MM/YY","DD-MM-YY",
            "DD-MMM-YYYY","DD-MMM-YY","MMM-DD-YYYY","MMM-DD-YY",
            "DD MMM YYYY","MMM DD, YYYY","YYYY-MM-DD","YYYY/MM/DD" };

        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        static readonly object _lock = new object();
        static string _key;
        static DateTime _loadedUtc = DateTime.MinValue;

        public static string Key
        {
            get
            {
                if (_key == null || (DateTime.UtcNow - _loadedUtc).TotalMinutes >= 2) Refresh();
                return _key ?? Fallback;
            }
        }

        public static string FlatFormat => Key.Replace("YYYY", "Y").Replace("YY", "y")
                                      .Replace("DD", "d").Replace("MMM", "M").Replace("MM", "m");




        public static void Set(string key)
        {
            _key = Array.IndexOf(Keys, key) >= 0 ? key : Fallback;
            _loadedUtc = DateTime.UtcNow;
        }

        public static void Refresh()
        {
            lock (_lock)
            {
                try
                {
                    int s = 0;
                    var a = new AppInfo().GetAppInfoDetails(ref s);
                    Set(a?.DateFormat);
                }
                catch { if (_key == null) _key = Fallback; _loadedUtc = DateTime.UtcNow; }
            }
        }




        // jQuery UI datepicker format: yy = 4-digit year, y = 2-digit, dd, mm, M = short month
        public static string JqFormat => Key.Replace("YYYY", "yy").Replace("YY", "y")
                                            .Replace("DD", "dd").Replace("MMM", "M").Replace("MM", "mm");





        static string ToNet(string k) => k.Replace("YYYY", "yyyy").Replace("YY", "yy").Replace("DD", "dd");
        public static string NetFormat => ToNet(Key);

        // bootstrap-datepicker format: dd-M-yyyy etc.
        public static string JsFormat => ToNet(Key).Replace("MMM", "M").Replace("MM", "mm");

        public static string Format(DateTime d, string key = null)
        {
            var f = ToNet(key ?? Key);
            var s = d.ToString(f, Inv);
            return f.Contains("MMM") ? s.ToUpperInvariant() : s;
        }

        public static string Example(string key) => Format(new DateTime(2026, 10, 6), key);

        public static bool TryParse(string input, out DateTime result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(input)) return false;
            input = input.Trim();
            return DateTime.TryParseExact(input, NetFormat, Inv, DateTimeStyles.None, out result)
                || DateTime.TryParseExact(input, "yyyy-MM-dd", Inv, DateTimeStyles.None, out result);
        }
    }

    public static class AppDateExtensions
    {
        public static string ToAppDate(this DateTime d) =>
            d == DateTime.MinValue ? "" : AppDate.Format(d);
        public static string ToAppDate(this DateTime? d) =>
            d.HasValue ? d.Value.ToAppDate() : "";
        public static string ToAppDateTime(this DateTime d) =>
            d == DateTime.MinValue ? "" : AppDate.Format(d) + " " + d.ToString(AppDate.TimeFormat, CultureInfo.InvariantCulture);
        public static string ToAppDateTime(this DateTime? d) =>
            d.HasValue ? d.Value.ToAppDateTime() : "";


        public static string ToAppDateWithDay(this DateTime d) =>
    d == DateTime.MinValue ? "" : d.ToString("ddd", CultureInfo.InvariantCulture) + ", " + AppDate.Format(d);





    }
}


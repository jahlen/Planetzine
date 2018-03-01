using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text.RegularExpressions;

namespace Planetzine.Models
{
    public static class ExtensionMethods
    {
        public static string Capitalize(this string str)
        {
            if (str == null)
                return null;

            if (str.Length == 1)
                return str.ToUpper();

            return str[0].ToString().ToUpper() + str.Substring(1);
        }

        public static string RemoveHtmlTags(this string str)
        {
            var regex = new Regex("<.+?>");
            return regex.Replace(str, "");
        }

        public static string GetBeginning(this string str, int maximumCharacters)
        {
            if (str.Length > maximumCharacters)
                return str.Substring(0, maximumCharacters);
            else
                return str;
        }
    }
}
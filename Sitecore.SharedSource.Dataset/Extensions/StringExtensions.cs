using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sitecore.SharedSource.Dataset.Extensions
{
    public static class @string
    {
        /// <summary>
        /// Check if context string IsNullOrEmpty. 
        /// Return "then" parameter if true, 
        /// otherwise return context string
        /// </summary>
        /// <param name="s"></param>
        /// <param name="then"></param>
        /// <returns></returns>
        public static string IfNullOrEmpty(this string s, string then)
        {
            if (string.IsNullOrEmpty(s))
                return then;
            else
                return s;
        }
    }
}

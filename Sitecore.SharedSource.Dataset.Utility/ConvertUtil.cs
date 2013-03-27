using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sitecore.SharedSource.Dataset
{
    public static class ConvertUtil
    {
        public static Double ToDouble(string value, Double defaultvalue)
        {
            Double dbl;
            if (!Double.TryParse(value, out dbl))
                dbl = defaultvalue;
            return dbl;
        }

        public static Int32 ToInt32(string value, Int32 defaultvalue)
        {
            Int32 i;
            if (!Int32.TryParse(value, out i))
                i = defaultvalue;
            return i;
        }

        public static bool ToBool(string value, bool defaultvalue)
        {
            bool b;
            if (!bool.TryParse(value, out b))
                b = defaultvalue;
            return b;
        }
    }
}

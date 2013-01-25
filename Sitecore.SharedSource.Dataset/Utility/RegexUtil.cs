using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Sitecore.SharedSource.Dataset
{
    public class RegexUtil
    {
        private const string _REGEX_FIELD = @"\[(?<fieldidentifier>[^\[\]\?]*)(?:\?(?<parameters>[^\]]*))?\]";

        // Sample match: <a>the text i want as a link to the passed item</a>
        public static Regex rxHtmlLink = new Regex(@"<a>(?<linkcontent>.*?)</a>", RegexOptions.Compiled | RegexOptions.Singleline);

        // Sample match: [my field?param=1]
        public static Regex rxFieldExpression = new Regex(_REGEX_FIELD, RegexOptions.Compiled);

        // Sample match: {"some prefix", [my field?param=1], "some suffix"} 
        // Note: prefix & suffix are only rendered if content is not empty
        public static Regex rxFieldExpressionWithPreSufFix = new Regex(@"\{\s*""(?<before>[^\[\}]*)""\s*,\s*" + _REGEX_FIELD + @"\s*,\s*""(?<after>[^\}]*)""\s*\}", RegexOptions.Compiled);


        public static Regex rxFieldExpressionWithPreSufFixHtmlEncoded = new Regex(@"\{\s*&quot;(?<before>[^\[\}]*)&quot;\s*,\s*" + _REGEX_FIELD + @"\s*,\s*&quot;(?<after>[^\}]*)&quot;\s*\}", RegexOptions.Compiled);
    }
}

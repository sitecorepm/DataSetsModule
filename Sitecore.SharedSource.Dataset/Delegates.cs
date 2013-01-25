using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sitecore.SharedSource.Dataset
{
    public delegate string GetDataItemFieldValue<T>(T dataitem, string fieldname, string before, string after, string parameters);
    public delegate string GetDataItemUrl<T>(T dataitem);
    //public delegate bool CustomFunctionHandler(string vcFunctionName, object[] args, ref string result);
}

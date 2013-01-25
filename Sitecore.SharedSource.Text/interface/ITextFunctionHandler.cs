using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sitecore.SharedSource.Text
{
	public interface ITextFunctionHandler
	{
        bool ProcessFunction(string functionName, object[] args, ref string result);
	}
}

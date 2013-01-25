using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sitecore.SharedSource.Dataset.Domain
{
    public class FieldDeclaration
    {
        public string FieldName { get; set; }
        public string Before { get; set; }
        public string After { get; set; }
        public string Parameters { get; set; }
    }
}

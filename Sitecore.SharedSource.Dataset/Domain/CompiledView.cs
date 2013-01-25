using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sitecore.SharedSource.Dataset.Domain
{
    public class CompiledView
    {
        private string _viewText = null;
        private Dictionary<string, FieldDeclaration> _fieldDeclarations = null;

        public CompiledView(string viewText, Dictionary<string, FieldDeclaration> fieldDeclarations)
        {
            _viewText = viewText;
            _fieldDeclarations = fieldDeclarations;
        }

        public string ViewText
        {
            get
            {
                return _viewText;
            }
        }

        public Dictionary<string, FieldDeclaration> FieldDeclarations
        {
            get
            {
                return _fieldDeclarations;
            }
        }

        public Dictionary<string, string> ResolveVariables<T>(T dataitem, GetDataItemFieldValue<T> gfv)
        { 
            var variables = new Dictionary<string, string>();
            foreach(var kv in _fieldDeclarations)
            {
                variables.Add(kv.Key, gfv(dataitem, kv.Value.FieldName, kv.Value.Before, kv.Value.After, kv.Value.Parameters));
            }
            return variables;
        }
    }
}

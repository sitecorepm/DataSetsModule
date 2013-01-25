using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Sitecore.SharedSource.Dataset.Caching
{
    public class CacheFailureException : Exception
    {
        public CacheFailureException() : base() { }
        public CacheFailureException(string message) : base(message) { }
        public CacheFailureException(string message, Exception inner) : base(message, inner) { }
        public CacheFailureException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}

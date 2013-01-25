using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sitecore.SharedSource.Dataset.Caching
{
    [Serializable]
    public class CacheFailure
    {
        public DateTime Timestamp { get; set; }
        public Exception InnerException { get; set; }
        public TimeSpan Duration { get; set; }
        public object LastGoodData { get; set; }
        public string Key { get; set; }

        public TimeSpan TimeRemaining
        {
            get
            {
                var end = Timestamp.Add(Duration);
                return end.Subtract(DateTime.Now);
            }
        }

        public CacheFailureException GetException()
        {
            return new CacheFailureException("CACHED EXCEPTION -----> [" + TimeRemaining.TotalMinutes.ToString("N1") + "m remaining before retry][ItemPath: " + Key + "]: " + InnerException.Message);
        }
    }
}

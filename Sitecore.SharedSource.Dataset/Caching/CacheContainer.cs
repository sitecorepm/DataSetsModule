using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sitecore.SharedSource.Dataset.Caching
{
    public class CacheContainer
    {
        public CacheContainer(object data, TimeSpan cacheduration)
        {
            this.Timestamp = DateTime.UtcNow;
            this.Duration = cacheduration;
            this.Data = data;
        }

        public DateTime Timestamp { get; set; }
        public TimeSpan Duration { get; set; }
        public object Data { get; set; }

        public bool IsExpired
        {
            get
            {
                return Timestamp.Add(Duration) < DateTime.UtcNow;
            }
        }
    }
}

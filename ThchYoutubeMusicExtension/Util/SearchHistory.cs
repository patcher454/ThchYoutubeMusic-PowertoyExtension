using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThchYoutubeMusicExtension.Util
{
    public class SearchHistory
    {
        [JsonConstructor]
        public SearchHistory() 
        {
            ThumbnailUrl = string.Empty;
            Title = string.Empty;
            VideoId = string.Empty;
            AccessibilityData = string.Empty;
            Timestamp = DateTime.Now;
        }


        [JsonProperty(nameof(ThumbnailUrl))]
        public string ThumbnailUrl { get; set; }

        [JsonProperty(nameof(Title))]
        public string Title { get; set; }

        [JsonProperty(nameof(VideoId))]
        public string VideoId { get; set; }

        [JsonProperty(nameof(AccessibilityData))]
        public string AccessibilityData { get; set; }

        [JsonProperty(nameof(Timestamp))]
        public DateTime Timestamp { get; set; }
    }
}

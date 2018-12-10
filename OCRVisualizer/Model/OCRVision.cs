using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.OCRVision
{
    public class OCRVision
    {
        [JsonProperty(PropertyName = "language")]
        public string Language { get; set; }

        [JsonProperty(PropertyName = "textAngle")]
        public float TextAngle { get; set; }

        [JsonProperty(PropertyName = "orientation")]
        public string Orientation { get; set; }

        [JsonProperty(PropertyName = "regions")]
        public Region[] Regions { get; set; }

        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
    }

    public class Region
    {
        [JsonProperty(PropertyName = "boundingBox")]
        public string BoundingBox { get; set; }

        [JsonProperty(PropertyName = "lines")]
        public WLine[] Lines { get; set; }
    }

    public class WLine
    {
        [JsonProperty(PropertyName = "boundingBox")]
        public string BoundingBox { get; set; }

        [JsonProperty(PropertyName = "words")]
        public Word[] Words { get; set; }
    }

    public class Word
    {
        [JsonProperty(PropertyName = "boundingBox")]
        public string BoundingBox { get; set; }

        [JsonProperty(PropertyName = "text")]
        public string Text { get; set; }
    }

}

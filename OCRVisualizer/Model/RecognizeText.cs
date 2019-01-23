using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCRVisualizer.Model
{
    class RecognizeText
    {
        [JsonProperty(PropertyName = "status")]
        public string Status { get; set; }
        [JsonProperty(PropertyName = "recognitionResult")]
        public RegionNew RecognitionResult { get; set; }
    }

    public class RegionNew
    {
        [JsonProperty(PropertyName = "lines")]
        public LineNew[] Lines { get; set; }
    }

    public class LineNew
    {
        [JsonProperty(PropertyName = "boundingBox")]
        public int[] BoundingBox { get; set; }

        [JsonProperty(PropertyName = "text")]
        public string Text { get; set; }

        [JsonProperty(PropertyName = "words")]
        public WordNew[] Words { get; set; }
    }

    public class WordNew
    {
        [JsonProperty(PropertyName = "boundingBox")]
        public int[] BoundingBox { get; set; }

        [JsonProperty(PropertyName = "text")]
        public string Text { get; set; }

        [JsonProperty(PropertyName = "Confidence")]
        public string Confidence { get; set; }
    }

}

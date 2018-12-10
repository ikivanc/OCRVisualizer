using Model.OCRVision;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OCRVisualizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string OCRResponse = String.Empty;

        // Brush Color assigment for Regions, Lines and Words
        private static Brush _regionColor = Brushes.Green;
        private static Brush _lineColor = Brushes.Red;
        private static Brush _wordColor = Brushes.Aqua;
        private static Brush _highlightColor = Brushes.Black;

        // Microsoft Cognitive Services Computer Vision Endpoint details.
        const string subscriptionKey = "YOUR_COMPUTER_VISION_API_KEY";
        const string uriBase = "https://northeurope.api.cognitive.microsoft.com/vision/v2.0/ocr";

        public MainWindow()
        {
            InitializeComponent();
        }

        // Extract Text & Text Regions 
        public void ExtractTextAndRegionsFromResponse()
        {
            var response = JObject.Parse(OCRResponse);

            //Full extracted text if it is needed
            //var fulltext = ExtractTextFromResponse(response);

            OCRVision ocrVision = JsonConvert.DeserializeObject<OCRVision>(OCRResponse);

            foreach (Region ereg in ocrVision.Regions)
            {
                // Draw rectangles for the regions
                CreateRectangle(ereg.BoundingBox, _regionColor);

                foreach (WLine sline in ereg.Lines)
                {
                    string line = string.Join(" ", from Word sword in sline.Words
                                                   select (string)sword.Text);

                    foreach (Word sword in sline.Words)
                    {
                        // Draw rectangles for the lines
                        CreateRectangle(sword.BoundingBox, _wordColor);
                        CreateImageLabels(sword.BoundingBox, sword.Text);
                    }
                }
            }

            canvas.Visibility = Visibility.Visible;
            txtOcrOutput.Text = ExtractTextByRegions(ocrVision);

        }

        private void CreateRectangle(string boundingBox, Brush color)
        {
            CreateRectangle(boundingBox, color, false);
        }

        // Create Rectangle method on Images
        private void CreateRectangle(string boundingBox, Brush color, bool highlight)
        {
            // Detect the edges & size values of the box
            int[] values = Array.ConvertAll(boundingBox.Split(','), int.Parse);
            int width = values[2];
            int height = values[3];
            int left = values[0];
            int top = values[1];

            // Create the rectangle
            Rectangle rec = new Rectangle()
            {
                Width = width,
                Height = height,
                Stroke = color,
                StrokeThickness = 2,
            };
            if (highlight)
            {
                rec.Fill = _highlightColor;
            }

            // Add  rectangle object to a canvas
            canvas.Children.Add(rec);
            Canvas.SetTop(rec, top);
            Canvas.SetLeft(rec, left);
        }

        // Create Labels on Rectangles & Images
        private void CreateImageLabels(string boundingBox, string text)
        {
            // Detect the edges & size values of the box
            int[] values = Array.ConvertAll(boundingBox.Split(','), int.Parse);
            int width = values[2];
            int height = values[3];
            int left = values[0];
            int top = values[1];

            // Create the rectangle
            Rectangle rec = new Rectangle()
            {
                Width = width,
                Height = height,
                Fill = Brushes.Yellow,
                Stroke = Brushes.Red,
                StrokeThickness = 1,
                Opacity = 0.8,
            };

            //Create Label
            Label lbl = new Label()
            {
                Content = text,
                FontSize = height + 2
            };


            // Add  rectangle object to a canvas
            canvas.Children.Add(rec);
            Canvas.SetTop(rec, top);
            Canvas.SetLeft(rec, left);

            // Add Labels on top of rectangles
            canvas.Children.Add(lbl);
            Canvas.SetTop(lbl, top-10);
            Canvas.SetLeft(lbl, left-5);
        }

        // Extract Text Line by Lines
        private string ExtractTextByRegions(OCRVision ocrVision)
        {
            string resultText = String.Empty;

            foreach (Region ereg in ocrVision.Regions)
            {
                resultText += string.Join(" ", from WLine sline in ereg.Lines
                                               from Word sword in sline.Words
                                               select (string)sword.Text) + "\n";
            }

            return resultText;
        }

        // Extract full text from the response
        public static string ExtractTextFromResponse(JObject responseJson)
        {
            return string.Join(" ", from r in responseJson["regions"]
                                    from l in r["lines"]
                                    from w in l["words"]
                                    select (string)w["text"]);
        }

        // Microsoft Cognitive Services Computer Vision OCR Method
        static async Task<string> MakeOCRRequest(string imageFilePath)
        {
            try
            {
                HttpClient client = new HttpClient();

                // Request headers.
                client.DefaultRequestHeaders.Add(
                    "Ocp-Apim-Subscription-Key", subscriptionKey);

                // Request parameters. 
                // The language parameter doesn't specify a language, so the 
                // method detects it automatically.
                // The detectOrientation parameter is set to true, so the method detects and
                // and corrects text orientation before detecting text.
                string requestParameters = "language=unk&detectOrientation=true";

                // Assemble the URI for the REST API method.
                string uri = uriBase + "?" + requestParameters;

                HttpResponseMessage response;

                // Read the contents of the specified local image
                // into a byte array.
                byte[] byteData = GetImageAsByteArray(imageFilePath);

                // Add the byte array as an octet stream to the request body.
                using (ByteArrayContent content = new ByteArrayContent(byteData))
                {
                    // This example uses the "application/octet-stream" content type.
                    // The other content types you can use are "application/json"
                    // and "multipart/form-data".
                    content.Headers.ContentType =
                        new MediaTypeHeaderValue("application/octet-stream");

                    // Asynchronously call the REST API method.
                    response = await client.PostAsync(uri, content);
                }

                // Asynchronously get the JSON response.
                string contentString = await response.Content.ReadAsStringAsync();

                return contentString;

            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        static byte[] GetImageAsByteArray(string imageFilePath)
        {
            // Open a read-only file stream for the specified file.
            using (FileStream fileStream =
                new FileStream(imageFilePath, FileMode.Open, FileAccess.Read))
            {
                // Read the file's contents into a byte array.
                BinaryReader binaryReader = new BinaryReader(fileStream);
                return binaryReader.ReadBytes((int)fileStream.Length);
            }
        }

        private void ButtonVisiblity_Click(object sender, RoutedEventArgs e)
        {
            if (canvas.Visibility == Visibility.Visible)
            {
                canvas.Visibility = Visibility.Collapsed;
            }
            else
            {
                canvas.Visibility = Visibility.Visible;
            }
        }

        private async void ButtonBrowse_Click(object sender, RoutedEventArgs e)
        {
            var openDlg = new Microsoft.Win32.OpenFileDialog();
            openDlg.Filter = "JPEG and PNG Images|*.jpg;*.jpeg;*.png";
            bool? result = openDlg.ShowDialog(this);
            if (!(bool)result)
            {
                return;
            }

            string filePath = openDlg.FileName;
            Uri fileUri = new Uri(filePath);
            BitmapImage bitmapSource = new BitmapImage();
            bitmapSource.BeginInit();
            bitmapSource.CacheOption = BitmapCacheOption.None;
            bitmapSource.UriSource = fileUri;
            bitmapSource.EndInit();

            canvasImg.Height = bitmapSource.Height;
            canvasImg.Width = bitmapSource.Width;
            canvas.Height = bitmapSource.Height;
            canvas.Width = bitmapSource.Width;

            imgInvoice.Source = bitmapSource;

            canvas.Children.Clear();
            OCRResponse = await MakeOCRRequest(filePath);
            ExtractTextAndRegionsFromResponse();

        }

    }
}

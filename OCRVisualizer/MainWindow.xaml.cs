using Model.OCRVision;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OCRVisualizer.Model;
using System;
using System.Collections.Generic;
using System.Configuration;
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
using System.Xml;

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
        private string subscriptionKey = ConfigurationManager.AppSettings["subscriptionKey"].ToString();
        private string uriEndpoint = ConfigurationManager.AppSettings["endpointRegion"].ToString();
        private string docLanguage = ConfigurationManager.AppSettings["documentLanguage"].ToString();
        private string searchKeys = ConfigurationManager.AppSettings["searchValues"].ToString();
        private double DPIrenderSize;
        private List<TextValue> textvalues = new List<TextValue>();

        public MainWindow()
        {
            InitializeComponent();
            InitUI();
        }

        private void InitUI()
        {
            // Set the index of selected language
            if (comboLanguage.SelectedIndex == -1)
            {
                var comboBoxItem = comboLanguage.Items.OfType<ComboBoxItem>().FirstOrDefault(x => x.Content.ToString().Contains(docLanguage));
                comboLanguage.SelectedIndex = comboLanguage.Items.IndexOf(comboBoxItem);
            }

            // Set values of Key-Value field data
            txtKeys.Text = searchKeys;
        }

        // Extract Text & Text Regions 
        private void ExtractTextAndRegionsFromResponse()
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
                    // Draw rectangles for the lines
                    CreateRectangle(sline.BoundingBox, _lineColor);

                    //string line = string.Join(" ", from Word sword in sline.Words
                    //                               select (string)sword.Text);

                    foreach (Word sword in sline.Words)
                    {
                        // Draw rectangles for the words
                        CreateRectangle(sword.BoundingBox, _wordColor);
                        CreateImageLabels(sword.BoundingBox, sword.Text);

                    }
                }
            }

            canvas.Visibility = Visibility.Visible;
            stckOutput.Visibility = Visibility.Visible;
            txtOcrOutput.Text = ExtractTextByRegions(ocrVision);

            //Search key-value pairs if Keys are defined in Settings.
            if (!string.IsNullOrEmpty(searchKeys))
            { 
                ExtractKeyValuePairs(ocrVision.Regions);
            }
        }

        // Extract Key-Value Pairs
        private void ExtractKeyValuePairs(Region[] regions)
        {
            // Extract regions of text words
            foreach (Region ereg in regions)
            {
                foreach (WLine sline in ereg.Lines)
                {
                    foreach (Word sword in sline.Words)
                    {
                        int[] wvalues = Array.ConvertAll(sword.BoundingBox.Split(','), int.Parse);
                        int wwidth = (int)(wvalues[2]);
                        int wheight = (int)(wvalues[3] );
                        int wleft = (int)(wvalues[0]);
                        int wtop = (int)(wvalues[1]);
                        textvalues.Add(new TextValue { BoundingBox = sword.BoundingBox, Text=sword.Text, Left = wleft, Top = wtop, Height = wheight, Width = wwidth });
                    }
                }
            }

            // Search Key-Value Pairs inside the documents
            if (!string.IsNullOrEmpty(searchKeys))
            {
                var ocrsearchkeys = searchKeys.Split(',');
                foreach (string key in ocrsearchkeys)
                {
                    List<TextValue> resultkeys = textvalues.Where(a => a.Text.Contains(key)).ToList<TextValue>();

                    foreach (TextValue tv in resultkeys)
                    {
                        // For width 300px right is assigned
                        // For height It's looking for 10px above
                        string txtreply = string.Join(" ", 
                                                    from a in textvalues
                                                    where (a.Left > tv.Left) && (a.Left < tv.Left + tv.Width + 300) && (a.Top > tv.Top - 10) && (a.Top < tv.Top + tv.Height)
                                                    select (string)a.Text);
                        //MessageBox.Show(tv.Text + " - " + txtreply);
                        listBoxKeyValue.Items.Add(tv.Text + " - " + txtreply);
                    }                    
                }

                // Hide key-value extraction if there's no match
                if (listBoxKeyValue.Items.Count > 0)
                {
                    stckKeyValResult.Visibility = Visibility.Visible;
                    gridKeyVal.Visibility = Visibility.Visible;
                }
            }
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
            int width = (int)(values[2]/ DPIrenderSize);
            int height = (int)(values[3] / DPIrenderSize);
            int left = (int)(values[0] / DPIrenderSize);
            int top = (int)(values[1] / DPIrenderSize);

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
            int width = (int)(values[2] / DPIrenderSize);
            int height = (int)(values[3] / DPIrenderSize);
            int left = (int)(values[0] / DPIrenderSize);
            int top = (int)(values[1] / DPIrenderSize);

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
        private static string ExtractTextFromResponse(JObject responseJson)
        {
            return string.Join(" ", from r in responseJson["regions"]
                                    from l in r["lines"]
                                    from w in l["words"]
                                    select (string)w["text"]);
        }

        // Microsoft Cognitive Services Computer Vision OCR Method
        private async Task<string> MakeOCRRequest(string imageFilePath)
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

                string requestParameters = String.Format("language={0}&detectOrientation=true",docLanguage.Split()[0]);

                // Assemble the URI for the REST API method.
                string uri = uriEndpoint + "?" + requestParameters;

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

        // Update Setting Panel paramaters
        public void UpdateConnectionKeys(string subscriptionValue, string endpointValue, string languageValue)
        {
            XmlDocument appconfigFile = new XmlDocument();
            appconfigFile.Load(AppDomain.CurrentDomain.BaseDirectory + "App.config");
            XmlNode appSettingsNode = appconfigFile.SelectSingleNode("configuration/appSettings");

            foreach (XmlNode childNode in appSettingsNode)
            {
                if (childNode.Attributes["key"].Value == "subscriptionKey") childNode.Attributes["value"].Value = subscriptionValue;
                else if (childNode.Attributes["key"].Value == "endpointRegion") childNode.Attributes["value"].Value = endpointValue;
                else if (childNode.Attributes["key"].Value == "documentLanguage") childNode.Attributes["value"].Value = languageValue;
            }
            appconfigFile.Save(AppDomain.CurrentDomain.BaseDirectory + "App.config");
            appconfigFile.Save(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);
            MessageBox.Show("Fields are updated!");
        }

        // Update Key-Value Field Extraction parameters
        public void UpdateKeyValue(string searchKeyValue)
        {
            XmlDocument appconfigFile = new XmlDocument();
            appconfigFile.Load(AppDomain.CurrentDomain.BaseDirectory + "App.config");
            XmlNode appSettingsNode = appconfigFile.SelectSingleNode("configuration/appSettings");

            foreach (XmlNode childNode in appSettingsNode)
            {
                if (childNode.Attributes["key"].Value == "searchValues") childNode.Attributes["value"].Value = searchKeyValue;
            }
            appconfigFile.Save(AppDomain.CurrentDomain.BaseDirectory + "App.config");
            appconfigFile.Save(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);
            MessageBox.Show("Field keys are updated!");
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

            // This is for calculating RenderSize on the screen
            DPIrenderSize = bitmapSource.PixelHeight / bitmapSource.Height;
            
            imgInvoice.Stretch = Stretch.Fill;
            imgInvoice.Source = bitmapSource;

            //Clear previous queries if exists.
            canvas.Children.Clear();
            textvalues.Clear();
            gridKeyVal.Visibility = Visibility.Collapsed;
            stckKeyValResult.Visibility = Visibility.Collapsed;
            listBoxKeyValue.Items.Clear();

            OCRResponse = await MakeOCRRequest(filePath);
            ExtractTextAndRegionsFromResponse();

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

        private void ButtonVisiblityOutPut_Click(object sender, RoutedEventArgs e)
        {
            if (stckOutput.Visibility == Visibility.Visible)
            {
                stckOutput.Visibility = Visibility.Collapsed;
            }
            else
            {
                stckOutput.Visibility = Visibility.Visible;
            }
        }

        private void ButtonSettings_Click(object sender, RoutedEventArgs e)
        {
            if (stckSettings.Visibility == Visibility.Visible)
            {
                stckSettings.Visibility = Visibility.Collapsed;
            }
            else
            {
                txtSubscriptionKey.Text = subscriptionKey;
                txtEndPoint.Text = uriEndpoint;
                txtKeys.Text = searchKeys;
                comboLanguage.SelectedItem = docLanguage;
                stckSettings.Visibility = Visibility.Visible;
            }
        }

        private void ButtonSettingsUpdate_Click(object sender, RoutedEventArgs e)
        {
            subscriptionKey = txtSubscriptionKey.Text;
            uriEndpoint = txtEndPoint.Text;
            docLanguage = (comboLanguage.SelectedValue as ComboBoxItem).Content as string;
            UpdateConnectionKeys(subscriptionKey, uriEndpoint, docLanguage);
        }

        private void ComboLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // pick the only language code of the combobox text
            docLanguage = ((comboLanguage.SelectedValue as ComboBoxItem).Content as string).Split(' ')[0];
            if (string.IsNullOrEmpty(docLanguage))
            {
                docLanguage = "unk";
            }

        }

        private void ButtonKeyValue_Click(object sender, RoutedEventArgs e)
        {
            if (gridKeyVal.Visibility == Visibility.Visible)
            {
                gridKeyVal.Visibility = Visibility.Collapsed;
            }
            else
            {
                txtKeys.Text = searchKeys;
                gridKeyVal.Visibility = Visibility.Visible;
            }
        }

        private void ButtonKeyValueUpdate_Click(object sender, RoutedEventArgs e)
        {
            searchKeys = txtKeys.Text;
            UpdateKeyValue(searchKeys);
            gridKeyVal.Visibility = Visibility.Collapsed;
        }

        private void MouseLeftButtonKeyValue_Down(object sender, MouseButtonEventArgs e)
        {
            gridKeyVal.Visibility = Visibility.Collapsed;
        }
    
    }
}

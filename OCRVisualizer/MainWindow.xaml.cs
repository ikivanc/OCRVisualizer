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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml;

namespace OCRVisualizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Brush Color assigment for Regions, Lines and Words
        private static Brush _regionColor = Brushes.Green;
        private static Brush _lineColor = Brushes.Red;
        private static Brush _wordColor = Brushes.Aqua;
        private static Brush _highlightColor = Brushes.Black;

        // Microsoft Cognitive Services Computer Vision Endpoint details & Settings saved in App.config
        private string subscriptionKey = ConfigurationManager.AppSettings["subscriptionKey"].ToString();
        private string uriEndpoint = ConfigurationManager.AppSettings["endpointRegion"].ToString();
        private string docLanguage = ConfigurationManager.AppSettings["documentLanguage"].ToString();
        private string searchKeys = ConfigurationManager.AppSettings["searchValues"].ToString();
        private int searchKeysWidth = Convert.ToInt32(ConfigurationManager.AppSettings["searchValuesWidth"].ToString());
        private bool isNewOCREnabled = ConfigurationManager.AppSettings["isNewOCREnabled"].ToString() == "true"? true:false;

        // Local variables for the project
        private List<WordNew> textvalues = new List<WordNew>();
        private string OCRResponse = String.Empty;
        private double DPIrenderSize;
        private bool regionVis = true, lineVis = true, wordVis = true, textVis = true;


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
            txtKeysWidth.Text = searchKeysWidth.ToString();
        }

        // Extract Text & Text Regions 
        private void ExtractTextAndRegionsFromResponse()
        {
            var response = JObject.Parse(OCRResponse);
            OCRText ocrOutput = JsonConvert.DeserializeObject<OCRText>(OCRResponse);

            foreach (Region ereg in ocrOutput.Regions)
            {
                // Draw rectangles for the regions
                CreateRectangle("region",ereg.BoundingBox, _regionColor);
                foreach (WLine sline in ereg.Lines)
                {
                    // Draw rectangles for the lines
                    CreateRectangle("line",sline.BoundingBox, _lineColor);
                    foreach (Word sword in sline.Words)
                    {
                        // Draw rectangles for the words
                        CreateImageLabelsAndRectangle(sword.BoundingBox, sword.Text);
                    }
                }
            }

            // Visibility check for the bounding box layers
            if (regionVis) canvasRegions.Visibility = Visibility.Visible;
            if (lineVis) canvasLines.Visibility = Visibility.Visible;
            if (wordVis) canvasWords.Visibility = Visibility.Visible;
            if (textVis) canvasText.Visibility = Visibility.Visible;

            // Visibility check for text output 
            stckOutput.Visibility = Visibility.Visible;
            txtOcrOutput.Text = ExtractTextByRegions(ocrOutput);

            //Search key-value pairs if keys are defined in Settings.
            if (!string.IsNullOrEmpty(searchKeys))
            { 
                ExtractKeyValuePairs(ocrOutput.Regions);
            }
        }

        // NEW API Extract Text & Text Regions 
        private void ExtractTextAndRegionsNewAPIFromResponse()
        {
                var response = JObject.Parse(OCRResponse);
                RecognizeText ocrOutput = JsonConvert.DeserializeObject<RecognizeText>(OCRResponse);

            if (ocrOutput != null && ocrOutput.RecognitionResult != null && ocrOutput.RecognitionResult.Lines != null)
            {
                foreach (LineNew sline in ocrOutput.RecognitionResult.Lines)
                {
                    // Draw rectangles for the lines
                    CreateRectangle("line", sline.BoundingBox, _lineColor);
                    foreach (WordNew sword in sline.Words)
                    {
                        // Draw rectangles for the words
                        CreateImageLabelsAndRectangle(sword.BoundingBox, sword.Text);
                    }
                }


                // Visibility check for the bounding box layers
                if (regionVis) canvasRegions.Visibility = Visibility.Visible;
                if (lineVis) canvasLines.Visibility = Visibility.Visible;
                if (wordVis) canvasWords.Visibility = Visibility.Visible;
                if (textVis) canvasText.Visibility = Visibility.Visible;

                // Visibility check for text output 
                stckOutput.Visibility = Visibility.Visible;

                txtOcrOutput.Text = ExtractTextByRegions(ocrOutput);

                //Search key-value pairs if keys are defined in Settings.
                if (!string.IsNullOrEmpty(searchKeys))
                {
                    ExtractKeyValuePairs(ocrOutput.RecognitionResult.Lines);
                }
            }
            else
                MessageBox.Show("Output can't be extracted");
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
                        textvalues.Add(new WordNew { Text = sword.Text, BoundingBox = wvalues });
                    }
                }
            }

            // Search Key-Value Pairs inside the documents
            if (!string.IsNullOrEmpty(searchKeys))
            {
                var ocrsearchkeys = searchKeys.Split(',');
                foreach (string key in ocrsearchkeys)
                {
                    var resultkeys = textvalues.Where(a => a.Text.Contains(key));

                    foreach (var tv in resultkeys)
                    {
                        // For width, searchKeysWidth is binded from App.config file, 'searchValuesWidth' and 300px is assigned for this case
                        // For height It's looking for 10px above
                        string txtreply = string.Join(" ",
                                                    from a in textvalues
                                                    where (a.BoundingBox[0] > tv.BoundingBox[0]) && (a.BoundingBox[0] < tv.BoundingBox[0] + tv.BoundingBox[2] + searchKeysWidth) && (a.BoundingBox[1] > tv.BoundingBox[1] - 10) && (a.BoundingBox[1] < tv.BoundingBox[1]+ tv.BoundingBox[3])
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

        private void ExtractKeyValuePairs(LineNew[] lines)
        {
            // Search Key-Value Pairs inside the documents
            if (!string.IsNullOrEmpty(searchKeys))
            {
                var ocrsearchkeys = searchKeys.Split(',');
                foreach (string key in ocrsearchkeys)
                {
                    var resultkeys = lines.Where(a => a.Text.Contains(key));

                    foreach (var tv in resultkeys)
                    {
                        // For width, searchKeysWidth is binded from App.config file, 'searchValuesWidth' and 300px is assigned for this case
                        // For height It's looking for 10px above
                        string txtreply = string.Join(" ",
                                                    from a in lines
                                                    where (a.BoundingBox[0] > tv.BoundingBox[0]) && (a.BoundingBox[0] < tv.BoundingBox[2] + searchKeysWidth) && (a.BoundingBox[1] > tv.BoundingBox[1] - 10) && (a.BoundingBox[1] < tv.BoundingBox[3])
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


        // Create Rectangle method on Images for OCR output and RecognizeText output
        private void CreateRectangle(string type, int[] boundingBox, Brush color)
        {
            // Detect the edges & size values of the box  for RecognizeText
            int[] values = boundingBox;
            int width = (int)((values[2] - values[0]) / DPIrenderSize);
            int height = (int)((values[7] - values[1]) / DPIrenderSize);
            int left = (int)(values[0] / DPIrenderSize);
            int top = (int)(values[1] / DPIrenderSize);

            CreateRectangle(type, left,top,width,height, color, false);
        }

        private void CreateRectangle(string type, string boundingBox, Brush color)
        {
            // Detect the edges & size values of the box for MS OCR
            int[] values = Array.ConvertAll(boundingBox.Split(','), int.Parse);
            int width = (int)(values[2] / DPIrenderSize);
            int height = (int)(values[3] / DPIrenderSize);
            int left = (int)(values[0] / DPIrenderSize);
            int top = (int)(values[1] / DPIrenderSize);

            CreateRectangle(type, left, top, width, height, color, false);
        }

        private void CreateRectangle(string type, int left,int top, int width, int height, Brush color, bool highlight)
        {
            try
            {
                // Create the rectangle
                Rectangle rec = new Rectangle()
                {
                    Width = width,
                    Height = height,
                    Stroke = color,
                    StrokeThickness = 1,
                };
                if (highlight)
                {
                    rec.Fill = _highlightColor;
                }

                // Add  rectangle object to a canvas
                switch (type)
                {
                    case "line":
                        canvasLines.Children.Add(rec);
                        break;
                    case "region":
                        canvasRegions.Children.Add(rec);
                        break;
                    case "word":
                        canvasRegions.Children.Add(rec);
                        break;
                    default:
                        canvasWords.Children.Add(rec);
                        break;
                }

                Canvas.SetTop(rec, top);
                Canvas.SetLeft(rec, left);
            }
            catch
            {
                MessageBox.Show("An exception handled while creating line rectangle");
            }
        }


        // Create Labels on Rectangles & Images for OCR output and RecognizeText output
        private void CreateImageLabelsAndRectangle(string boundingBox, string text)
        {
            // Detect the edges & size values of the box
            int[] values = Array.ConvertAll(boundingBox.Split(','), int.Parse);
            int width = (int)(values[2] / DPIrenderSize);
            int height = (int)(values[3] / DPIrenderSize);
            int left = (int)(values[0] / DPIrenderSize);
            int top = (int)(values[1] / DPIrenderSize);

            CreateImageLabelsAndRectangle(text, width, height, left, top);
        }

        private void CreateImageLabelsAndRectangle(int[] boundingBox, string text)
        {
            int[] values = boundingBox;
            int width = (int)((values[2] - values[0]) / DPIrenderSize);
            int height = (int)((values[7] - values[1]) / DPIrenderSize);
            int left = (int)(values[0] / DPIrenderSize);
            int top = (int)(values[1] / DPIrenderSize);

            CreateImageLabelsAndRectangle(text, width, height, left, top);
        }

        private void CreateImageLabelsAndRectangle(string text, int width, int height, int left, int top)
        {
            try
            {
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
                    FontSize = height
                };

                // Add  rectangle object to a canvas
                canvasWords.Children.Add(rec);
                Canvas.SetTop(rec, top);
                Canvas.SetLeft(rec, left);

                // Add Labels on top of rectangles
                canvasText.Children.Add(lbl);
                Canvas.SetTop(lbl, top - 10);
                Canvas.SetLeft(lbl, left - 5);
            }
            catch
            {
                MessageBox.Show("An exception handled while creating a text label");
            }
        }


        // Extract Text Line by Lines
        private string ExtractTextByRegions(OCRText ocrOutput)
        {
            string resultText = String.Empty;

            foreach (Region ereg in ocrOutput.Regions)
            {
                resultText += string.Join("\n", from WLine sline in ereg.Lines
                                               from Word sword in sline.Words
                                               select (string)sword.Text);
            }

            return resultText;
        }

        private string ExtractTextByRegions(RecognizeText ocrOutput)
        {
            string resultText = String.Empty;

            resultText += string.Join("\n", from LineNew sline in ocrOutput.RecognitionResult.Lines
                                               select sline.Text);

            return resultText;
        }

        // Microsoft Cognitive Services Computer Vision OCR Method
        private async Task<string> MakeOCRRequest(string imageFilePath)
        {
            try
            {
                HttpClient client = new HttpClient();

                // Request headers.
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

                string uri = uriEndpoint;
                if (isNewOCREnabled)
                {
                    string requestParameters = String.Format("?mode={0}", (bool)checkboxTextMode.IsChecked ? "Handwritten" : "Printed");
                    // Assemble the URI for the REST API method.
                    uri += "recognizeText" + requestParameters;
                }
                else
                {
                    // Request parameters. 
                    // The language parameter doesn't specify a language, so the method detects it automatically.
                    // The detectOrientation parameter is set to true, so the method detects and corrects text orientation before detecting text.
                    string requestParameters = String.Format("language={0}&detectOrientation=true", docLanguage.Split()[0]);
                    // Assemble the URI for the REST API method.
                    uri += "ocr?" + requestParameters;
                }

                
                HttpResponseMessage response;

                // Read the contents of the specified local image
                // into a byte array.
                byte[] byteData = GetImageAsByteArray(imageFilePath);

                // Add the byte array as an octet stream to the request body.
                using (ByteArrayContent content = new ByteArrayContent(byteData))
                {
                    // This example uses the "application/octet-stream" content type.
                    // The other content types you can use are "application/json" and "multipart/form-data".
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                    // Asynchronously call the REST API method.
                    response = await client.PostAsync(uri, content);
                }

                if (isNewOCREnabled)
                {
                    string operationLocation = null;

                    // The response contains the URI to retrieve the result of the process.
                    if (response.IsSuccessStatusCode)
                    {
                        operationLocation = response.Headers.GetValues("Operation-Location").FirstOrDefault();
                    }

                    string contentString;
                    int i = 0;
                    do
                    {
                        System.Threading.Thread.Sleep(1000);
                        response = await client.GetAsync(operationLocation);
                        contentString = await response.Content.ReadAsStringAsync();
                        ++i;
                    }
                    while (i < 10 && contentString.IndexOf("\"status\":\"Succeeded\"") == -1);

                    return contentString;

                }
                else
                {
                    // Asynchronously get the JSON response.
                    string contentString = await response.Content.ReadAsStringAsync();
                    return contentString;
                }
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        static byte[] GetImageAsByteArray(string imageFilePath)
        {
            // Open a read-only file stream for the specified file.
            using (FileStream fileStream = new FileStream(imageFilePath, FileMode.Open, FileAccess.Read))
            {
                // Read the file's contents into a byte array.
                BinaryReader binaryReader = new BinaryReader(fileStream);
                return binaryReader.ReadBytes((int)fileStream.Length);
            }
        }

        // Update Setting Panel paramaters
        public void UpdateConnectionKeys(string subscriptionValue, string endpointValue, string languageValue, bool isnewocrenabled)
        {
            XmlDocument appconfigFile = new XmlDocument();
            appconfigFile.Load(AppDomain.CurrentDomain.BaseDirectory + "App.config");
            XmlNode appSettingsNode = appconfigFile.SelectSingleNode("configuration/appSettings");

            foreach (XmlNode childNode in appSettingsNode)
            {
                if (childNode.Attributes["key"].Value == "subscriptionKey") childNode.Attributes["value"].Value = subscriptionValue;
                else if (childNode.Attributes["key"].Value == "endpointRegion") childNode.Attributes["value"].Value = endpointValue;
                else if (childNode.Attributes["key"].Value == "documentLanguage") childNode.Attributes["value"].Value = languageValue;
                else if (childNode.Attributes["key"].Value == "isNewOCREnabled") childNode.Attributes["value"].Value = isnewocrenabled == true? "true":"false";
            }

            appconfigFile.Save(AppDomain.CurrentDomain.BaseDirectory + "App.config");
            appconfigFile.Save(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);
            MessageBox.Show("Fields are updated!");
        }

        // Update Key-Value Field Extraction parameters
        public void UpdateKeyValue(string searchKeyValue, int searchKeyWidthValue)
        {
            XmlDocument appconfigFile = new XmlDocument();
            appconfigFile.Load(AppDomain.CurrentDomain.BaseDirectory + "App.config");
            XmlNode appSettingsNode = appconfigFile.SelectSingleNode("configuration/appSettings");

            foreach (XmlNode childNode in appSettingsNode)
            {
                if (childNode.Attributes["key"].Value == "searchValues") childNode.Attributes["value"].Value = searchKeyValue;
                else if (childNode.Attributes["key"].Value == "searchValuesWidth") childNode.Attributes["value"].Value = searchKeyWidthValue.ToString();
            }

            appconfigFile.Save(AppDomain.CurrentDomain.BaseDirectory + "App.config");
            appconfigFile.Save(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);
            MessageBox.Show("Field keys are updated!");
        }

        // Browse an image to send to Microsoft Cognitive Services OCR endpoint
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

            canvasText.Height = bitmapSource.Height;
            canvasText.Width = bitmapSource.Width;

            // This is for calculating RenderSize on the screen
            DPIrenderSize = bitmapSource.PixelHeight / bitmapSource.Height;
            
            imgInvoice.Stretch = Stretch.Fill;
            imgInvoice.Source = bitmapSource;

            //Clear previous queries if exists.
            canvasText.Children.Clear();
            canvasRegions.Children.Clear();
            canvasLines.Children.Clear();
            canvasWords.Children.Clear();
            textvalues.Clear();
            gridKeyVal.Visibility = Visibility.Collapsed;
            stckKeyValResult.Visibility = Visibility.Collapsed;
            listBoxKeyValue.Items.Clear();

            OCRResponse = await MakeOCRRequest(filePath);
            if (isNewOCREnabled)
{
                ExtractTextAndRegionsNewAPIFromResponse();
            }
            else 
            ExtractTextAndRegionsFromResponse();

        }

        // Change Visibility of Bounding Box Layers
        private void ButtonVisiblity_Click(object sender, RoutedEventArgs e)
        {
            switch (((System.Windows.Controls.HeaderedItemsControl)sender).Header)
            {
                case "Regions":
                    SwitchCanvasVisibility(canvasRegions);
                    regionVis = !regionVis;
                    break;
                case "Lines":
                    SwitchCanvasVisibility(canvasLines);
                    lineVis = !lineVis;
                    break;
                case "Words":
                    SwitchCanvasVisibility(canvasWords);
                    wordVis = !wordVis;
                    break;
                case "Texts":
                    SwitchCanvasVisibility(canvasText);
                    textVis = !textVis;
                    break;
            }
        }

        private void SwitchCanvasVisibility(System.Windows.Controls.Canvas swcanvas)
        {
            if (swcanvas.Visibility == Visibility.Visible)
            {
                swcanvas.Visibility = Visibility.Collapsed;
            }
            else
            {
                swcanvas.Visibility = Visibility.Visible;
            }
        }

        private void SwitchStackVisibility(System.Windows.Controls.StackPanel swstack)
        {
            if (swstack.Visibility == Visibility.Visible)
            {
                swstack.Visibility = Visibility.Collapsed;
            }
            else
            {
                swstack.Visibility = Visibility.Visible;
            }
        }

        private void SwitchGridVisibility(System.Windows.Controls.Grid swgrid)
        {
            if (swgrid.Visibility == Visibility.Visible)
            {
                swgrid.Visibility = Visibility.Collapsed;
            }
            else
            {
                swgrid.Visibility = Visibility.Visible;
            }
        }

        private void ButtonVisiblityOutPut_Click(object sender, RoutedEventArgs e)
        {
            SwitchStackVisibility(stckOutput);
        }

        private void ButtonSettings_Click(object sender, RoutedEventArgs e)
        {
            if (gridSettings.Visibility == Visibility.Visible)
            {
                gridSettings.Visibility = Visibility.Collapsed;
            }
            else
            {
                checkboxIsNewOCR.IsChecked = isNewOCREnabled;
                txtSubscriptionKey.Text = subscriptionKey;
                txtEndPoint.Text = uriEndpoint;
                txtKeys.Text = searchKeys;
                comboLanguage.SelectedItem = docLanguage;
                gridSettings.Visibility = Visibility.Visible;
            }
        }

        private void ButtonSettingsUpdate_Click(object sender, RoutedEventArgs e)
        {
            subscriptionKey = txtSubscriptionKey.Text;
            uriEndpoint = txtEndPoint.Text;
            docLanguage = (comboLanguage.SelectedValue as ComboBoxItem).Content as string;
            isNewOCREnabled = (bool)checkboxIsNewOCR.IsChecked;

            UpdateConnectionKeys(subscriptionKey, uriEndpoint, docLanguage, isNewOCREnabled);
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

        // Enable/Disable Language due to selection of checkbox
        private void NewOCRCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            isNewOCREnabled = (bool)(sender as CheckBox).IsChecked;
            comboLanguage.IsEnabled = !isNewOCREnabled;
        }

        private void ButtonKeyValue_Click(object sender, RoutedEventArgs e)
        {
            SwitchGridVisibility(gridKeyVal);
        }

        private void ButtonKeyValueUpdate_Click(object sender, RoutedEventArgs e)
        {
            searchKeys = txtKeys.Text;
            searchKeysWidth = Convert.ToInt32(txtKeysWidth.Text);

            UpdateKeyValue(searchKeys, searchKeysWidth);
            SwitchGridVisibility(gridKeyVal);
        }

        private void MouseLeftButtonKeyValue_Down(object sender, MouseButtonEventArgs e)
        {
            // Collapse the grid if it's clicked on the background
            if(((System.Windows.FrameworkElement)sender).Name == "recKeyVal")
                gridKeyVal.Visibility = Visibility.Collapsed;
            else
                gridSettings.Visibility = Visibility.Collapsed;
        }
    }
}

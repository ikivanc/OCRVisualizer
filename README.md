# OCR Visualizer
OCRVisualizer is a tool to visualize Microsoft Cognitive Services OCR API json output to get familiar with bounding boxes of Regions, Lines and Words. It's written in C#/WPF.

This tool will be helful for your data discovery, if you use OCR with your documents.

## Main Functinality
In this version;
* You can see Bonding boxes of Regions, Lines and Words
* You can see extracted text over your original document
* You can extract full text as output

## Build the project

Please chance in `Mainpage.xaml.cs` file below code snippet with your Cognitive Services Computer Vision API subscription key and if your service hosted other than `northeurope` region, change the region with yours. 
```csharp
// Microsoft Cognitive Services Computer Vision Endpoint details.
const string subscriptionKey = "YOUR_CUMPUTER_VISION_API_KEY";
const string uriBase ="https://northeurope.api.cognitive.microsoft.com/vision/v2.0/ocr";
```

Here are some examples of of output of documents.

OCR for unstuctured documents.
![](screenshots/ocroutput2.png)

OCR for a part of documents
![](screenshots/ocroutput1.png)

OCR from full page documents 
![](screenshots/ocroutput.png)


For more information about Optical character recognition (OCR) in images | [Demo](https://azure.microsoft.com/en-us/services/cognitive-services/computer-vision/#text) | [Container Support](https://docs.microsoft.com/en-us/azure/cognitive-services/computer-vision/computer-vision-how-to-install-containers)

Thanks.
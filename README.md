# OCR Visualizer
OCRVisualizer is a tool to visualize Microsoft Cognitive Services OCR API json output to get familiar with bounding boxes of Regions, Lines and Words. It's written in C#/WPF.

This tool will be helful for your data discovery, if you use OCR with your documents.

## Main Functinality
In this version;
* You can see Bonding boxes of Regions, Lines and Words
* You can see extracted text over your original document
* You can extract full text as output

## Run Executable File
If you would like to test OCR visualizer on your local, download Setup File and update with your `subscription Key` and `endpoint` in `OCR Settings`. After defining your endpoint you can visualize your OCR documents via `Browse`.

* [Download OCRVisualizer Executable File on Windows](Setup/Release_Setup.zip) 

![](screenshots/setup.png)


## Build the project

Please change in `App.config` file below code snippet with your Cognitive Services Computer Vision API subscription key and if your service hosted other than `northeurope` region, change the region with yours.

Microsoft Cognitive Services Computer Vision Endpoint details.

```json
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <startup>
    <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.6.1" />
  </startup>
  <appSettings>
    <add key="subscriptionKey" value="YOUR_COMPUTER_VISION_API_KEY" />
    <add key="endpointRegion" value="https://northeurope.api.cognitive.microsoft.com/vision/v2.0/ocr" />
  </appSettings>
</configuration>
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
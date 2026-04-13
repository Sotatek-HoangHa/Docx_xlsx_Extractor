using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Linq;

// Read the filled template
using (WordprocessingDocument doc = WordprocessingDocument.Open("sample-template-filled.docx", false))
{
    var body = doc.MainDocumentPart.Document.Body;
    
    Console.WriteLine("=== FILLED TEMPLATE STRUCTURE ===\n");
    
    foreach (var element in body.Elements())
    {
        if (element is Paragraph para)
        {
            Console.WriteLine($"Paragraph:");
            foreach (var child in para.Elements())
            {
                if (child is SdtRun sdtRun)
                {
                    Console.WriteLine("  SdtRun:");
                    foreach (var sdtChild in sdtRun.Elements())
                    {
                        Console.WriteLine($"    - {sdtChild.GetType().Name}");
                        
                        if (sdtChild is Run run)
                        {
                            foreach (var text in run.Elements<Text>())
                            {
                                Console.WriteLine($"      Text: '{text.Text}'");
                            }
                        }
                        else if (sdtChild is SdtContentRun scr)
                        {
                            Console.WriteLine($"      SdtContentRun children:");
                            foreach (var scrChild in scr.Elements())
                            {
                                Console.WriteLine($"        - {scrChild.GetType().Name}");
                                if (scrChild is Run scrRun)
                                {
                                    foreach (var text in scrRun.Elements<Text>())
                                    {
                                        Console.WriteLine($"          Text: '{text.Text}'");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

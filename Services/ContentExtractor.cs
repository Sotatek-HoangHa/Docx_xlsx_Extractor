using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocxExtractor.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DocxExtractor.Services;

/// <summary>
/// Extracts content control data from a DOCX file and returns structured field values.
/// </summary>
public class ContentExtractor
{
    /// <summary>
    /// Extracts all content control data from a DOCX file.
    /// </summary>
    /// <param name="filePath">Path to the DOCX file</param>
    /// <returns>List of extracted field values</returns>
    public static List<FieldValue> ExtractFields(string filePath)
    {
        List<FieldValue> fields = new List<FieldValue>();

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        using (WordprocessingDocument doc = WordprocessingDocument.Open(filePath, false))
        {
            MainDocumentPart? mainPart = doc.MainDocumentPart;
            if (mainPart?.Document.Body == null)
            {
                return fields;
            }

            Body body = mainPart.Document.Body;

            // Walk through all elements in the body
            foreach (var element in body.Elements())
            {
                ExtractFromElement(element, fields);
            }
        }

        return fields;
    }

    /// <summary>
    /// Recursively extracts content controls from document elements.
    /// </summary>
    private static void ExtractFromElement(OpenXmlElement element, List<FieldValue> fields)
    {
        // Handle block-level SDTs
        if (element is SdtBlock sdtBlock)
        {
            FieldValue? field = ExtractFromSdt(sdtBlock.SdtProperties, sdtBlock);
            if (field != null)
            {
                fields.Add(field);
            }
        }
        // Handle paragraphs containing inline SDTs
        else if (element is Paragraph paragraph)
        {
            foreach (var run in paragraph.Elements<OpenXmlElement>())
            {
                if (run is SdtRun sdtRun)
                {
                    FieldValue? field = ExtractFromSdt(sdtRun.SdtProperties, sdtRun);
                    if (field != null)
                    {
                        fields.Add(field);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Extracts field data from an SDT element.
    /// </summary>
    private static FieldValue? ExtractFromSdt(SdtProperties? props, OpenXmlElement sdtElement)
    {
        if (props == null)
        {
            return null;
        }

        // Get key from Tag
        string key = props.Elements<Tag>().FirstOrDefault()?.Val?.Value ?? string.Empty;
        if (string.IsNullOrEmpty(key))
        {
            return null;
        }

        // Get label from Alias
        string label = props.Elements<SdtAlias>().FirstOrDefault()?.Val?.Value ?? key;

        // Extract content value
        string value = ExtractContentValue(sdtElement);

        // Determine control type (simplified - just return "Content Control" for now)
        string controlType = "Content Control";

        return new FieldValue
        {
            Key = key,
            Label = label,
            Value = value,
            Type = controlType
        };
    }

    /// <summary>
    /// Extracts all text content from an SDT element.
    /// </summary>
    private static string ExtractContentValue(OpenXmlElement sdtElement)
    {
        StringBuilder sb = new StringBuilder();

        if (sdtElement is SdtBlock sdtBlock)
        {
            // For block-level, iterate through all elements
            foreach (var child in sdtBlock.Elements())
            {
                if (child is not SdtProperties)
                {
                    ExtractTextFromElement(child, sb);
                }
            }
        }
        else if (sdtElement is SdtRun sdtRun)
        {
            // For run-level, iterate through all elements
            foreach (var child in sdtRun.Elements())
            {
                if (child is not SdtProperties)
                {
                    ExtractTextFromElement(child, sb);
                }
            }
        }

        return sb.ToString().Trim();
    }

    /// <summary>
    /// Recursively extracts text from elements.
    /// </summary>
    private static void ExtractTextFromElement(OpenXmlElement element, StringBuilder sb)
    {
        if (element is Paragraph paragraph)
        {
            foreach (var run in paragraph.Elements<Run>())
            {
                foreach (var text in run.Elements<Text>())
                {
                    sb.Append(text.Text);
                }
            }
        }
        else if (element is SdtContentRun sdtContentRun)
        {
            // Handle SdtContentRun by extracting text from its Run children
            foreach (var run in sdtContentRun.Elements<Run>())
            {
                foreach (var text in run.Elements<Text>())
                {
                    sb.Append(text.Text);
                }
            }
        }
        else if (element is Run run)
        {
            foreach (var text in run.Elements<Text>())
            {
                sb.Append(text.Text);
            }
        }
    }
}

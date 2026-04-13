using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.IO;

namespace DocxExtractor.Services;

/// <summary>
/// Creates a sample DOCX template with content controls (structured document tags).
/// </summary>
public class TemplateCreator
{
    /// <summary>
    /// Creates a sample DOCX template file with multiple content controls.
    /// </summary>
    public static void CreateSampleTemplate(string filePath)
    {
        using (WordprocessingDocument doc = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document))
        {
            MainDocumentPart mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document();

            Body body = new Body();

            // Add title
            AddHeading(body, "APPLICATION FORM");

            // Add instructions
            AddParagraph(body, "Please fill in all required fields below.");

            // Section 1: Personal Information
            AddHeading(body, "1. PERSONAL INFORMATION");
            AddControlledParagraph(body, "Full Name", "full_name");
            AddControlledParagraph(body, "Email Address", "email_address");
            AddControlledParagraph(body, "Date of Birth", "birth_date");

            // Section 2: Additional Information
            AddHeading(body, "2. ADDITIONAL INFORMATION");
            AddControlledParagraph(body, "Gender", "gender");
            AddControlledParagraph(body, "Country", "country");
            AddControlledParagraph(body, "Address", "address");

            // Section 3: Agreement
            AddHeading(body, "3. AGREEMENT");
            AddControlledParagraph(body, "I agree to the terms and conditions", "agree_terms");

            body.AppendChild(new Paragraph());

            // Add signature section
            AddParagraph(body, "Signature: ___________________     Date: ___________");

            mainPart.Document.AppendChild(body);
            mainPart.Document.Save();
        }
    }

    /// <summary>
    /// Adds a heading paragraph.
    /// </summary>
    private static void AddHeading(Body body, string text)
    {
        Paragraph para = new Paragraph();
        ParagraphProperties props = new ParagraphProperties();
        props.AppendChild(new ParagraphStyleId { Val = "Heading1" });
        props.AppendChild(new SpacingBetweenLines { Before = "240", After = "120" });
        para.AppendChild(props);

        Run run = new Run();
        run.AppendChild(new Text { Text = text });
        para.AppendChild(run);

        body.AppendChild(para);
    }

    /// <summary>
    /// Adds a regular paragraph.
    /// </summary>
    private static void AddParagraph(Body body, string text)
    {
        Paragraph para = new Paragraph();
        ParagraphProperties props = new ParagraphProperties();
        props.AppendChild(new SpacingBetweenLines { After = "120" });
        para.AppendChild(props);

        Run run = new Run();
        run.AppendChild(new Text { Text = text });
        para.AppendChild(run);

        body.AppendChild(para);
    }

    /// <summary>
    /// Adds a paragraph with a content control (SDT).
    /// </summary>
    private static void AddControlledParagraph(Body body, string label, string key)
    {
        Paragraph para = new Paragraph();
        ParagraphProperties paraProps = new ParagraphProperties();
        paraProps.AppendChild(new SpacingBetweenLines { After = "120" });
        para.AppendChild(paraProps);

        // Add label
        Run labelRun = new Run();
        RunProperties labelProps = new RunProperties();
        labelProps.AppendChild(new Bold());
        labelRun.AppendChild(labelProps);
        labelRun.AppendChild(new Text { Text = $"{label}: " });
        para.AppendChild(labelRun);

        // Add content control
        SdtRun sdt = new SdtRun();

        SdtProperties sdtProps = new SdtProperties();
        sdtProps.AppendChild(new Tag { Val = key });
        sdtProps.AppendChild(new SdtAlias { Val = label });
        sdt.AppendChild(sdtProps);

        // Content with placeholder
        SdtContentRun sdtContent = new SdtContentRun();
        Run contentRun = new Run();
        RunProperties contentProps = new RunProperties();
        contentProps.AppendChild(new Italic());
        contentProps.AppendChild(new Color { Val = "808080" });
        contentRun.AppendChild(contentProps);
        contentRun.AppendChild(new Text { Text = "[Please fill this field]", Space = SpaceProcessingModeValues.Preserve });
        sdtContent.AppendChild(contentRun);
        sdt.AppendChild(sdtContent);

        para.AppendChild(sdt);
        body.AppendChild(para);
    }
}

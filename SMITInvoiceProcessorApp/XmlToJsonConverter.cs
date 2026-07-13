using System;
using System.IO;
using System.Xml.Linq;
using System.Text;
using SchedulerJob;

public static class XmlToJsonConverter
{
    public static string ConvertXmlToJson(string xmlFilePath)
    {
        try
        {
            string rawXml = File.ReadAllText(xmlFilePath);
            string safeXml = SanitizeXmlContent(rawXml);

            XDocument doc = XDocument.Parse(safeXml);

            XElement root = doc.Root;

            StringBuilder json = new StringBuilder();
            json.Append("{");
            json.Append($"\"{root.Name.LocalName}\":");
            json.Append("{");

            bool first = true;

            // 🔥 IMPORTANT: read attributes
            foreach (var attr in root.Attributes())
            {
                if (!first) json.Append(",");

                json.Append($"\"{attr.Name.LocalName}\":");
                json.Append($"\"{Escape(attr.Value)}\"");

                first = false;
            }

            json.Append("}");
            json.Append("}");

            return json.ToString();
        }
        catch (Exception ex)
        {
            Logger.Error("XML to JSON failed");
            Logger.Error(ex);
            return "{}";
        }
    }

    private static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "")
            .Replace("\r", "");
    }

    private static string SanitizeXmlContent(string xml)
    {
        return xml.Replace("&", "&amp;");
    }
}
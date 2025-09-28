using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using SvgConverterApp.Models;

namespace SvgConverterApp.Services;

public class SvgConversionService : ISvgConversionService
{
    private static readonly XNamespace SvgNamespace = "http://www.w3.org/2000/svg";
    private static readonly XNamespace InkscapeNamespace = "http://www.inkscape.org/namespaces/inkscape";
    private static readonly XNamespace SodipodiNamespace = "http://sodipodi.sourceforge.net/DTD/sodipodi-0.dtd";
    private static readonly XNamespace SiemensNamespace = "http://www.siemens.com/schemas/svghmi/1.0";

    public Task<string> ConvertAsync(string svgContent, SvgFormat inputFormat, SvgFormat outputFormat, CancellationToken cancellationToken = default)
    {
        if (outputFormat == inputFormat)
        {
            return Task.FromResult(svgContent);
        }

        return Task.Run(() => ConvertInternal(svgContent, inputFormat, outputFormat), cancellationToken);
    }

    private string ConvertInternal(string svgContent, SvgFormat inputFormat, SvgFormat outputFormat)
    {
        var document = XDocument.Parse(svgContent, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

        NormalizeDocument(document, inputFormat);
        ApplyTargetFormat(document, outputFormat);

        EnsureDeclaration(document);
        return document.ToString(SaveOptions.DisableFormatting);
    }

    private static void NormalizeDocument(XDocument document, SvgFormat inputFormat)
    {
        if (document.Root is null)
        {
            throw new InvalidOperationException("Das Dokument enthält kein Root-Element.");
        }

        // Always ensure default SVG namespace
        if (document.Root.Name.Namespace == XNamespace.None)
        {
            document.Root.Name = SvgNamespace + document.Root.Name.LocalName;
        }

        switch (inputFormat)
        {
            case SvgFormat.Inkscape:
                StripNamespace(document.Root, InkscapeNamespace);
                StripNamespace(document.Root, SodipodiNamespace);
                RemoveMetadataByPrefix(document.Root, "inkscape");
                RemoveMetadataByPrefix(document.Root, "sodipodi");
                break;
            case SvgFormat.SiemensSvghmi:
                StripNamespace(document.Root, SiemensNamespace);
                RemoveMetadataByPrefix(document.Root, "siemens");
                break;
        }
    }

    private static void ApplyTargetFormat(XDocument document, SvgFormat outputFormat)
    {
        if (document.Root is null)
        {
            throw new InvalidOperationException("Das Dokument enthält kein Root-Element.");
        }

        switch (outputFormat)
        {
            case SvgFormat.Standard:
                ApplyStandardMetadata(document.Root);
                break;
            case SvgFormat.Inkscape:
                ApplyInkscapeMetadata(document.Root);
                break;
            case SvgFormat.SiemensSvghmi:
                ApplySiemensMetadata(document.Root);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(outputFormat), outputFormat, null);
        }
    }

    private static void ApplyStandardMetadata(XElement root)
    {
        root.SetAttributeValue(XNamespace.Xmlns + "inkscape", null);
        root.SetAttributeValue(XNamespace.Xmlns + "sodipodi", null);
        root.SetAttributeValue(XNamespace.Xmlns + "siemens", null);
        root.SetAttributeValue(XNamespace.Xmlns + "xlink", "http://www.w3.org/1999/xlink");
        root.SetAttributeValue("version", root.Attribute("version")?.Value ?? "1.1");
    }

    private static void ApplyInkscapeMetadata(XElement root)
    {
        root.SetAttributeValue(XNamespace.Xmlns + "inkscape", InkscapeNamespace.NamespaceName);
        root.SetAttributeValue(XNamespace.Xmlns + "sodipodi", SodipodiNamespace.NamespaceName);
        root.SetAttributeValue("inkscape:version", "1.3");
        root.SetAttributeValue("sodipodi:docname", CreateSafeFileName(root.Attribute("id")?.Value ?? "document"));
    }

    private static void ApplySiemensMetadata(XElement root)
    {
        root.SetAttributeValue(XNamespace.Xmlns + "siemens", SiemensNamespace.NamespaceName);
        root.SetAttributeValue("siemens:version", "1.0");
        root.SetAttributeValue("siemens:exported", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
    }

    private static void StripNamespace(XElement element, XNamespace ns)
    {
        var attributes = element.Attributes().Where(a => a.IsNamespaceDeclaration && a.Value == ns.NamespaceName).ToList();
        foreach (var attribute in attributes)
        {
            attribute.Remove();
        }

        var descendants = element.DescendantsAndSelf().ToList();
        foreach (var descendant in descendants)
        {
            if (descendant.Name.Namespace == ns)
            {
                descendant.Name = XNamespace.None + descendant.Name.LocalName;
            }

            var attributesToRemove = descendant.Attributes().Where(a => a.Name.Namespace == ns).ToList();
            foreach (var attribute in attributesToRemove)
            {
                attribute.Remove();
            }
        }
    }

    private static void RemoveMetadataByPrefix(XElement element, string prefix)
    {
        var attributesToRemove = element.DescendantsAndSelf()
            .SelectMany(x => x.Attributes())
            .Where(a => a.Name.NamespaceName.Contains(prefix, StringComparison.OrdinalIgnoreCase) ||
                        a.Name.LocalName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var attribute in attributesToRemove)
        {
            attribute.Remove();
        }
    }

    private static void EnsureDeclaration(XDocument document)
    {
        if (document.Declaration is null)
        {
            document.Declaration = new XDeclaration("1.0", "utf-8", "yes");
        }
        else
        {
            document.Declaration.Encoding = document.Declaration.Encoding ?? "utf-8";
        }
    }

    private static string CreateSafeFileName(string baseName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(baseName.Length);
        foreach (var ch in baseName)
        {
            builder.Append(invalidChars.Contains(ch) ? '_' : ch);
        }

        return builder.Length == 0 ? "document" : builder.ToString();
    }
}

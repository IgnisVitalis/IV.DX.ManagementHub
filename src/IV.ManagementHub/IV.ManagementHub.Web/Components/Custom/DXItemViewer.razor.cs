using IV.DX.Kernel.Enums;
using IV.ManagementHub.Common.Models;
using IV.ManagementHub.Web.Components.Custom.Base;
using IV.ManagementHub.Web.Models;
using Microsoft.AspNetCore.Components;
using System.Globalization;

namespace IV.ManagementHub.Web.Components.Custom
{
    public partial class DXItemViewer : DXItemEditorBaseComponent
    {
        [Parameter, EditorRequired] public DXColumnDefinition ColumnDefinition { get; set; } = default!;
        [Parameter, EditorRequired] public DXRecordItem DXItem { get; set; } = default!;

        private string GetDisplayText()
        {
            switch (ColumnDefinition.ColumnType)
            {
                case DXColumnTypeEnum.Bool:
                    return GetBool(DXItem.Content, ColumnDefinition) ? "Yes" : "No";

                case DXColumnTypeEnum.HashedString:
                case DXColumnTypeEnum.EncryptedString:
                    return base.GetStringN(DXItem.Content, ColumnDefinition) is { Length: > 0 } ? "******" : "—";

                case DXColumnTypeEnum.String:
                case DXColumnTypeEnum.Text:
                    {
                        var s = base.GetStringN(DXItem.Content, ColumnDefinition);
                        return string.IsNullOrWhiteSpace(s) ? "—" : s!;
                    }

                case DXColumnTypeEnum.DateTime:
                    {
                        var dt = base.GetDateTime(DXItem.Content, ColumnDefinition);
                        return dt.HasValue
                            ? dt.Value.ToString("u", CultureInfo.InvariantCulture).Replace("Z", "")
                            : "—";
                    }

                case DXColumnTypeEnum.Int:
                    {
                        var n = GetIntN(DXItem.Content, ColumnDefinition);
                        if (!n.HasValue) return "—";

                        if (ColumnDefinition.EnumValues is { Count: > 0 } &&
                            ColumnDefinition.EnumValues.TryGetValue(n.Value, out var label) &&
                            !string.IsNullOrWhiteSpace(label))
                        {
                            return label;
                        }

                        return n.Value.ToString(CultureInfo.InvariantCulture);
                    }

                case DXColumnTypeEnum.Short:
                case DXColumnTypeEnum.Long:
                    {
                        var n = base.GetLongN(DXItem.Content, ColumnDefinition);
                        return n.HasValue ? n.Value.ToString(CultureInfo.InvariantCulture) : "—";
                    }

                case DXColumnTypeEnum.Float:
                    {
                        var n = base.GetDoubleN(DXItem.Content, ColumnDefinition);
                        return n.HasValue ? n.Value.ToString(CultureInfo.InvariantCulture) : "—";
                    }

                case DXColumnTypeEnum.Decimal:
                case DXColumnTypeEnum.Currency:
                    {
                        var n = base.GetDecimalN(DXItem.Content, ColumnDefinition);
                        return n.HasValue ? n.Value.ToString(CultureInfo.InvariantCulture) : "—";
                    }

                case DXColumnTypeEnum.GUID:
                    {
                        var g = base.GetGuid(DXItem.Content, ColumnDefinition);
                        if (!g.HasValue) return "—";

                        if (ColumnDefinition.RelationValues is { Count: > 0 } &&
                            ColumnDefinition.RelationValues.TryGetValue(g.Value, out var label) &&
                            !string.IsNullOrWhiteSpace(label))
                        {
                            return label;
                        }

                        return g.Value.ToString();
                    }

                default:
                    return "—";
            }
        }

        private string GetBlobDownloadHref()
        {
            if (!base.HasBlob(DXItem.Content, ColumnDefinition))
                return string.Empty;

            base.TryGetBlobMeta(DXItem.Content, ColumnDefinition, out _, out var mime, out _);
            var raw = base.GetBlobRawBytes(DXItem.Content, ColumnDefinition) ?? Array.Empty<byte>();

            return $"data:{(string.IsNullOrWhiteSpace(mime) ? "application/octet-stream" : mime)};base64,{Convert.ToBase64String(raw)}";
        }

        private static string GetSafeDownloadName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "file";
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        private string GetBlobFileName()
        {
            base.TryGetBlobMeta(DXItem.Content, ColumnDefinition, out var name, out _, out _);
            return string.IsNullOrWhiteSpace(name) ? "file" : name;
        }

        private static string FormatSize(long bytes)
        {
            if (bytes <= 0) return "0 KB";

            const double KB = 1024d;
            const double MB = 1024d * KB;
            const double GB = 1024d * MB;

            static string F(double v) => v < 10 ? v.ToString("0.##", CultureInfo.InvariantCulture)
                                               : v.ToString("0.#", CultureInfo.InvariantCulture);

            if (bytes >= GB) return $"{F(bytes / GB)} GB";
            if (bytes >= MB) return $"{F(bytes / MB)} MB";

            return $"{F(bytes / KB)} KB";
        }
    }
}


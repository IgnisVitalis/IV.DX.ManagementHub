using IV.DX.ManagementHub.Common.Models;
using IV.DX.ManagementHub.Web.Components.Custom.Base;
using IV.DX.ManagementHub.Web.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Globalization;

namespace IV.DX.ManagementHub.Web.Components.Custom
{
    public partial class DXItemEditor : DXItemEditorBaseComponent
    {
        [Parameter, EditorRequired] public DXColumnDefinition ColumnDefinition { get; set; } = default!;
        [Parameter, EditorRequired] public DXRecordItem DXItem { get; set; } = default!;

        private int _blobProgressPercent;
        private string? _blobProgressTitle;
        private string? _pendingName;
        private long? _pendingSize;

        private async Task OnBlobInputFileChange(InputFileChangeEventArgs e)
        {
            var file = e.File;

            _pendingName = file.Name;
            _pendingSize = file.Size;

            _blobProgressTitle = base.HasBlob(DXItem.Content, ColumnDefinition) ? "Replacing..." : "Uploading...";
            _blobProgressPercent = 0;
            await InvokeAsync(StateHasChanged);

            using var stream = file.OpenReadStream(maxAllowedSize: 50L * 1024 * 1024);

            var total = file.Size;
            long read = 0;

            const int bufferSize = 64 * 1024; // 64 KB
            var buffer = new byte[bufferSize];

            using var ms = new MemoryStream((int)Math.Min(total, int.MaxValue));

            int n;
            while ((n = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await ms.WriteAsync(buffer, 0, n);
                read += n;

                _blobProgressPercent = total == 0 ? 0 : (int)Math.Round(read * 100d / total);
                if (_blobProgressPercent > 99) _blobProgressPercent = 99;

                await InvokeAsync(StateHasChanged);
            }

            var bytes = ms.ToArray();
                    
            await base.SetBlobAsync(DXItem.Content, ColumnDefinition, e);

            _pendingName = null;
            _pendingSize = null;

            _blobProgressPercent = 100;
            _blobProgressTitle = "Completed";
            await InvokeAsync(StateHasChanged);
        }

        private string GetBlobDownloadHref()
        {
            // Берём raw bytes из контейнера + mime, делаем data-url
            if (!base.HasBlob(DXItem.Content, ColumnDefinition))
                return string.Empty;

            // packed bytes в словаре — container
            // достанем meta+data через TryGetBlobMeta + GetBlobRawBytes
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

        private string _blobInputId = $"blob_{Guid.NewGuid():N}";
      
        private string GetBlobFileName()
        {
            base.TryGetBlobMeta(DXItem.Content, ColumnDefinition, out var name, out _, out _);
            return string.IsNullOrWhiteSpace(name) ? "file" : name;
        }

        protected static string FormatSize(long bytes)
        {
            if (bytes <= 0) return "0 KB";

            const double KB = 1024d;
            const double MB = 1024d * KB;
            const double GB = 1024d * MB;

            string F(double v) => v < 10 ? v.ToString("0.##", CultureInfo.InvariantCulture)
                                         : v.ToString("0.#", CultureInfo.InvariantCulture);

            if (bytes >= GB) return $"{F(bytes / GB)} GB";
            if (bytes >= MB) return $"{F(bytes / MB)} MB";

            return $"{F(bytes / KB)} KB";
        }

        private void ClearBlobUi()
        {
            base.ClearBlob(DXItem.Content, ColumnDefinition);

            _blobProgressPercent = 0;
            _blobProgressTitle = null;

            _blobInputId = $"blob_{ColumnDefinition.Name}_{DXItem.Id}_{Guid.NewGuid():N}";

            StateHasChanged();
        }
    }
}

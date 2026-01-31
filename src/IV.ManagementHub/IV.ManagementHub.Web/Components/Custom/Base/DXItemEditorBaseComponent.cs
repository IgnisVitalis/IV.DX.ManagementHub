using IV.DataProvider.WebApp.Services.Web.ApiClients;
using IV.DX.Kernel.Enums;
using IV.DX.Kernel.Models;
using IV.ManagementHub.Common.Models;
using IV.ManagementHub.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Globalization;

namespace IV.ManagementHub.Web.Components.Custom.Base
{
    public abstract class DXItemEditorBaseComponent : ComponentBase
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        protected IDictionary<string, object?> GetRequiredAttr(DXColumnDefinition col)
            => col.AllowNull ? new Dictionary<string, object?>()
                             : new Dictionary<string, object?> { ["required"] = true };

        // -------------------- helpers --------------------
        private static bool TryGet(IDictionary<string, object?> row, string name, out object? value)
        {
            if (row is null) { value = null; return false; }
            if (!row.TryGetValue(name, out var v)) { value = null; return false; }
            value = v;
            return true;
        }

        private static T? ConvertTo<T>(object? v)
        {
            if (v is null) return default;
            // direct
            if (v is T t) return t;

            try
            {
                // numeric widen/narrow (int<-long, double<-int, etc.)
                var target = typeof(T);

                if (target == typeof(int?) || target == typeof(int))
                {
                    if (v is int vi) return (T)(object)vi;
                    if (v is long vl && vl >= int.MinValue && vl <= int.MaxValue) return (T)(object)(int)vl;
                    if (v is short vs) return (T)(object)(int)vs;
                    if (v is byte vb) return (T)(object)(int)vb;
                    if (v is string si && int.TryParse(si, NumberStyles.Integer, Inv, out var ip)) return (T)(object)ip;
                }

                if (target == typeof(long?) || target == typeof(long))
                {
                    if (v is long vl) return (T)(object)vl;
                    if (v is int vi) return (T)(object)(long)vi;
                    if (v is short vs) return (T)(object)(long)vs;
                    if (v is byte vb) return (T)(object)(long)vb;
                    if (v is string sl && long.TryParse(sl, NumberStyles.Integer, Inv, out var lp)) return (T)(object)lp;
                }

                if (target == typeof(double?) || target == typeof(double))
                {
                    if (v is double vd) return (T)(object)vd;
                    if (v is float vf) return (T)(object)(double)vf;
                    if (v is decimal vdc) return (T)(object)(double)vdc;
                    if (v is int vi) return (T)(object)(double)vi;
                    if (v is long vl) return (T)(object)(double)vl;
                    if (v is string sd && double.TryParse(sd, NumberStyles.Float | NumberStyles.AllowThousands, Inv, out var dp)) return (T)(object)dp;
                }

                if (target == typeof(decimal?) || target == typeof(decimal))
                {
                    if (v is decimal vd) return (T)(object)vd;
                    if (v is double vdd) return (T)(object)(decimal)vdd;
                    if (v is float vf) return (T)(object)(decimal)vf;
                    if (v is int vi) return (T)(object)(decimal)vi;
                    if (v is long vl) return (T)(object)(decimal)vl;
                    if (v is string ss && decimal.TryParse(ss, NumberStyles.Number, Inv, out var dp)) return (T)(object)dp;
                }

                if (target == typeof(bool?) || target == typeof(bool))
                {
                    if (v is bool vb) return (T)(object)vb;
                    if (v is string sb && bool.TryParse(sb, out var bp)) return (T)(object)bp;
                    if (v is int n) return (T)(object)(n != 0);
                    if (v is long ln) return (T)(object)(ln != 0);
                }

                if (target == typeof(DateTime?) || target == typeof(DateTime))
                {
                    if (v is DateTime dt) return (T)(object)dt;
                    if (v is string sdt && DateTime.TryParse(sdt, Inv, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dtp))
                        return (T)(object)dtp;
                }

                if (target == typeof(Guid?) || target == typeof(Guid))
                {
                    if (v is Guid g) return (T)(object)g;
                    if (v is string sg && Guid.TryParse(sg, out var gp)) return (T)(object)gp;
                }

                if (target == typeof(string))
                {
                    return (T)(object)(v?.ToString() ?? string.Empty);
                }
            }
            catch { /* ignore and return default */ }

            return default;
        }

        private static void SetNull(IDictionary<string, object?> row, string name)
            => row[name] = null!;

        // -------------------- INT as string (для Select) --------------------
        protected string GetIntAsString(IDictionary<string, object?> row, DXColumnDefinition col)
        {
            var n = GetIntN(row, col);
            return n.HasValue ? n.Value.ToString(Inv) : string.Empty;
        }

        //protected string GetGuidAsString(IDictionary<string, object?> row, DXColumnDefinition col)
        //{
        //    var n = GetGuidString(row, col);
        //    return n.HasValue ? n.Value.ToString(Inv) : string.Empty;
        //}

        protected void SetIntFromString(IDictionary<string, object?> row, DXColumnDefinition col, string v)
        {
            if (string.IsNullOrEmpty(v))
            {
                if (col.AllowNull) { SetNull(row, col.Name); return; }
                if (!TryApplyDefault(row, col)) row[col.Name] = 0;
                return;
            }

            if (int.TryParse(v, NumberStyles.Integer, Inv, out var i)) { row[col.Name] = i; return; }

            if (!col.AllowNull)
            {
                if (!TryApplyDefault(row, col)) row[col.Name] = 0;
            }
            else
            {
                SetNull(row, col.Name);
            }
        }

        protected void SetGuidFromString(IDictionary<string, object?> row, DXColumnDefinition col, string v)
        {
            if (string.IsNullOrEmpty(v))
            {
                if (col.AllowNull) { SetNull(row, col.Name); return; }
                // Need to throw error?
                //if (!TryApplyDefault(row, col)) row[col.Name] = 0;
                return;
            }

            if (Guid.TryParse(v, out var i)) { row[col.Name] = i; return; }

            if (!col.AllowNull)
            {
                // Need to throw error?
                //if (!TryApplyDefault(row, col)) row[col.Name] = 0;
            }
            else
            {
                SetNull(row, col.Name);
            }
        }

        // -------- INT? for Enum-select --------
        protected int? GetIntN(IDictionary<string, object?> row, DXColumnDefinition col)
        {
            TryGet(row, col.Name, out var v);
            return ConvertTo<int?>(v);
        }

        // -------- BOOL --------
        protected bool GetBool(IDictionary<string, object?> row, DXColumnDefinition col)
        {
            TryGet(row, col.Name, out var v);
            return ConvertTo<bool?>(v) ?? false;
        }

        protected void SetBool(IDictionary<string, object?> row, DXColumnDefinition col, bool v)
            => row[col.Name] = v;

        // -------- STRING (nullable) --------
        protected string? GetStringN(IDictionary<string, object?> row, DXColumnDefinition col)
        {
            TryGet(row, col.Name, out var v);
            return ConvertTo<string>(v);
        }

        protected void SetStringN(IDictionary<string, object?> row, DXColumnDefinition col, string? v)
        {
            if (string.IsNullOrWhiteSpace(v))
            {
                if (!col.AllowNull)
                {
                    if (!TryApplyDefault(row, col))
                        row[col.Name] = string.Empty; // fallback
                }
                else
                {
                    SetNull(row, col.Name);
                }
                return;
            }

            row[col.Name] = v!;
        }

        // -------- DATETIME? --------
        protected DateTime? GetDateTime(IDictionary<string, object?> row, DXColumnDefinition col)
        {
            TryGet(row, col.Name, out var v);
            return ConvertTo<DateTime?>(v);
        }

        protected void SetDateTime(IDictionary<string, object?> row, DXColumnDefinition col, DateTime? v)
            => row[col.Name] = v;

        // -------- LONG? (Short/Int/Long) --------
        protected long? GetLongN(IDictionary<string, object?> row, DXColumnDefinition col)
        {
            TryGet(row, col.Name, out var v);
            return ConvertTo<long?>(v);
        }

        protected void SetLongN(IDictionary<string, object?> row, DXColumnDefinition col, long? v)
            => row[col.Name] = v;

        // -------- DOUBLE? (Float) --------
        protected double? GetDoubleN(IDictionary<string, object?> row, DXColumnDefinition col)
        {
            TryGet(row, col.Name, out var v);
            return ConvertTo<double?>(v);
        }

        protected void SetDoubleN(IDictionary<string, object?> row, DXColumnDefinition col, double? v)
            => row[col.Name] = v;

        // -------- DECIMAL? (Decimal/Currency) --------
        protected decimal? GetDecimalN(IDictionary<string, object?> row, DXColumnDefinition col)
        {
            TryGet(row, col.Name, out var v);
            return ConvertTo<decimal?>(v);
        }

        protected void SetDecimalN(IDictionary<string, object?> row, DXColumnDefinition col, decimal? v)
            => row[col.Name] = v;

        // -------- GUID как строка --------
        protected string GetGuidString(IDictionary<string, object?> row, DXColumnDefinition col)
        {
            return GetGuid(row, col)?.ToString() ?? string.Empty;
        }

        protected Guid? GetGuid(IDictionary<string, object?> row, DXColumnDefinition col)
        {
            TryGet(row, col.Name, out var v);
            var g = ConvertTo<Guid?>(v);
            return g;
        }

        protected void SetGuidString(IDictionary<string, object?> row, DXColumnDefinition col, string value)
        {
            if (Guid.TryParse(value, out var guid))
                row[col.Name] = guid;
            else
                SetNull(row, col.Name);
        }

        // -------- BLOB --------
        protected void OpenBlobDialog(IDictionary<string, object?> row, DXColumnDefinition col)
        {
            // no-op placeholder
        }

        // -------- Defaults --------
        public static bool TryApplyDefault(IDictionary<string, object?> row, DXColumnDefinition col)
        {
            if (string.IsNullOrWhiteSpace(col.DefaultValue))
                return false;

            switch (col.ColumnType)
            {
                case DXColumnTypeEnum.Bool:
                    if (bool.TryParse(col.DefaultValue, out var b)) { row[col.Name] = b; return true; }
                    break;

                case DXColumnTypeEnum.String:
                case DXColumnTypeEnum.Text:
                    row[col.Name] = col.DefaultValue;
                    return true;

                case DXColumnTypeEnum.DateTime:
                    if (DateTime.TryParse(col.DefaultValue, Inv, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt))
                    { row[col.Name] = dt; return true; }
                    if (string.Equals(col.DefaultValue, "now", StringComparison.OrdinalIgnoreCase))
                    { row[col.Name] = DateTime.UtcNow; return true; }
                    break;

                case DXColumnTypeEnum.Short:
                case DXColumnTypeEnum.Int:
                case DXColumnTypeEnum.Long:
                case DXColumnTypeEnum.Float:
                case DXColumnTypeEnum.Decimal:
                case DXColumnTypeEnum.Currency:
                    if (decimal.TryParse(col.DefaultValue, NumberStyles.Number, Inv, out var dec))
                    {
                        switch (col.ColumnType)
                        {
                            case DXColumnTypeEnum.Short: row[col.Name] = (short)dec; return true;
                            case DXColumnTypeEnum.Int: row[col.Name] = (int)dec; return true;
                            case DXColumnTypeEnum.Long: row[col.Name] = (long)dec; return true;
                            case DXColumnTypeEnum.Float: row[col.Name] = (double)dec; return true;
                            case DXColumnTypeEnum.Decimal:
                            case DXColumnTypeEnum.Currency: row[col.Name] = dec; return true;
                        }
                    }
                    break;

                case DXColumnTypeEnum.GUID:
                    if (Guid.TryParse(col.DefaultValue, out var g)) { row[col.Name] = g; return true; }
                    if (string.Equals(col.DefaultValue, "new", StringComparison.OrdinalIgnoreCase)) { row[col.Name] = Guid.NewGuid(); return true; }
                    break;

                case DXColumnTypeEnum.Blob:
                    if (!string.IsNullOrEmpty(col.DefaultValue)) { row[col.Name] = col.DefaultValue; return true; }
                    break;
            }

            // Enum-Int default
            if (col.ColumnType == DXColumnTypeEnum.Int && col.EnumValues != null && col.EnumValues.Count > 0)
            {
                if (int.TryParse(col.DefaultValue, NumberStyles.Integer, Inv, out var enumKey) && col.EnumValues.ContainsKey(enumKey))
                {
                    row[col.Name] = enumKey;
                    return true;
                }

                var kv = col.EnumValues.FirstOrDefault(k =>
                    string.Equals(k.Value, col.DefaultValue, StringComparison.OrdinalIgnoreCase));
                if (!kv.Equals(default(KeyValuePair<int, string>)))
                {
                    row[col.Name] = kv.Key;
                    return true;
                }
            }

            return false;
        }

        public static void ApplyNonNullableFallback(IDictionary<string, object?> row, DXColumnDefinition col)
        {
            switch (col.ColumnType)
            {
                case DXColumnTypeEnum.Bool: row[col.Name] = false; break;
                case DXColumnTypeEnum.String:
                case DXColumnTypeEnum.Text: row[col.Name] = string.Empty; break;
                case DXColumnTypeEnum.DateTime: row[col.Name] = DateTime.UtcNow; break;
                case DXColumnTypeEnum.Short: row[col.Name] = (short)0; break;
                case DXColumnTypeEnum.Int: row[col.Name] = 0; break;
                case DXColumnTypeEnum.Long: row[col.Name] = 0L; break;
                case DXColumnTypeEnum.Float: row[col.Name] = 0.0; break;
                case DXColumnTypeEnum.Decimal:
                case DXColumnTypeEnum.Currency: row[col.Name] = 0m; break;
                case DXColumnTypeEnum.GUID: row[col.Name] = Guid.NewGuid(); break;
                case DXColumnTypeEnum.Blob: row[col.Name] = null!; break;
            }
        }

        protected async Task SetBlobAsync(IDictionary<string, object?> row, DXColumnDefinition col, InputFileChangeEventArgs e)
        {
            var file = e.File;
            const long maxAllowedSize = 50 * 1024 * 1024;

            using var s = file.OpenReadStream(maxAllowedSize);
            using var ms = new MemoryStream();
            await s.CopyToAsync(ms);

            var raw = ms.ToArray();

            var packed = DXBlobContainer.Pack(
                data: raw,
                meta: new DXBlobContainer.Meta
                {
                    Mime = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                    FileName = file.Name
                },
                includeSha256: true,
                includeCreatedUtc: true);

            row[col.Name] = packed; // <-- одно свойство, byte[]
        }

        protected void ClearBlob(IDictionary<string, object?> row, DXColumnDefinition col)
        {
            if (col.AllowNull) row[col.Name] = null!;
            else row[col.Name] = Array.Empty<byte>();
        }

        protected bool HasBlob(IDictionary<string, object?> row, DXColumnDefinition col)
            => TryGetBlobPacked(row, col, out var packed) && packed.Length > 0;

        protected bool TryGetBlobMeta(IDictionary<string, object?> row, DXColumnDefinition col,
                                      out string fileName, out string mime, out long sizeBytes)
        {
            fileName = "";
            mime = "application/octet-stream";
            sizeBytes = 0;

            if (!TryGetBlobPacked(row, col, out var packed) || packed.Length == 0)
                return false;

            if (DXBlobContainer.TryUnpack(packed, out var meta, out var data))
            {
                fileName = meta.FileName ?? "";
                mime = string.IsNullOrWhiteSpace(meta.Mime) ? "application/octet-stream" : meta.Mime;
                sizeBytes = data.LongLength;
                return true;
            }

            // fallback: raw bytes without container
            sizeBytes = packed.LongLength;
            return true;
        }

        protected bool TryGetImagePreviewDataUrl(IDictionary<string, object?> row, DXColumnDefinition col, out string dataUrl)
        {
            dataUrl = "";

            if (!TryGetBlobPacked(row, col, out var packed) || packed.Length == 0)
                return false;

            string mime;
            byte[] data;

            if (DXBlobContainer.TryUnpack(packed, out var meta, out var raw))
            {
                mime = string.IsNullOrWhiteSpace(meta.Mime) ? "application/octet-stream" : meta.Mime;
                data = raw;
            }
            else
            {
                // raw bytes without mime => preview не делаем
                return false;
            }

            if (!mime.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                return false;

            dataUrl = $"data:{mime};base64,{Convert.ToBase64String(data)}";
            return true;
        }

        /// <summary>
        /// Достаёт сырой файл (без хедера), удобно для записи в bytea/varbinary или для отдачи наружу.
        /// </summary>
        protected byte[]? GetBlobRawBytes(IDictionary<string, object?> row, DXColumnDefinition col)
        {
            if (!TryGetBlobPacked(row, col, out var packed) || packed.Length == 0)
                return null;

            if (DXBlobContainer.TryUnpack(packed, out _, out var data))
                return data;

            return packed; // fallback: raw bytes
        }

        private static bool TryGetBlobPacked(IDictionary<string, object?> row, DXColumnDefinition col, out byte[] packed)
        {
            packed = Array.Empty<byte>();
            if (row is null) return false;
            if (!row.TryGetValue(col.Name, out var v) || v is null) return false;

            // уже bytes
            if (v is byte[] b)
            {
                packed = b;
                return packed.Length > 0;
            }

            // Newtonsoft JValue / JToken string
            if (v is Newtonsoft.Json.Linq.JValue jv) v = jv.Value!;
            if (v is Newtonsoft.Json.Linq.JToken jt && jt.Type == Newtonsoft.Json.Linq.JTokenType.String) v = jt.ToString();

            // base64 string
            if (v is string s)
            {
                if (string.IsNullOrWhiteSpace(s)) return false;

                // если вдруг data-url
                var comma = s.IndexOf(',');
                if (s.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma >= 0)
                    s = s[(comma + 1)..];

                packed = Convert.FromBase64String(s);
                return packed.Length > 0;
            }

            return false;
        }
    }
}

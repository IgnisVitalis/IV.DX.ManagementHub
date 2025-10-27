using IV.DX.Kernel.Enums;
using IV.ManagementHub.Common.Models;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json.Linq;

namespace IV.ManagementHub.Web.Components.Custom
{
    public abstract class DXItemFluentDataDialog : ComponentBase
    {
        private readonly string[] systemColumns = new[] { "ID", "DXUnitID", "TimeStamp" };

        protected IDictionary<string, object> GetRequiredAttr(DXColumnDefinition col)
            => col.AllowNull ? new Dictionary<string, object>()
                             : new Dictionary<string, object> { ["required"] = true };


        protected string GetIntAsString(JObject row, DXColumnDefinition col)
        {
            var n = GetIntN(row, col);
            return n.HasValue ? n.Value.ToString() : string.Empty;
        }

        protected void SetIntFromString(JObject row, DXColumnDefinition col, string v)
        {
            if (string.IsNullOrEmpty(v))
            {
                if (col.AllowNull) { row[col.Name] = JValue.CreateNull(); return; }
                if (!TryApplyDefault(row, col)) row[col.Name] = 0;
                return;
            }

            if (int.TryParse(v, out var i)) { row[col.Name] = i; return; }

            if (!col.AllowNull)
            {
                if (!TryApplyDefault(row, col)) row[col.Name] = 0;
            }
            else
            {
                row[col.Name] = JValue.CreateNull();
            }
        }

        // -------- INT? for Enum-select --------
        protected int? GetIntN(JObject row, DXColumnDefinition col)
        {
            var t = row[col.Name];
            if (t == null || t.Type == JTokenType.Null || t.Type == JTokenType.Undefined) return null;
            if (t.Type == JTokenType.Integer) return t.Value<int?>();
            return int.TryParse(t.ToString(), out var parsed) ? parsed : (int?)null;
        }

        // -------- BOOL --------
        protected bool GetBool(JObject row, DXColumnDefinition col)
            => row.Value<bool?>(col.Name) ?? false;

        protected void SetBool(JObject row, DXColumnDefinition col, bool v)
            => row[col.Name] = v;

        // -------- STRING (nullable) --------
        protected string? GetStringN(JObject row, DXColumnDefinition col)
            => row.Value<string?>(col.Name);

        protected void SetStringN(JObject row, DXColumnDefinition col, string? v)
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
                    row[col.Name] = JValue.CreateNull();
                }
                return;
            }

            row[col.Name] = v;
        }

        // -------- DATETIME? --------
        protected DateTime? GetDateTime(JObject row, DXColumnDefinition col)
            => row.Value<DateTime?>(col.Name);

        protected void SetDateTime(JObject row, DXColumnDefinition col, DateTime? v)
            => row[col.Name] = v.HasValue ? JValue.FromObject(v.Value) : JValue.CreateNull();

        // -------- LONG? (for Short/Int/Long) --------
        protected long? GetLongN(JObject row, DXColumnDefinition col)
        {
            var t = row[col.Name];
            if (t == null || t.Type == JTokenType.Null || t.Type == JTokenType.Undefined) return null;
            if (t.Type == JTokenType.Integer) return t.Value<long?>();
            return long.TryParse(t.ToString(), out var parsed) ? parsed : null;
        }

        protected void SetLongN(JObject row, DXColumnDefinition col, long? v)
            => row[col.Name] = v.HasValue ? JValue.FromObject(v.Value) : JValue.CreateNull();

        // -------- DOUBLE? (for Float) --------
        protected double? GetDoubleN(JObject row, DXColumnDefinition col)
        {
            var t = row[col.Name];
            if (t == null || t.Type == JTokenType.Null || t.Type == JTokenType.Undefined) return null;
            if (t.Type is JTokenType.Integer or JTokenType.Float) return t.Value<double?>();
            return double.TryParse(t.ToString(), out var parsed) ? parsed : (double?)null;
        }

        protected void SetDoubleN(JObject row, DXColumnDefinition col, double? v)
            => row[col.Name] = v.HasValue ? JValue.FromObject(v.Value) : JValue.CreateNull();

        // -------- DECIMAL? (for Decimal/Currency) --------
        protected decimal? GetDecimalN(JObject row, DXColumnDefinition col)
        {
            var t = row[col.Name];
            if (t == null || t.Type == JTokenType.Null || t.Type == JTokenType.Undefined) return null;
            if (t.Type is JTokenType.Integer or JTokenType.Float) return t.Value<decimal?>();
            return decimal.TryParse(t.ToString(), out var parsed) ? parsed : (decimal?)null;
        }

        protected void SetDecimalN(JObject row, DXColumnDefinition col, decimal? v)
            => row[col.Name] = v.HasValue ? JValue.FromObject(v.Value) : JValue.CreateNull();

        // -------- GUID как строка --------
        protected string GetGuidString(JObject row, DXColumnDefinition col)
        {
            var g = row.Value<Guid?>(col.Name);
            return g?.ToString() ?? string.Empty;
        }

        protected void SetGuidString(JObject row, DXColumnDefinition col, string value)
        {
            if (Guid.TryParse(value, out var guid))
                row[col.Name] = guid;
            else
                row[col.Name] = JValue.CreateNull();
        }

        // -------- BLOB --------
        protected void OpenBlobDialog(JObject row, DXColumnDefinition col)
        {

        }

        protected bool TryApplyDefault(JObject row, DXColumnDefinition col)
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
                    if (DateTime.TryParse(col.DefaultValue, out var dt)) { row[col.Name] = dt; return true; }
                    if (string.Equals(col.DefaultValue, "now", StringComparison.OrdinalIgnoreCase)) { row[col.Name] = DateTime.UtcNow; return true; }
                    break;

                case DXColumnTypeEnum.Short:
                case DXColumnTypeEnum.Int:
                case DXColumnTypeEnum.Long:
                case DXColumnTypeEnum.Float:
                case DXColumnTypeEnum.Decimal:
                case DXColumnTypeEnum.Currency:
                    if (decimal.TryParse(col.DefaultValue, out var dec))
                    {
                        // пишем типосообразно
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

            if (col.ColumnType == DXColumnTypeEnum.Int && col.EnumValues != null && col.EnumValues.Count > 0)
            {
                if (int.TryParse(col.DefaultValue, out var enumKey) && col.EnumValues.ContainsKey(enumKey))
                {
                    row[col.Name] = enumKey;
                    return true;
                }

                var kv = col.EnumValues.FirstOrDefault(k => string.Equals(k.Value, col.DefaultValue, StringComparison.OrdinalIgnoreCase));
                if (!kv.Equals(default(KeyValuePair<int, string>)))
                {
                    row[col.Name] = kv.Key;
                    return true;
                }
            }

            return false;
        }

        protected void ApplyNonNullableFallback(JObject row, DXColumnDefinition col)
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
                case DXColumnTypeEnum.Blob: row[col.Name] = JValue.CreateNull(); break;
            }
        }
    }
}

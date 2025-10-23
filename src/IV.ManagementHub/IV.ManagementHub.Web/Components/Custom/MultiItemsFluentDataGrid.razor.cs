using IV.ManagementHub.Common.Models;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json.Linq;

namespace IV.ManagementHub.Web.Components.Custom
{
    public partial class MultiItemsFluentDataGrid : ComponentBase
    {
        [Parameter, EditorRequired] public DXElementDefinitionStructure Definition { get; set; } = default!;
        [Parameter, EditorRequired] public JArray Items { get; set; } = default!;

        // Материализуем в List<JObject>, чтобы не споткнуться о JEnumerable в expression trees грида
        protected List<JObject> RowObjects { get; private set; } = new();

        protected override void OnParametersSet()
        {
            RowObjects = Items?.OfType<JObject>().ToList() ?? new List<JObject>();
        }

        private void Add()
        {
            //Content.DXColumnDefinitionElement.AddToAnnounced(new DXColumnDefinitionElement()
            //{
            //    ID = Guid.NewGuid(),
            //    ColumnType = DXColumnTypeEnum.String
            //});
        }

        // -------- BOOL --------
        bool GetBool(JObject row, DXColumnDefinitionStructure col)
            => row.Value<bool?>(col.Name) ?? false;

        void SetBool(JObject row, DXColumnDefinitionStructure col, bool v)
            => row[col.Name] = v;

        // -------- STRING (nullable) --------
        string? GetStringN(JObject row, DXColumnDefinitionStructure col)
            => row.Value<string?>(col.Name);

        void SetStringN(JObject row, DXColumnDefinitionStructure col, string? v)
            => row[col.Name] = string.IsNullOrWhiteSpace(v) ? JValue.CreateNull() : JValue.FromObject(v);

        // -------- DATETIME? --------
        DateTime? GetDateTime(JObject row, DXColumnDefinitionStructure col)
            => row.Value<DateTime?>(col.Name);

        void SetDateTime(JObject row, DXColumnDefinitionStructure col, DateTime? v)
            => row[col.Name] = v.HasValue ? JValue.FromObject(v.Value) : JValue.CreateNull();

        // -------- LONG? (для Short/Int/Long) --------
        long? GetLongN(JObject row, DXColumnDefinitionStructure col)
        {
            var t = row[col.Name];
            if (t == null || t.Type == JTokenType.Null || t.Type == JTokenType.Undefined) return null;
            if (t.Type == JTokenType.Integer) return t.Value<long?>();
            return long.TryParse(t.ToString(), out var parsed) ? parsed : null;
        }

        void SetLongN(JObject row, DXColumnDefinitionStructure col, long? v)
            => row[col.Name] = v.HasValue ? JValue.FromObject(v.Value) : JValue.CreateNull();

        // -------- DOUBLE? (для Float) --------
        double? GetDoubleN(JObject row, DXColumnDefinitionStructure col)
        {
            var t = row[col.Name];
            if (t == null || t.Type == JTokenType.Null || t.Type == JTokenType.Undefined) return null;
            if (t.Type is JTokenType.Integer or JTokenType.Float) return t.Value<double?>();
            return double.TryParse(t.ToString(), out var parsed) ? parsed : (double?)null;
        }

        void SetDoubleN(JObject row, DXColumnDefinitionStructure col, double? v)
            => row[col.Name] = v.HasValue ? JValue.FromObject(v.Value) : JValue.CreateNull();

        // -------- DECIMAL? (для Decimal/Currency) --------
        decimal? GetDecimalN(JObject row, DXColumnDefinitionStructure col)
        {
            var t = row[col.Name];
            if (t == null || t.Type == JTokenType.Null || t.Type == JTokenType.Undefined) return null;
            if (t.Type is JTokenType.Integer or JTokenType.Float) return t.Value<decimal?>();
            return decimal.TryParse(t.ToString(), out var parsed) ? parsed : (decimal?)null;
        }

        void SetDecimalN(JObject row, DXColumnDefinitionStructure col, decimal? v)
            => row[col.Name] = v.HasValue ? JValue.FromObject(v.Value) : JValue.CreateNull();

        // -------- GUID как строка --------
        string GetGuidString(JObject row, DXColumnDefinitionStructure col)
        {
            var g = row.Value<Guid?>(col.Name);
            return g?.ToString() ?? string.Empty;
        }

        void SetGuidString(JObject row, DXColumnDefinitionStructure col, string value)
        {
            if (Guid.TryParse(value, out var guid))
                row[col.Name] = guid;
            else
                row[col.Name] = JValue.CreateNull();
        }

        // -------- BLOB заглушка --------
        void OpenBlobDialog(JObject row, DXColumnDefinitionStructure col)
        {
            // TODO: открыть диалог/панель для загрузки/просмотра blob;
            // хранить можно как base64-строку или массив байт (JArray из byte/int).
        }
    }
}

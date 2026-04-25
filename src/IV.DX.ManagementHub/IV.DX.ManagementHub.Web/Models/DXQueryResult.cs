using IV.DX.Kernel.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;

namespace IV.DX.ManagementHub.Web.Models
{
    internal class DXQueryResult
    {
        public string TypeName { get; }
        public IReadOnlyList<QueryDefinition> QueryDefinition { get; }
        public IReadOnlyList<JObject> Content { get; }

        private IEnumerable<string> _columns;

        public IEnumerable<string> Columns
        {
            get
            {
                if (this._columns == null)
                {
                    this._columns = this.QueryDefinition.OrderBy(x => x.Order).Where(x => x.Name != "Id").Select(x => x.Name);
                }

                return this._columns;
            }
        }

        public IQueryable<JObject> AsQueryable()
        {
            return this.Content.AsQueryable();
        }

        public object? GetValue(JObject jObject, string columnName)
        {
            return jObject[columnName];
        }


        public Guid GetId(JObject jObject)
        {
            if (jObject == null)
                throw new ArgumentNullException(nameof(jObject));

            if (!jObject.ContainsKey("Id"))
                throw new ArgumentException($"Property 'Id' not found.", nameof(jObject));

            var token = jObject["Id"];
            if (token == null || token.Type == JTokenType.Null)
                throw new ArgumentException("Property 'Id' is null.", nameof(jObject));

            Guid id;
            if (token.Type == JTokenType.Guid)
            {
                id = token.Value<Guid>();
            }
            else if (token.Type == JTokenType.String)
            {
                var value = token.Value<string>();
                if (!Guid.TryParse(value, out id))
                    throw new ArgumentException($"Property 'Id' {value} couldn't be parsed.", nameof(jObject));
            }
            else
            {
                // Fallback for numeric/other types that can be converted to string
                var value = token.ToString();
                if (!Guid.TryParse(value, out id))
                    throw new ArgumentException($"Property 'Id' {value} couldn't be parsed.", nameof(jObject));
            }

            if (id == default(Guid))
                throw new ArgumentException($"Property 'Id' has default value.", nameof(jObject));

            return id;
        }

        private DXQueryResult(
            string typeName,
            IReadOnlyList<QueryDefinition> queryDefinition,
            IReadOnlyList<JObject> content)
        {
            this.TypeName = typeName;
            this.QueryDefinition = queryDefinition;
            this.Content = content;
        }

        public static DXQueryResult Empty(string? typeName = default)
        {
            return new DXQueryResult(typeName ?? string.Empty, Array.Empty<QueryDefinition>(), Array.Empty<JObject>());
        }

        public static DXQueryResult Parse(string str)
        {
            if (string.IsNullOrWhiteSpace(str))
                throw new ArgumentException("DXQueryResult string is null or empty.", nameof(str));

            var jObject = JObject.Parse(str);

            var typeName = jObject.SelectToken("Content.Meta.Type")!.Value<string>();

            var dataDefToken = jObject["QueryDefinition"] as JArray
                               ?? throw new JsonException("Property 'QueryDefinition' is missing or not an array.");

            var dataDefinition = dataDefToken.ToObject<List<QueryDefinition>>()
                               ?? new List<QueryDefinition>();

            var content = ParseContent(jObject["Content"]);

            return new DXQueryResult(typeName, dataDefinition, content);
        }

        private static IReadOnlyList<JObject> ParseContent(JToken? token)
        {
            if (token is JArray array)
            {
                return array.OfType<JObject>().ToList();
            }

            if (token is JObject obj)
            {
                var block = obj.ToObject<DXDataBlock<DXUnitRecord>>();
                if (block?.Data?.Items == null)
                    return Array.Empty<JObject>();

                return block.Data.Items
                    .Select(ToRowObject)
                    .ToList();
            }

            throw new JsonException("Property 'Content' is missing or has unsupported format.");
        }

        private static JObject ToRowObject(DXUnitRecord record)
        {
            var row = new JObject
            {
                ["Id"] = JToken.FromObject(record.Id),
                ["TimeStamp"] = JToken.FromObject(record.TimeStamp)
            };

            if (record.Fields != null)
            {
                foreach (var kvp in record.Fields)
                {
                    row[kvp.Key] = kvp.Value ?? JValue.CreateNull();
                }
            }

            return row;
        }

        public DataTable AsDataTable()
        {
            DataTable dataTable = new DataTable(this.TypeName);

            foreach (var item in QueryDefinition.OrderBy(x => x.Order))
            {
                dataTable.Columns.Add(item.Name);
            }

            foreach (var item in Content)
            {
                DataRow dataRow = dataTable.NewRow();

                foreach (var column in dataTable.Columns.Cast<DataColumn>())
                {
                    var token = item.SelectToken(column.ColumnName);

                    dataRow[column] = token is JValue v ? v.Value ?? DBNull.Value
                                   : token is null ? DBNull.Value
                                   : token.ToString();
                }

                dataTable.Rows.Add(dataRow);
            }

            return dataTable;
        }
    }

    internal class QueryDefinition
    {
        public string Name { get; set; } = default!;
        public int Order { get; set; }
        public string? Expression { get; set; }
    }
}


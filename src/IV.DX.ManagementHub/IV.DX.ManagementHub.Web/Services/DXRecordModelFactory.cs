using IV.DX.Kernel.Models;
using IV.DX.ManagementHub.Common.Models;
using IV.DX.ManagementHub.Web.Models;
using Newtonsoft.Json.Linq;

namespace IV.DX.ManagementHub.Web.Services
{
    internal static class DXRecordModelFactory
    {
        private static readonly HashSet<string> SystemColumns = new(StringComparer.OrdinalIgnoreCase)
        {
            "ID",
            "DXUnitID",
            "TimeStamp"
        };

        public static DXUnitRecordModel GetDefault(DXModelDefinition definition)
        {
            var unitId = Guid.NewGuid();
            var timeStamp = DateTime.UtcNow;

            var mainContent = BuildContent(definition.MainSingleElement);
            var mainItem = new DXRecordItem(definition.Name, unitId, unitId, timeStamp, mainContent);

            var singleElements = new Dictionary<string, DXRecordSingleElement>(StringComparer.OrdinalIgnoreCase);
            var multiElements = new Dictionary<string, DXRecordMultiElement>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in definition.RequiredSingleElements)
            {
                var elementItem = BuildElementItem(item, unitId, timeStamp);
                singleElements[item.Name] = new DXRecordSingleElement(item.Name, elementItem);
            }

            foreach (var item in definition.OptionalSingleElements)
            {
                singleElements[item.Name] = new DXRecordSingleElement(item.Name, null);
            }

            foreach (var item in definition.RequiredMultiElements)
            {
                var elementItem = BuildElementItem(item, unitId, timeStamp);
                multiElements[item.Name] = new DXRecordMultiElement(item.Name, new[] { elementItem }, trackOriginal: false);
            }

            foreach (var item in definition.OptionalMultiElements)
            {
                multiElements[item.Name] = new DXRecordMultiElement(item.Name, trackOriginal: false);
            }

            return new DXUnitRecordModel(definition.Name, mainItem, singleElements, multiElements);
        }

        public static DXUnitRecordModel FromBlock(DXDataBlock<DXUnitRecord> block, DXModelDefinition definition)
        {
            var record = block?.Data?.Items?.FirstOrDefault();
            if (record == null)
            {
                return GetDefault(definition);
            }

            var typeName = block.Meta?.Type ?? definition.Name;
            var unitId = record.ID;
            var timeStamp = record.TimeStamp;

            var mainContent = ToObjectDictionary(record.Fields);
            var singleElements = new Dictionary<string, DXRecordSingleElement>(StringComparer.OrdinalIgnoreCase);
            var multiElements = new Dictionary<string, DXRecordMultiElement>(StringComparer.OrdinalIgnoreCase);

            if (record.DXElements != null)
            {
                foreach (var kvp in record.DXElements)
                {
                    var elementBlock = kvp.Value;
                    if (elementBlock == null) continue;

                    var elementName = elementBlock.Meta?.Type ?? kvp.Key;
                    var announced = ParseRecordItems(elementBlock.Data?.Items, elementName, unitId, timeStamp);
                    var deleted = ParseDeleteItems(elementBlock.Data?.Delete, elementName, unitId, timeStamp);

                    AddElement(definition, singleElements, multiElements, elementName, announced, deleted);
                }
            }

            EnsureRequired(definition, unitId, timeStamp, singleElements, multiElements);

            var mainItem = new DXRecordItem(typeName, unitId, unitId, timeStamp, mainContent);
            return new DXUnitRecordModel(typeName, mainItem, singleElements, multiElements);
        }

        public static DXDataBlock<DXUnitRecord> ToBlock(DXUnitRecordModel model, DXModelDefinition definition)
        {
            var record = new DXUnitRecord
            {
                ID = model.MainItem.ID,
                TimeStamp = model.MainItem.TimeStamp,
                Fields = ToJTokenDictionary(model.MainItem.Content)
            };

            var elements = new Dictionary<string, DXDataBlock<DXElementRecord>>(StringComparer.OrdinalIgnoreCase);

            foreach (var single in model.SingleElements.Values)
            {
                if (single.Item == null)
                    continue;

                var elementRecord = ToElementRecord(single.Item);
                elements[single.Name] = new DXDataBlock<DXElementRecord>
                {
                    Meta = new DXMeta
                    {
                        Kind = "DXElement",
                        Type = single.Name,
                        Op = "Patch",
                        IsMulti = false,
                        IsRequired = definition.IsRequired(single.Name)
                    },
                    Data = new DXData<DXElementRecord>
                    {
                        Items = new List<DXElementRecord> { elementRecord }
                    }
                };
            }

            foreach (var multi in model.MultiElements.Values)
            {
                var announced = FilterSystemColumnDefinitions(multi.Name, multi.GetItems());
                var deleted = FilterSystemColumnDefinitions(multi.Name, multi.Deleted);

                var upserts = announced.Select(ToElementRecord).ToList();
                var deletes = deleted.Select(ToDeleteRef).ToList();

                if (upserts.Count == 0 && deletes.Count == 0)
                    continue;

                elements[multi.Name] = new DXDataBlock<DXElementRecord>
                {
                    Meta = new DXMeta
                    {
                        Kind = "DXElement",
                        Type = multi.Name,
                        Op = "Patch",
                        IsMulti = true,
                        IsRequired = definition.IsRequired(multi.Name)
                    },
                    Data = new DXData<DXElementRecord>
                    {
                        Items = upserts.Count == 0 ? null : upserts,
                        Delete = deletes.Count == 0 ? null : deletes
                    }
                };
            }

            record.DXElements = elements.Count == 0 ? null : elements;

            return new DXDataBlock<DXUnitRecord>
            {
                Meta = new DXMeta
                {
                    Kind = "DXUnit",
                    Type = model.Type,
                    Op = "Patch",
                    IsMulti = true,
                    IsRequired = false
                },
                Data = new DXData<DXUnitRecord>
                {
                    Items = new List<DXUnitRecord> { record }
                }
            };
        }

        private static IEnumerable<DXRecordItem> FilterSystemColumnDefinitions(string elementName, IEnumerable<DXRecordItem> items)
        {
            if (!elementName.Equals("DXColumnDefinitionElement", StringComparison.OrdinalIgnoreCase))
                return items;

            return items.Where(item => !IsSystemColumnDefinition(item));
        }

        private static bool IsSystemColumnDefinition(DXRecordItem item)
        {
            if (item.Content == null)
                return false;

            if (!item.Content.TryGetValue("Name", out var raw) || raw == null)
                return false;

            var name = raw.ToString();
            if (string.IsNullOrWhiteSpace(name))
                return false;

            return SystemColumns.Contains(name);
        }

        private static void AddElement(
            DXModelDefinition definition,
            IDictionary<string, DXRecordSingleElement> singleElements,
            IDictionary<string, DXRecordMultiElement> multiElements,
            string elementName,
            List<DXRecordItem> announced,
            List<DXRecordItem> deleted)
        {
            if (IsSingle(definition, elementName))
            {
                var item = announced.FirstOrDefault();
                singleElements[elementName] = new DXRecordSingleElement(elementName, item);
            }
            else
            {
                multiElements[elementName] = new DXRecordMultiElement(elementName, announced, deleted);
            }
        }

        private static bool IsSingle(DXModelDefinition definition, string elementName)
        {
            return definition.RequiredSingleElements.Any(x => x.Name.Equals(elementName, StringComparison.OrdinalIgnoreCase))
                || definition.OptionalSingleElements.Any(x => x.Name.Equals(elementName, StringComparison.OrdinalIgnoreCase));
        }

        private static void EnsureRequired(
            DXModelDefinition definition,
            Guid unitId,
            DateTime timeStamp,
            IDictionary<string, DXRecordSingleElement> singleElements,
            IDictionary<string, DXRecordMultiElement> multiElements)
        {
            foreach (var item in definition.RequiredSingleElements)
            {
                if (!singleElements.ContainsKey(item.Name))
                {
                    singleElements[item.Name] = new DXRecordSingleElement(item.Name, BuildElementItem(item, unitId, timeStamp));
                }
            }

            foreach (var item in definition.RequiredMultiElements)
            {
                if (!multiElements.ContainsKey(item.Name))
                {
                    var elementItem = BuildElementItem(item, unitId, timeStamp);
                    multiElements[item.Name] = new DXRecordMultiElement(item.Name, new[] { elementItem }, trackOriginal: false);
                }
            }

            foreach (var item in definition.OptionalSingleElements)
            {
                if (!singleElements.ContainsKey(item.Name))
                {
                    singleElements[item.Name] = new DXRecordSingleElement(item.Name, null);
                }
            }

            foreach (var item in definition.OptionalMultiElements)
            {
                if (!multiElements.ContainsKey(item.Name))
                {
                    multiElements[item.Name] = new DXRecordMultiElement(item.Name, trackOriginal: false);
                }
            }
        }

        private static List<DXRecordItem> ParseRecordItems(IList<DXElementRecord>? records, string elementName, Guid unitId, DateTime timeStamp)
        {
            if (records == null || records.Count == 0)
                return new List<DXRecordItem>();

            return records.Select(r =>
            {
                var content = ToObjectDictionary(r.Fields);
                var dxUnitId = r.DXUnitID == Guid.Empty ? unitId : r.DXUnitID;
                return new DXRecordItem(elementName, r.ID, dxUnitId, r.TimeStamp == default ? timeStamp : r.TimeStamp, content);
            }).ToList();
        }

        private static List<DXRecordItem> ParseDeleteItems(IList<DXDeleteRef>? refs, string elementName, Guid unitId, DateTime timeStamp)
        {
            if (refs == null || refs.Count == 0)
                return new List<DXRecordItem>();

            return refs.Select(r =>
            {
                var content = ToObjectDictionary(r.Fields);
                var dxUnitId = unitId;
                if (r.Fields != null && r.Fields.TryGetValue("DXUnitID", out var token) && token != null)
                {
                    var parsed = token.ToObject<Guid?>();
                    if (parsed.HasValue) dxUnitId = parsed.Value;
                }

                return new DXRecordItem(elementName, r.ID, dxUnitId, timeStamp, content);
            }).ToList();
        }

        private static DXElementRecord ToElementRecord(DXRecordItem item)
        {
            return new DXElementRecord
            {
                ID = item.ID,
                TimeStamp = item.TimeStamp,
                DXUnitID = item.DXUnitID,
                Fields = ToJTokenDictionary(item.Content)
            };
        }

        private static DXDeleteRef ToDeleteRef(DXRecordItem item)
        {
            var fields = new Dictionary<string, JToken>(StringComparer.OrdinalIgnoreCase)
            {
                ["DXUnitID"] = JToken.FromObject(item.DXUnitID)
            };

            return new DXDeleteRef
            {
                ID = item.ID,
                Fields = fields
            };
        }

        private static IDictionary<string, object?> ToObjectDictionary(IDictionary<string, JToken>? fields)
        {
            var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            if (fields == null)
                return result;

            foreach (var kvp in fields)
            {
                result[kvp.Key] = TokenToValue(kvp.Value);
            }

            return result;
        }

        private static IDictionary<string, JToken> ToJTokenDictionary(IDictionary<string, object?> fields)
        {
            var result = new Dictionary<string, JToken>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in fields)
            {
                if (SystemColumns.Contains(kvp.Key))
                    continue;

                result[kvp.Key] = kvp.Value == null ? JValue.CreateNull() : JToken.FromObject(kvp.Value);
            }

            return result;
        }

        private static object? TokenToValue(JToken token)
        {
            if (token.Type == JTokenType.Null)
                return null;

            if (token is JValue jv)
                return jv.Value;

            return token.ToObject<object>();
        }

        private static DXRecordItem BuildElementItem(DXElementDefinition definition, Guid unitId, DateTime timeStamp)
        {
            var content = BuildContent(definition);
            return new DXRecordItem(definition.Name, Guid.NewGuid(), unitId, timeStamp, content);
        }

        private static Dictionary<string, object?> BuildContent(DXElementDefinition definition)
        {
            var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var column in definition.Columns)
            {
                dict[column.Name] = null;
            }

            return dict;
        }
    }
}


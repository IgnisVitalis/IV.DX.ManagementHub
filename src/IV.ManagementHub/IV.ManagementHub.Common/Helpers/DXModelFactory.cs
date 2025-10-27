using IV.DX.Kernel;
using IV.DX.Kernel.Attributes;
using IV.DX.Kernel.Models;
using IV.ManagementHub.Common.Models;
using Newtonsoft.Json.Linq;

namespace IV.ManagementHub.Common.Helpers
{
    internal static class DXModelFactory
    {

        public static DXModel Normalize(DXModel original, DXModelDefinition dxModelDefinition)
        {
            var id = original.MainElement.Item.ID.Value;
            var timeStamp = DateTime.UtcNow;

            var singleItemNamesExisting = original.DXSingleElements.Select(x => x.Name).ToList();
            var multiItemNamesExisting = original.DXMultiElements.Select(x => x.Name).ToList();

            foreach (var item in dxModelDefinition.SingleItemMandatory.Where(x => !singleItemNamesExisting.Contains(x.Name)).ToList())
            {
                var singleElement = GetNewDXSingleElement(dxModelDefinition, item, id, timeStamp, true);
                original.DXSingleElements.Add(singleElement);
            }

            foreach (var item in dxModelDefinition.SingleItemOptional.Where(x => !singleItemNamesExisting.Contains(x.Name)).ToList())
            {
                var singleElement = GetNewDXSingleElement(dxModelDefinition, item, id, timeStamp, false);
                original.DXSingleElements.Add(singleElement);
            }

            foreach (var item in dxModelDefinition.MultiItemsMandatory.Where(x => !multiItemNamesExisting.Contains(x.Name)).ToList())
            {
                var multiElement = GetNewDXMultiElement(item, id, timeStamp);
                multiElement.AddToAnnounced(GetNewDXItem(dxModelDefinition, item, id, timeStamp));

                original.DXMultiElements.Add(multiElement);
            }

            foreach (var item in dxModelDefinition.MultiItemsOptional.Where(x => !multiItemNamesExisting.Contains(x.Name)).ToList())
            {
                var multiElement = GetNewDXMultiElement(item, id, timeStamp);
                original.DXMultiElements.Add(multiElement);
            }

            return original;
        }

        public static DXModel GetDefault(DXModelDefinition dxModelDefinition)
        {
            var id = Guid.NewGuid();
            var timeStamp = DateTime.UtcNow;

            var result = new DXModel(new DXMainElement(new DXUnitAttribute(dxModelDefinition.Name))
            {
                Item = new DXItem()
                {
                    ID = id,
                    DXUnitID = id,
                    Content = new JObject()
                    {
                        new JProperty(Constants.SystemPropertyTypeName, dxModelDefinition.Name),
                        new JProperty(Constants.ID, id),
                        new JProperty(Constants.TimeStamp, timeStamp)
                    }
                }
            });

            result.DXSingleElements = new HashSet<DXSingleElement>();
            result.DXMultiElements = new HashSet<DXMultiElement>();

            foreach (var item in dxModelDefinition.SingleItemMandatory)
            {
                var singleElement = GetNewDXSingleElement(dxModelDefinition, item, id, timeStamp, true);
                result.DXSingleElements.Add(singleElement);
            }

            foreach (var item in dxModelDefinition.SingleItemOptional)
            {
                var singleElement = GetNewDXSingleElement(dxModelDefinition, item, id, timeStamp, false);
                result.DXSingleElements.Add(singleElement);
            }

            foreach (var item in dxModelDefinition.MultiItemsMandatory)
            {
                var multiElement = GetNewDXMultiElement(item, id, timeStamp);
                multiElement.AddToAnnounced(GetNewDXItem(dxModelDefinition, item, id, timeStamp));

                result.DXMultiElements.Add(multiElement);
            }

            foreach (var item in dxModelDefinition.MultiItemsOptional)
            {
                var multiElement = GetNewDXMultiElement(item, id, timeStamp);
                result.DXMultiElements.Add(multiElement);
            }

            return result;
        }

        private static DXMultiElement GetNewDXMultiElement(DXElementDefinition item, Guid dxUnitID, DateTime timeStamp)
        {
            return new DXMultiElement()
            {
                Name = item.Name,
                Mode = MultiElementsMode.Full,
                DXElementInfo = new DXElementAttribute(item.Name),
                Announced = new HashSet<DXItem>(),
                Deleted = new HashSet<DXItem>()
            };
        }

        private static DXSingleElement GetNewDXSingleElement(DXModelDefinition dxModelDefinition, DXElementDefinition item, Guid dxUnitID, DateTime timeStamp, bool initItem)
        {
            return new DXSingleElement()
            {
                Name = item.Name,
                ElementInfo = new DXElementAttribute(item.Name),
                Item = initItem ? GetNewDXItem(dxModelDefinition, item, dxUnitID, timeStamp) : GetNewEmptyDXItem(dxModelDefinition, dxUnitID, timeStamp)
            };
        }

        private static DXItem GetNewEmptyDXItem(DXModelDefinition dxModelDefinition, Guid dxUnitID, DateTime timeStamp)
        {
            var elementID = Guid.NewGuid();

            var jObject = GetDXItemDefaultContent(dxModelDefinition, elementID, dxUnitID, timeStamp);

            return new DXItem()
            {
                ID = elementID,
                DXUnitID = dxUnitID,
                Content = jObject
            };
        }


        private static DXItem GetNewDXItem(DXModelDefinition dxModelDefinition, DXElementDefinition item, Guid dxUnitID, DateTime timeStamp)
        {
            var elementID = Guid.NewGuid();

            var jObject = GetDXItemDefaultContent(dxModelDefinition, elementID, dxUnitID, timeStamp);

            foreach (var column in item.Columns)
            {
                jObject.Add(new JProperty(column.Name, null));
            }

            return new DXItem()
            {
                ID = elementID,
                DXUnitID = dxUnitID,
                Content = jObject
            };
        }

        private static JObject GetDXItemDefaultContent(DXModelDefinition dxModelDefinition, Guid elementID, Guid dxUnitID, DateTime timeStamp)
        {
            var jObject = new JObject()
                {
                    new JProperty(Constants.SystemPropertyTypeName, dxModelDefinition.Name),
                    new JProperty(Constants.ID, elementID),
                    new JProperty(Constants.DXUnitID, dxUnitID),
                    new JProperty(Constants.TimeStamp, timeStamp)
                };

            return jObject;
        }

    }
}

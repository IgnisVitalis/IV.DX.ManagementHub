using IV.DX.Kernel.Attributes;
using IV.DX.Kernel.Models;
using IV.ManagementHub.Common.Models;

namespace IV.ManagementHub.Common.Helpers
{
    internal static class DXModelFactory
    {
        public static DXModel Normalize(DXModel original, DXModelDefinition dxModelDefinition)
        {
            var id = original.DXMainElement.Item.ID;
            var timeStamp = DateTime.UtcNow;

            var singleItemNamesExisting = original.DXSingleElements.Select(x => x.Name).ToList();
            var multiItemNamesExisting = original.DXMultiElements.Select(x => x.Name).ToList();

            foreach (var item in dxModelDefinition.RequiredSingleElements.Where(x => !singleItemNamesExisting.Contains(x.Name)).ToList())
            {
                if (item.Name.Equals(original.DXMainElement.Attribute.Type))
                    continue;

                var singleElement = GetNewDXSingleElement(dxModelDefinition, item, id, timeStamp, true);
                original.DXSingleElements.Add(singleElement);
            }

            foreach (var item in dxModelDefinition.OptionalSingleElements.Where(x => !singleItemNamesExisting.Contains(x.Name)).ToList())
            {
                if (item.Name.Equals(original.DXMainElement.Attribute.Type))
                    continue;

                var singleElement = GetNewDXSingleElement(dxModelDefinition, item, id, timeStamp, false);
                original.DXSingleElements.Add(singleElement);
            }

            foreach (var item in dxModelDefinition.RequiredMultiElements.Where(x => !multiItemNamesExisting.Contains(x.Name)).ToList())
            {
                var dxItem = GetNewDXItem(dxModelDefinition, item, id, timeStamp);

                var multiElement = GetNewDXMultiElement(item, id, timeStamp, new HashSet<DXItem>() { dxItem });

                original.DXMultiElements.Add(multiElement);
            }

            foreach (var item in dxModelDefinition.OptionalMultiElements.Where(x => !multiItemNamesExisting.Contains(x.Name)).ToList())
            {
                var multiElement = GetNewDXMultiElement(item, id, timeStamp, new HashSet<DXItem>());
                original.DXMultiElements.Add(multiElement);
            }

            return original;
        }

        public static DXModel GetDefault(DXModelDefinition dxModelDefinition)
        {
            var id = Guid.NewGuid();
            var timeStamp = DateTime.UtcNow;

            var dxSingleElements = new HashSet<DXSingleElement>();
            var dxMultiElements = new HashSet<DXMultiElement>();

            foreach (var item in dxModelDefinition.RequiredSingleElements)
            {
                var singleElement = GetNewDXSingleElement(dxModelDefinition, item, id, timeStamp, true);
                dxSingleElements.Add(singleElement);
            }

            //foreach (var item in dxModelDefinition.OptionalSingleElements)
            //{
            //    var singleElement = GetNewDXSingleElement(dxModelDefinition, item, id, timeStamp, false);
            //    dxSingleElements.Add(singleElement);
            //}

            foreach (var item in dxModelDefinition.RequiredMultiElements)
            {
                var dxItem = GetNewDXItem(dxModelDefinition, item, id, timeStamp);

                var multiElement = GetNewDXMultiElement(item, id, timeStamp, new HashSet<DXItem>() { dxItem });

                dxMultiElements.Add(multiElement);
            }

            foreach (var item in dxModelDefinition.OptionalMultiElements)
            {
                var multiElement = GetNewDXMultiElement(item, id, timeStamp, new HashSet<DXItem>());
                dxMultiElements.Add(multiElement);
            }

            var dxMainDXItem = new DXItem(dxModelDefinition.Name, id, id, timeStamp, new Dictionary<string, object>());

            var result = new DXModel(
                new DXMainElement(
                    new DXUnitAttribute(dxModelDefinition.Name), 
                    dxMainDXItem), 
                dxSingleElements, 
                dxMultiElements);

            return result;
        }

        private static DXMultiElement GetNewDXMultiElement(
            DXElementDefinition item,
            Guid dxUnitID,
            DateTime timeStamp,
            HashSet<DXItem> announced)
        {
            return DXMultiElement.CreateForFullMode(item.Name, new DXElementAttribute(item.Name), announced);
        }

        private static DXSingleElement GetNewDXSingleElement(DXModelDefinition dxModelDefinition, DXElementDefinition item, Guid dxUnitID, DateTime timeStamp, bool initItem)
        {
            var dxItem = initItem
                ? GetNewDXItem(dxModelDefinition, item, dxUnitID, timeStamp)
                : GetNewEmptyDXItem(dxModelDefinition, item, dxUnitID, timeStamp);

            return new DXSingleElement(item.Name, new DXElementAttribute(item.Name), dxItem, false);
        }

        private static DXItem GetNewEmptyDXItem(
            DXModelDefinition dxModelDefinition,
            DXElementDefinition item,
            Guid dxUnitID,
            DateTime timeStamp)
        {
            var elementID = Guid.NewGuid();

            var dict = new Dictionary<string, object>();

            return new DXItem(item.Name, elementID, dxUnitID, timeStamp, dict);
        }

        private static DXItem GetNewDXItem(
            DXModelDefinition dxModelDefinition,
            DXElementDefinition item,
            Guid dxUnitID,
            DateTime timeStamp)
        {
            var elementID = Guid.NewGuid();

            var dict = new Dictionary<string, object>();

            foreach (var column in item.Columns)
            {
                dict.Add(column.Name, null);
            }

            return new DXItem(item.Name, elementID, dxUnitID, timeStamp, dict);
        }

    }
}

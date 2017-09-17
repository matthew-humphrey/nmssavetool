using System;
using System.Collections.Generic;
using System.Linq;

namespace nmssavetool
{
    public enum InventoryItemCategory
    {
        Unknown,
        Product,
        Substance,
        SuitTech,
        ShipTech,
        GunTech,
        FreighterTech,
        VehicleTech
    }

    //public sealed class InventoryItemRecord
    //{
    //    public string Id;
    //    public string Name;
    //    public InventoryItemCategory Category;
    //    public IsRec
    //}

    public class InventoryItemType
    {
        public string Id { get; private set; }
        public string Name { get; private set; }
        public InventoryItemCategory Category { get; private set; }
        public bool IsRechargeable { get; private set; }
        public int DefaultAmount { get; private set; }
        public int MaxAmount { get; private set; }

        public InventoryItemType()
        {
            Id = string.Empty;
            Name = "?";
            Category = InventoryItemCategory.Unknown;
            DefaultAmount = 0;
            MaxAmount = 0;
        }

        public InventoryItemType(string id, string name, InventoryItemCategory type, bool isRechargeable, int defaultAmount, int maxAmount)
        {
            Id = id;
            Name = name;
            Category = type;
            IsRechargeable = isRechargeable;
            DefaultAmount = defaultAmount;
            MaxAmount = maxAmount;
        }

        public override string ToString()
        {
            return string.Format("{0},{1},{2}", Id, Name, Category);
        }

        public static bool CategoryIsTech(InventoryItemCategory category)
        {
            switch (category)
            {
                case InventoryItemCategory.Unknown:
                case InventoryItemCategory.Product:
                case InventoryItemCategory.Substance:
                    return false;
                default:
                    return true;
            }
        }

    }

    public class InventoryItemTypes
    {
        private Dictionary<string, InventoryItemType> _itemTypes = new Dictionary<string, InventoryItemType>();

        public InventoryItemTypes(IEnumerable<InventoryItemType> itemTypes)
        {
            foreach(var itemType in itemTypes)
            {
                _itemTypes[itemType.Id] = itemType;
            }
        }

        private InventoryItemType UnknownFromId(string id)
        {
            return new InventoryItemType(id, "?", InventoryItemCategory.Unknown, false, 0, 0);
        }

        public InventoryItemType this[string id]
        {
            get
            {
                InventoryItemType val;
                if (!id.StartsWith("^"))
                {
                    id = "^" + id;
                }
                if (!_itemTypes.TryGetValue(id, out val))
                {
                    val = UnknownFromId(id);
                }

                return val;
            }
        }

        public IList<InventoryItemType> FindMatchingItemTypes(string pattern, InventoryItemCategory[] categories)
        {
            if (pattern.StartsWith("^"))
            {
                var matching = _itemTypes.Where(w => w.Value.Id.Equals(pattern, StringComparison.OrdinalIgnoreCase) && categories.Contains(w.Value.Category)).Select(s => s.Value).ToList();
                if (matching.Count != 0)
                {
                    return matching;
                }

                matching = _itemTypes.Where(w => w.Value.Id.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) == 0 && categories.Contains(w.Value.Category)).Select(s => s.Value).ToList();
                if (matching.Count != 0)
                {
                    return matching;
                }

                matching = _itemTypes.Where(w => w.Value.Id.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) > 0 && categories.Contains(w.Value.Category)).Select(s => s.Value).ToList();
                return matching;
            }
            else
            {
                var matching = _itemTypes.Where(w =>
                    categories.Contains(w.Value.Category) && w.Value.Name.Equals(pattern, StringComparison.OrdinalIgnoreCase)).Select(s => s.Value).ToList();

                if (matching.Count != 0)
                {
                    return matching;
                }

                matching = _itemTypes.Where(w =>
                    categories.Contains(w.Value.Category) && w.Value.Name.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) == 0).Select(s => s.Value).ToList();

                if (matching.Count != 0)
                {
                    return matching;
                }

                matching = _itemTypes.Where(w =>
                    categories.Contains(w.Value.Category) && w.Value.Name.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) > 0).Select(s => s.Value).ToList(); ;

                return matching;
            }
        }
    }
}

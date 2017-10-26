using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace nmssavetool
{
    /// <summary>
    /// Provides methods to manipulate a NMS save game Inventory group.
    /// </summary>
    public class Inventory
    {
        //enum InventoryClass
        //{
        //    Unknown,
        //    SuitGeneral,
        //    SuitTechOnly,
        //    SuitCargo,
        //    ShipGeneral,
        //    ShipTechOnly,
        //    FreighterGeneral,
        //    FreighterTechOnly,
        //    Vehicle,
        //    MultiTool,
        //    Container
        //}

        private dynamic _json;
        private int _maxSubstanceAmount;
        private int _maxProductAmount;
        private string _groupName;
        private dynamic[,] _slots;
        private int _maxWidth;
        private int _maxHeight;
        private bool[,] _validSlots;
        private HashSet<InventoryItemCategory> _allowedCategories = new HashSet<InventoryItemCategory>();
        private InventoryItemTypes _invItemTypes;


        #region Public Constructors

        public Inventory(dynamic json, string groupName, InventoryItemTypes invItemTypes, IEnumerable<InventoryItemCategory> allowedCategories, int maxWidth, int maxHeight)
        {
            _json = json;
            _groupName = groupName;
            _invItemTypes = invItemTypes;
            _maxWidth = maxWidth;
            _maxHeight = maxHeight;

            _allowedCategories = new HashSet<InventoryItemCategory>(allowedCategories);

            _maxProductAmount = allowedCategories.Contains(InventoryItemCategory.Product) ? 1 * (int)_json.ProductMaxStorageMultiplier : 0;
            _maxSubstanceAmount = allowedCategories.Contains(InventoryItemCategory.Substance) ? 250 * (int)_json.SubstanceMaxStorageMultiplier : 0;

            LoadSlots();
        }

        #endregion


        #region Public Properties

        public InventoryItemCategory[] AllowedCategories
        {
            get
            {
                return _allowedCategories.ToArray();
            }
        }

        public int Width
        {
            get
            {
                return _json.Width;
            }
        }

        public int Height
        {
            get
            {
                return _json.Height;
            }
        }

        public dynamic this[int x, int y]
        {
            get
            {
                if (x >= Width || y >= Height || x < 0 || y < 0)
                {
                    return null;
                }

                return _slots[x, y];
            }            
        }

        public string GroupName
        {
            get { return _groupName; }
        }

        #endregion

        #region Public Methods

        public bool IsValidSlotPos(int x, int  y)
        {
            return (x >= 0) && (x < Width) && (y >= 0) && (y < Height);
        }

        public bool IsSlotAtPosValid(int x, int y)
        {
            return IsValidSlotPos(x, y) && _validSlots[x, y];
        }

        public bool IsSlotAtPosEmpty(int x, int y)
        {
            return IsValidSlotPos(x, y) && _validSlots[x, y] && (_slots[x, y] == null);
        }
        
        public bool IsSlotAtPosOccupied(int x, int y)
        {
            return IsSlotAtPosValid(x, y) && _slots[x, y] != null;
        }

        public bool IsSlotAtPosTechnology(int x, int y)
        {
            return IsSlotTechnology(_slots[x, y]);
        }

        private bool IsSlotTechnology(dynamic slot)
        {
            return slot != null && slot.Type.InventoryType == "Technology";
        }

        public bool IsSlotAtPosProduct(int x, int y)
        {
            return IsSlotProduct(_slots[x, y]);
        }

        private bool IsSlotProduct(dynamic slot)
        {
            return slot != null && slot.Type.InventoryType == "Product";
        }

        public bool IsSlotAtPosSubstance(int x, int y)
        {
            return IsSlotSubstance(_slots[x, y]);
        }

        private bool IsSlotSubstance(dynamic slot)
        {
            return slot != null && slot.Type.InventoryType == "Substance";
        }

        public string DescribeSlot(int x, int y)
        {
            if (!_validSlots[x,y])
            {
                return string.Format("[{0},{1}] <Invalid>", x + 1, y + 1);
            }

            var slot = _slots[x, y];

            if (slot == null)
            {
                return string.Format("[{0},{1}] <Empty>", x + 1, y + 1);
            }

            InventoryItemType invCode = _invItemTypes[slot.Id.Value];

            if (IsSlotTechnology(slot))
            {
                if ((float)slot.DamageFactor != 0.0)
                {
                    return string.Format("{0} ({1}), damage: {2:p0} <{3}>", invCode.Name, FormatSlotId(slot.Id), (float)slot.DamageFactor * 100.0, invCode.Category);
                }

                return string.Format("{0} ({1}) <{2}>", invCode.Name, FormatSlotId(slot.Id), invCode.Category);
            }

            if (IsSlotSubstance(slot))
            {
                return string.Format("{0} ({1}), {2}/{3}  <{4}>", invCode.Name, FormatSlotId(slot.Id), (int)slot.Amount, (int)slot.MaxAmount, invCode.Category);
            }

            if (IsSlotProduct(slot))
            {
                return string.Format("{0} ({1}), {2}/{3} <{4}>", invCode.Name, FormatSlotId(slot.Id), (int)slot.Amount, (int)slot.MaxAmount, invCode.Category);
            }

            return "<Unknown>";
        }

        public override string ToString()
        {
            return GroupName;
        }

        public bool IsItemTypeCategoryAllowed(InventoryItemCategory codeType)
        {
            return _allowedCategories.Contains(codeType);
        }

        public bool Repair()
        {
            bool changedSomething = false;

            foreach (var slot in _json.Slots)
            {
                if (IsSlotTechnology(slot) && (slot.DamageFactor != 0.0f))
                {
                    slot.DamageFactor = 0.0f;
                    changedSomething = true;
                }
            }

            return changedSomething;
        }

        public bool Recharge()
        {
            bool changedSomething = false;

            foreach (var slot in _json.Slots)
            {
                if (IsSlotTechnology(slot) && _invItemTypes[(string)slot.Id].IsRechargeable && (slot.Amount != slot.MaxAmount))
                { 
                    slot.Amount = slot.MaxAmount;
                    changedSomething = true;
                }
            }

            return changedSomething;
        }

        public bool Refill()
        {
            bool changedSomething = false;

            foreach (var slot in _json.Slots)
            {
                if (!IsSlotTechnology(slot) && (slot.Amount != slot.MaxAmount))
                {
                    slot.Amount = slot.MaxAmount;
                    changedSomething = true;
                }
            }

            return changedSomething;
        }

        public IList<InventoryItemType> FindMatchingItemTypes(string pattern, InventoryItemCategory[] categories)
        {
            return _invItemTypes.FindMatchingItemTypes(pattern, categories);
        }

        public bool AddItemToFreeSlot(InventoryItemType itemType, out int x, out int y)
        {
            if (!_allowedCategories.Contains(itemType.Category))
            {
                throw new ArgumentException(string.Format("Inventory group does not support items of the specified type, {0}", itemType.Category));
            }

            if (!FindFreeSlot(out x, out y))
            {
                return false;
            }

            if (itemType.Category == InventoryItemCategory.Product)
            {
                var slot = MakeProductSlot(itemType.Id, x, y);
                ((JArray)_json.Slots).Last.AddAfterSelf(slot);
                _slots[x, y] = slot;
            }
            else if (itemType.Category == InventoryItemCategory.Substance)
            {
                var slot = MakeSubstanceSlot(itemType.Id, x, y);
                ((JArray)_json.Slots).Last.AddAfterSelf(slot);
                _slots[x, y] = slot;
            }
            else
            {
                var slot = MakeTechSlot(itemType, x, y);
                ((JArray)_json.Slots).Last.AddAfterSelf(slot);
                _slots[x, y] = slot;
            }

            return true;
        }

        public void SwapSlot(int x1, int y1, int x2, int y2)
        {
            CheckSlotPos(x1, y1);
            CheckSlotPos(x2, y2);

            if (IsSlotAtPosOccupied(x1, y1))
            {
                // Slot 1 occupied

                if (IsSlotAtPosOccupied(x2, y2))
                {
                    // Both slots occupied, just swap them
                    _slots[x1, y1].Index.X = x2;
                    _slots[x1, y1].Index.Y = y2;
                    _slots[x2, y2].Index.X = x1;
                    _slots[x2, y2].Index.Y = y1;
                    dynamic tmp = _slots[x1, y1];
                    _slots[x1, y1] = _slots[x2, y2];
                    _slots[x2, y2] = tmp;
                }
                else if (IsSlotAtPosEmpty(x2, y2))
                {
                    // First slot occupied, second slot empty. 
                    _slots[x1, y1].Index.X = x2;
                    _slots[x1, y1].Index.Y = y2;
                    _slots[x2, y2] = _slots[x1, y1];
                    _slots[x1, y1] = null;
                }
                else
                {
                    // First slot occupied, second slot invalid (greyed out in UI)
                    _slots[x1, y1].Index.X = x2;
                    _slots[x1, y1].Index.Y = y2;
                    _slots[x2, y2] = _slots[x1, y1];
                    _slots[x1, y1] = null;
                    MakeSlotValidInternal(x2, y2);
                    InvalidateSlotInternal(x1, y1);
                }
            }
            else if (IsSlotAtPosEmpty(x1, y1))
            {
                // Slot 1 empty, but valid

                if (IsSlotAtPosOccupied(x2, y2))
                {
                    // First slot empty, second slot occupied. 
                    _slots[x2, y2].Index.X = x1;
                    _slots[x2, y2].Index.Y = y1;
                    _slots[x1, y1] = _slots[x2, y2];
                    _slots[x2, y2] = null;
                }
                // else if (IsSlotAtPosEmpty(x2, y2))
                // Both slots empty - do nothing
                else
                {
                    // First slot empty, second slot invalid
                    MakeSlotValidInternal(x2, y2);
                    InvalidateSlotInternal(x1, y1);
                }
            }
            else
            {
                // Slot 1 invalid (greyed out in UI)

                if (IsSlotAtPosOccupied(x2, y2))
                {
                    // First slot invalid, second slot occupied
                    MakeSlotValidInternal(x1, y1);
                    _slots[x2, y2].Index.X = x1;
                    _slots[x2, y2].Index.Y = y1;
                    _slots[x1, y1] = _slots[x2, y2];
                    _slots[x2, y2] = null;
                    InvalidateSlotInternal(x2, y2);
                }
                else if (IsSlotAtPosEmpty(x2, y2))
                {
                    // First slot invalid, second slot empty. 
                    MakeSlotValidInternal(x1, y1);
                    InvalidateSlotInternal(x2, y2);
                }
                // else
                // First slot invalid, second slot invalid
                // (nothing to do)
            }
        }

        public void MoveSlot(int srcX, int srcY, int dstX, int dstY)
        {
            CheckSlotPos(srcX, srcY);
            CheckSlotPos(dstX, dstY);

            if (IsSlotAtPosOccupied(srcX, srcY))
            {
                // Source slot is occupied (valid, non-empty)

                // Get source slot
                JToken srcSlot = (JToken)_slots[srcX, srcY];

                // Remove source slot from array
                srcSlot.Remove();
                _slots[srcX, srcY] = null;

                // Update source slot X and Y values to those of the destination
                srcSlot["Index"]["X"] = dstX;
                srcSlot["Index"]["Y"] = dstY;

                // Check destination slot position
                if (IsSlotAtPosOccupied(dstX, dstY))
                {
                    // Destination slot is occupied. Replace it with value from source slot.
                    JToken newSlotContents = (JToken)_slots[dstX, dstY];
                    newSlotContents.Replace(srcSlot);
                    _slots[dstX, dstY] = newSlotContents;
                }
                else if (IsSlotAtPosEmpty(dstX, dstY))
                {
                    // Destination slot is empty, but valid. 

                    // Add it to the arrays
                    ((JArray)_json.Slots).Add(srcSlot);
                    _slots[dstX, dstY] = srcSlot;
                }
                else
                {
                    // Destination slot not in the list of valid slots (it is greyed out in the UI)

                    // Make it valid
                    MakeSlotValidInternal(dstX, dstY);

                    // Add it to the arrays
                    ((JArray)_json.Slots).Add(srcSlot);
                    _slots[dstX, dstY] = srcSlot;
                }
            }
            else if (IsSlotAtPosEmpty(srcX, srcY))
            {
                // Source slot is empty, but valid

                // Check destination slot position
                if (IsSlotAtPosOccupied(dstX, dstY))
                {
                    // Remove destination contents, but leave valid
                    ((JToken)_slots[dstX, dstY]).Remove();
                    _slots[dstX, dstY] = null;
                }
                // Else if destination empty, there is nothing to do
                else if (!IsSlotAtPosValid(dstX, dstY))
                {
                    // Destination is invalid. Just make it valid
                    MakeSlotValidInternal(dstX, dstY);
                }
            }
            else
            {
                // Source slot not in the list of valid slots (it is greyed out in UI)

                if (IsSlotAtPosValid(dstX, dstY))
                {
                    // Destination slot is valid

                    // Invalidate destination position
                    InvalidateSlotInternal(dstX, dstY);

                    // Make source position valid, but leave it empty. By marking it valid we avoid increasing the number of invalid slots.
                    MakeSlotValidInternal(srcX, srcY);
                }
                // Else both slots are invalid - nothing to do
            }
        }

        public void MaximizeSlots()
        {
            int oldWidth = Width;
            int oldHeight = Height;
            int newWidth = Math.Max(_maxWidth, oldWidth);
            int newHeight = Math.Max(_maxHeight, oldHeight);

            if (newWidth != oldWidth || newHeight != oldHeight)
            {
                dynamic[,] oldSlots = _slots;
                bool[,] oldValidSlots = _validSlots;
                _slots = new dynamic[newWidth, newHeight];
                _validSlots = new bool[newWidth, newHeight];

                // Copy slots and valid flags to new array
                for (int x = 0; x < oldWidth; ++x)
                {
                    for (int y = 0; y < oldHeight; ++y)
                    {
                        _slots[x, y] = oldSlots[x, y];
                        _validSlots[x, y] = oldValidSlots[x, y];
                    }
                }

                // Update width and height values
                _json.Width = newWidth;
                _json.Height = newHeight;

            }

            // Now iterate through all slots, making them all valid
            for (int x = 0; x < newWidth; ++x)
            {
                for (int y = 0; y < newHeight; ++y)
                {
                    MakeSlotValidInternal(x, y);
                }
            }
        }

        public void SetSlot(InventoryItemType itemType, int x, int y)
        {
            CheckSlotPos(x, y);

            if (!IsItemTypeCategoryAllowed(itemType.Category))
            {
                throw new ArgumentException(string.Format("Invalid inventory item type. The item's category, {0}, is not compatible with inventory group, {1}", itemType.Category, GroupName));
            }

            MakeSlotValidInternal(x, y);
            DeleteSlotInternal(x, y);

            if (itemType.Category == InventoryItemCategory.Product)
            {
                var slot = MakeProductSlot(itemType.Id, x, y);
                ((JArray)_json.Slots).Last.AddAfterSelf(slot);
                _slots[x, y] = slot;
            }
            else if (itemType.Category == InventoryItemCategory.Substance)
            {
                var slot = MakeSubstanceSlot(itemType.Id, x, y);
                ((JArray)_json.Slots).Last.AddAfterSelf(slot);
                _slots[x, y] = slot;
            }
            else
            {
                var slot = MakeTechSlot(itemType, x, y);
                ((JArray)_json.Slots).Last.AddAfterSelf(slot);
                _slots[x, y] = slot;
            }
        }

        public void DeleteSlot(int x, int y)
        {
            CheckSlotPos(x, y);

            DeleteSlotInternal(x, y);
        }

        public void InvalidateSlot(int x, int y)
        {
            CheckSlotPos(x, y);

            InvalidateSlotInternal(x, y);
        }

        private void MakeSlotValid(int x, int y)
        {
            CheckSlotPos(x, y);

            MakeSlotValidInternal(x, y);
        }

        #endregion

            #region Private Methods

        private void DeleteSlotInternal(int x, int y)
        {
            if (_slots[x, y] != null)
            {
                ((JToken)_slots[x, y]).Remove();
                _slots[x, y] = null;
            }
        }

        public void InvalidateSlotInternal(int x, int y)
        {
            DeleteSlotInternal(x, y);

            if (_validSlots[x, y])
            {
                JObject jobj = (JObject)_json;
                List<JToken> slotIndicesToRemove = new List<JToken>();

                foreach (var slotIndex in jobj["ValidSlotIndices"].Where(s => (x == s.Value<int>("X")) && (y == s.Value<int>("Y"))))
                {
                    slotIndicesToRemove.Add(slotIndex);
                }

                foreach (var slotIndex in slotIndicesToRemove)
                {
                    slotIndex.Remove();
                }

                _validSlots[x, y] = false;
            }
        }

        private void CheckSlotPos(int x, int y)
        {
            if (!IsValidSlotPos(x,y))
            {
                throw new ArgumentException(string.Format("Invalid inventory slot position. Inventory Height x Width = {0} x {1}", Height, Width));
            }
        }

        private void MakeSlotValidInternal(int x, int y)
        {
            if (!_validSlots[x, y])
            {
                ((JArray)_json.ValidSlotIndices).Last.AddAfterSelf(MakeValidSlotIndex(x, y));
                _validSlots[x, y] = true;
            }
        }

        private string FormatSlotId(dynamic id)
        {
            string idStr = (string)id;
            if (idStr == null)
            {
                return string.Empty;
            }

            return idStr;
        }

        private static dynamic PrimaryShipNode(dynamic json)
        {
            int primaryShipIndex = json.PlayerStateData.PrimaryShip;
            return json.PlayerStateData.ShipOwnership[primaryShipIndex];
        }

        private static dynamic PrimaryVehicleNode(dynamic json)
        {
            int primaryVehicleIndex = json.PlayerStateData.PrimaryVehicle;
            return json.PlayerStateData.VehicleOwnership[primaryVehicleIndex];
        }

        private JObject MakeValidSlotIndex(int x, int y)
        {
            var jobj = new JObject();
            dynamic si = jobj;
            si.X = x;
            si.Y = y;
            return jobj;
        }

        private JObject MakeTechSlot(InventoryItemType itemType, int x, int  y)
        {
            //{
            //  "Type": {
            //    "InventoryType": "Technology"
            //  },
            //  "Id": "^JET1",
            //  "Amount": 1,
            //  "MaxAmount": 1,
            //  "DamageFactor": 0.0,
            //  "Index": {
            //    "X": 0,
            //    "Y": 0
            //  }
            //},

            var slotObj = new JObject();
            dynamic slot = slotObj;

            slot.Type = new JObject();
            slot.Type.InventoryType = "Technology";
            slot.Id = itemType.Id;
            slot.Amount = itemType.DefaultAmount;
            slot.MaxAmount = itemType.MaxAmount;
            slot.DamageFactor = 0.0f;
            slot.Index = new JObject();
            slot.Index.X = x;
            slot.Index.Y = y;

            return slotObj;
        }

        private JObject MakeSubstanceSlot(string id, int x, int y)
        {
            //{
            //  "Type": {
            //    "InventoryType": "Substance"
            //  },
            //  "Id": "^COMRARE1",
            //  "Amount": 250,
            //  "MaxAmount": 250,
            //  "DamageFactor": 0.0,
            //  "Index": {
            //    "X": 7,
            //    "Y": 5
            //  }
            //},

            var slotObj = new JObject();
            dynamic slot = slotObj;

            slot.Type = new JObject();
            slot.Type.InventoryType = "Substance";
            slot.Id = id;
            slot.Amount = _maxSubstanceAmount;
            slot.MaxAmount = _maxSubstanceAmount;
            slot.DamageFactor = 0.0f;
            slot.Index = new JObject();
            slot.Index.X = x;
            slot.Index.Y = y;

            return slotObj;
        }

        private JObject MakeProductSlot(string id, int x, int y)
        {
            //{
            //  "Type": {
            //    "InventoryType": "Product"
            //  },
            //  "Id": "^ACCESS3",
            //  "Amount": 1,
            //  "MaxAmount": 1,
            //  "DamageFactor": 0.0,
            //  "Index": {
            //    "X": 4,
            //    "Y": 0
            //  }
            //}

            var slotObj = new JObject();
            dynamic slot = slotObj;

            slot.Type = new JObject();
            slot.Type.InventoryType = "Product";
            slot.Id = id;
            slot.Amount = _maxProductAmount;
            slot.MaxAmount = _maxProductAmount;
            slot.DamageFactor = 0.0f;
            slot.Index = new JObject();
            slot.Index.X = x;
            slot.Index.Y = y;

            return slotObj;
        }

        private static bool IsInventoryCodeTypeTech(InventoryItemCategory codeType)
        {
            return !((codeType == InventoryItemCategory.Product) || (codeType == InventoryItemCategory.Substance));
        }

        private bool FindFreeSlot(out int x, out int y)
        {
            // Search from bottom right corner, as this is generally where it will be easier for the player to see
            for (int row = Height-1; row >= 0; row--)
            {
                for (int col = Width-1; col >= 0; col--)
                {
                    if (IsSlotAtPosEmpty(col, row))
                    {
                        x = col;
                        y = row;
                        return true;
                    }
                }
            }

            x = 0; y = 0;
            return false;
        }

        private void LoadSlots()
        {
            _slots = new dynamic[Width, Height];
            _validSlots = new bool[Width, Height];

            foreach (var slot in _json.Slots)
            {
                _slots[slot.Index.X, slot.Index.Y] = slot;
            }

            foreach (var validSlot in _json.ValidSlotIndices)
            {
                _validSlots[(int)validSlot.X, (int)validSlot.Y] = true;
            }
        }

        #endregion
    }
}

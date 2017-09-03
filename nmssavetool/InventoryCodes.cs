using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using CsvHelper;

namespace nmssavetool
{
    internal enum InventoryCodeType
    {
        Unknown,
        Product,
        Substance,
        SuitTech,
        ShipTech,
        GunTech,
        FreighterTech
    }

    internal class InventoryCode
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public InventoryCodeType Type { get; set; }

        public InventoryCode(string id, string name, InventoryCodeType type)
        {
            Id = id;
            Name = name;
            Type = type;
        }

        public override string ToString()
        {
            return string.Format("{0},{1},{2}", Id, Name, Type);
        }
    }

    internal class InventoryCodes
    {
        private Dictionary<string, InventoryCode> _codes = new Dictionary<string, InventoryCode>();

        public InventoryCodes()
        {
        }

        private InventoryCode UnknownFromId(string id)
        {
            return new InventoryCode(id, "?", InventoryCodeType.Unknown);
        }

        public InventoryCode this[string id]
        {
            get
            {
                InventoryCode val;
                if (id.StartsWith("^"))
                {
                    id = id.Substring(1);
                }
                if (!_codes.TryGetValue(id, out val))
                {
                    val = UnknownFromId(id);
                }

                return val;
            }
        }

        public void LoadFromCsvFile(string filePath)
        {
            var sr = new StreamReader(filePath);
            var csv = new CsvReader(sr);
            string idField, nameField, typeField;
            InventoryCodeType type;         

            while (csv.Read())
            {
                if (!csv.TryGetField(0, out idField))
                {
                    continue;
                }

                if (idField.StartsWith("^"))
                {
                    idField = idField.Substring(1);
                }

                if (!csv.TryGetField(1, out nameField))
                {
                    continue;
                }
                nameField = nameField.Trim();

                if (!csv.TryGetField(2, out typeField))
                {
                    continue;
                }

                if (!Enum.TryParse<InventoryCodeType>(typeField, true, out type))
                {
                    continue;
                }

                var code = new InventoryCode(idField, nameField, type);
                _codes[code.Id] = code;
            }
        }

        public void LoadFromDefaultCsvFile()
        {
            string dir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string codesFile = Path.Combine(dir, "InventoryCodes.csv");
            LoadFromCsvFile(codesFile);
        }
    }
}

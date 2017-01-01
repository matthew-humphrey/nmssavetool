using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using clipr;
using nomanssave;
using Newtonsoft.Json;

namespace nmssavetool
{
    public enum Commands
    {
        Decrypt,
        Encrypt,
        Refill,
        Repair
    }

    public class Options
    {
        public Options()
        {
            UseOldFormat = false;
        }

        [NamedArgument('g', "game-mode", Required = true, Description = "Use saves for which game mode (Normal|Surival|Creative)" )]
        public GameModes GameMode { get; set; }

        // Would like to use Verbs for these, but without support for help on the, the feature is useless.
        [PositionalArgument(0, Description = "Command to perform (Decrypt|Encrypt|Refill|Repair).")]
        public Commands Command { get; set; }

        [NamedArgument('f', "decrypted-file", Description = "Specifies the destination file for decrypt or the source file for encrypt.")]
        public string DecryptedFilePath { get; set; }

        [NamedArgument("v1-format", Description = "When encrypting a save file, use the old NMS V1 format")]
        public bool UseOldFormat { get; set; }
    }

    class Program
    {
        readonly string[] REFILLABLE_TECH = {
            // Inventory 
            "^PROTECT", "^ENERGY", "^TOX1", "^TOX2", "^TOX3", "^RAD1", "^RAD2", "^RAD3",
            "^COLD1", "^COLD2", "^COLD3", "^HOT1", "^HOT2", "^HOT3", "^UNW1", "^UNW2", "^UNW3",

            // ShipInventory
            "^SHIPGUN1", "^SHIPSHIELD", "^SHIPJUMP1", "^HYPERDRIVE", "^LAUNCHER", "^SHIPLAS1",

            // WeaponInventory
            "^LASER", "^GRENADE"
        };

        private GameSaveDir gsd;
        private HashSet<string> refillableTech;

        Program()
        {
            this.gsd = new GameSaveDir();
            this.refillableTech = new HashSet<string>(REFILLABLE_TECH);
        }

        public int Run(Options opt)
        {
            bool success = false;

            switch (opt.Command)
            {
                case Commands.Decrypt:
                    success = ProcessDecrypt(opt);
                    break;

                case Commands.Encrypt:
                    success = ProcessEncrypt(opt);
                    break;

                case Commands.Refill:
                    success = ProcessRefill(opt);
                    break;

                case Commands.Repair:
                    success = ProcessRepair(opt);
                    break;
            }

            Console.WriteLine(success ? "SUCCESS" : "FAILED");

            return success ? 0 : 1;
        }

        static Options ParseArgs(string[] args)
        {
            // Started out down the path of using this argument parser, only to find out that it has a lot of missing functionality, and hasn't been updated since 2015
            var opt = CliParser.StrictParse<Options>(args);

            switch (opt.Command)
            {
                case Commands.Decrypt:
                    if (opt.DecryptedFilePath == null)
                    {
                        Console.WriteLine("The Decrypt command requires an output file be specified with the (-d|--decrypted-file) parameter");
                        Environment.Exit(1);
                    }
                    break;
                case Commands.Encrypt:
                    if (opt.DecryptedFilePath == null || !File.Exists(opt.DecryptedFilePath))
                    {
                        Console.WriteLine("The Encrypt command requires an (existing) input file be specified with the (-d|--decrypted-file) parameter");
                        Environment.Exit(1);
                    }
                    break;
                case Commands.Refill:
                case Commands.Repair:
                    if (opt.DecryptedFilePath == null)
                    {
                        opt.DecryptedFilePath = Path.GetTempFileName();
                    }
                    break;
                default:
                    throw new InvalidOperationException();
            }

            return opt;
        }

        static void Main(string[] args)
        {
            var opt = ParseArgs(args);

            Program program = null;
            try
            {
                program = new Program();
                int rc = program.Run(opt);
                Environment.Exit(rc);
            }
            catch (Exception x)
            {
                Console.WriteLine(x.Message);
                Environment.Exit(2);
            }
        }

        private object ReadSaveFile(Options opt)
        {
            string metadataPath;
            string storagePath;
            uint archiveNumber;
            ulong profileKey;

            gsd.FindLatestGameSaveFiles(opt.GameMode, out metadataPath, out storagePath, out archiveNumber, out profileKey);

            Console.WriteLine("Reading latest {0}-mode save game file from: {1}", opt.GameMode, storagePath);

            string jsonStr = Storage.Read(metadataPath, storagePath, archiveNumber, profileKey);

            return JsonConvert.DeserializeObject(jsonStr);
        }

        private void WriteSaveFile(Options opt, object json)
        {
            string formattedJson = JsonConvert.SerializeObject(json, Formatting.None);

            string metadataPath;
            string storagePath;
            uint archiveNumber;
            ulong profileKey;

            gsd.FindLatestGameSaveFiles(opt.GameMode, out metadataPath, out storagePath, out archiveNumber, out profileKey);

            Console.WriteLine("Writing latest {0}-mode save game file to: {1}", opt.GameMode, storagePath);
            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(formattedJson)))
            {
                Storage.Write(metadataPath, storagePath, ms, archiveNumber, profileKey, opt.UseOldFormat);
            }
        }

        private bool ProcessRefill(Options opt)
        {
            dynamic json;
            try
            {
                json = ReadSaveFile(opt);
            }
            catch (Exception x)
            {
                Console.WriteLine("Error loading or parsing save file: {0}", x.Message);
                return false;
            }

            // Now iterate through JSON, maxing out technology, Substance, and Product values in Inventory, ShipInventory, and FreighterInventory
            foreach (var slot in json.PlayerStateData.Inventory.Slots)
            {
                slot.DamageFactor = 0.0f;
                if (slot.Type.InventoryType == "Product" || slot.Type.InventoryType == "Substance" || (slot.Type.InventoryType == "Technology" && refillableTech.Contains(slot.Id.Value)))
                {
                    slot.Amount = slot.MaxAmount;
                }
            }

            foreach (var slot in json.PlayerStateData.ShipInventory.Slots)
            {
                slot.DamageFactor = 0.0f;
                if (slot.Type.InventoryType == "Product" || slot.Type.InventoryType == "Substance" || (slot.Type.InventoryType == "Technology" && refillableTech.Contains(slot.Id.Value)))
                {
                    slot.Amount = slot.MaxAmount;
                }
            }

            foreach (var slot in json.PlayerStateData.WeaponInventory.Slots)
            {
                slot.DamageFactor = 0.0f;
                if (slot.Type.InventoryType == "Technology" && refillableTech.Contains(slot.Id.Value))
                {
                    slot.Amount = slot.MaxAmount;
                }
            }

            foreach (var slot in json.PlayerStateData.FreighterInventory.Slots)
            {
                if (slot.Type.InventoryType == "Product" || slot.Type.InventoryType == "Substance")
                {
                    slot.Amount = slot.MaxAmount;
                }
            }

            json.PlayerStateData.Health = 8;
            json.PlayerStateData.ShipHealth = 8;
            json.PlayerStateData.Shield = 100;
            json.PlayerStateData.ShipShield = 200;
            json.PlayerStateData.Energy = 100;

            try
            {
                WriteSaveFile(opt, json);
            }
            catch (Exception x)
            {
                Console.WriteLine("Error storing save file: {0}", x.Message);
                return false;
            }

            return true;
        }

        private bool ProcessRepair(Options opt)
        {
            dynamic json;
            try
            {
                json = ReadSaveFile(opt);
            }
            catch (Exception x)
            {
                Console.WriteLine("Error loading or parsing save file: {0}", x.Message);
                return false;
            }

            // Now iterate through JSON, maxing out technology, Substance, and Product values in Inventory, ShipInventory, and FreighterInventory
            foreach (var slot in json.PlayerStateData.Inventory.Slots)
            {
                slot.DamageFactor = 0.0f;
            }

            foreach (var slot in json.PlayerStateData.ShipInventory.Slots)
            {
                slot.DamageFactor = 0.0f;
            }

            try
            {
                WriteSaveFile(opt, json);
            }
            catch (Exception x)
            {
                Console.WriteLine("Error storing save file: {0}", x.Message);
                return false;
            }

            return true;
        }

        private bool ProcessEncrypt(Options opt)
        {
            Console.WriteLine("Reading JSON save game data from: {0}", opt.DecryptedFilePath);
            string unformattedJson;
            try
            {
                unformattedJson = File.ReadAllText(opt.DecryptedFilePath);
            }
            catch (IOException x)
            {
                Console.WriteLine("Error reading JSON save game file: {0}", x.Message);
                return false;
            }

            object json;
            try
            {
                json = JsonConvert.DeserializeObject(unformattedJson);
            }
            catch (Exception x)
            {
                Console.WriteLine("Error parsing save game file: {0}", x.Message);
                return false;
            }

            try
            {
                WriteSaveFile(opt, json);
            }
            catch (Exception x)
            {
                Console.WriteLine("Error storing save file: {0}", x.Message);
                return false;
            }

            return true;
        }

        private bool ProcessDecrypt(Options opt)
        {
            object json;
            try
            {
                json = ReadSaveFile(opt);
            }
            catch (Exception x)
            {
                Console.WriteLine("Error loading or parsing save file: {0}", x.Message);
                return false;
            }

            string formattedJson;
            try
            {
                formattedJson = JsonConvert.SerializeObject(json, Formatting.Indented);
            }
            catch (Exception x)
            {
                Console.WriteLine("Error formatting JSON (invalid save?): {0}", x.Message);
                return false;
            }

            try
            {
                File.WriteAllText(opt.DecryptedFilePath, formattedJson);
            }
            catch (Exception x)
            {
                Console.WriteLine("Error writing decrypted JSON: {0}", x.Message);
                return false;
            }

            return true;
        }
    }
}

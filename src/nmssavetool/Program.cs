using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        Refill
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
        [PositionalArgument(0, Description = "Command to perform (Decrypt|Encrypt|Refill).")]
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
            "^SHIPGUN1", "^SHIPSHIELD", "^SHIPJUMP1", "^HYPERDRIVE", "^LAUNCHER", "^SHIPLAS1"
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
            int rc = 0;

            switch (opt.Command)
            {
                case Commands.Decrypt:
                    rc = ProcessDecrypt(opt);
                    break;

                case Commands.Encrypt:
                    rc = ProcessEncrypt(opt);
                    break;

                case Commands.Refill:
                    rc = ProcessRefill(opt);
                    break;
            }

            Console.WriteLine(rc == 0 ? "SUCCESS" : "FAILED");

            return rc;
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

        private int ProcessRefill(Options opt)
        {
            string metadataPath = null;
            string storagePath = null;
            uint archiveNumber = 0;
            ulong profileKey = 0;

            try
            {
                gsd.FindLatestGameSaveFiles(opt.GameMode, out metadataPath, out storagePath, out archiveNumber, out profileKey);
            }
            catch (Exception x)
            {
                Console.WriteLine(x.Message);
                return 2;
            }

            string unformattedJson = null;
            string formattedJson = null;

            Console.WriteLine("Decrypting latest {0}-mode save game file from: {1}", opt.GameMode, storagePath);

            try
            {
                unformattedJson = Storage.Read(metadataPath, storagePath, archiveNumber, profileKey);
            }
            catch (Exception x)
            {
                Console.WriteLine("Error decrypting save game: {0}", x.Message);
                return 5;
            }

            dynamic json;
            try
            {
                json = JsonConvert.DeserializeObject(unformattedJson);
            }
            catch (Exception x)
            {
                Console.WriteLine("Error parsing JSON (invalid save?): {0}", x.Message);
                return 5;
            }

            // Now iterate through JSON, maxing out technology, Substance, and Product values in Inventory, ShipInventory, and FreighterInventory
            foreach (var slot in json.PlayerStateData.Inventory.Slots)
            {
                if (slot.Type.InventoryType == "Product" || slot.Type.InventoryType == "Substance" || (slot.Type.InventoryType == "Technology" && refillableTech.Contains(slot.Id.Value)))
                {
                    slot.Amount = slot.MaxAmount;
                }
            }

            foreach (var slot in json.PlayerStateData.ShipInventory.Slots)
            {
                if (slot.Type.InventoryType == "Product" || slot.Type.InventoryType == "Substance" || (slot.Type.InventoryType == "Technology" && refillableTech.Contains(slot.Id.Value)))
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

            try
            {
                formattedJson = JsonConvert.SerializeObject(json, Formatting.None);
            }
            catch (Exception x)
            {
                Console.WriteLine("Error creating modified JSON : {0}", x.Message);
                return 5;
            }

            // Write it back
            Console.WriteLine("Writing back modified save game");
            try
            {
                using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(formattedJson)))
                {
                    Storage.Write(metadataPath, storagePath, ms, archiveNumber, profileKey, opt.UseOldFormat);
                }
            }
            catch (Exception x)
            {
                Console.WriteLine("Error encrypting save game: {0}", x.Message);
                return 5;
            }

            return 0;
        }

        private int ProcessEncrypt(Options opt)
        {
            string metadataPath = null;
            string storagePath = null;
            uint archiveNumber = 0;
            ulong profileKey = 0;

            try
            {
                gsd.FindLatestGameSaveFiles(opt.GameMode, out metadataPath, out storagePath, out archiveNumber, out profileKey);
            }
            catch (Exception x)
            {
                Console.WriteLine(x.Message);
                return 2;
            }

            string unformattedJson = null;
            string formattedJson = null;

            Console.WriteLine("Reading JSON from: {0}", opt.DecryptedFilePath);
            Console.WriteLine("Encrypting to latest {0}-mode save game file: {1}", opt.GameMode, storagePath);
            try
            {
                unformattedJson = File.ReadAllText(opt.DecryptedFilePath);
            }
            catch (IOException x)
            {
                Console.WriteLine("Error reading JSON: {0}", x.Message);
                return 4;
            }

            // Not strictly necessary, but it's a good check for validity, and it compacts the file back down
            try
            {
                formattedJson = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(unformattedJson), Formatting.None);
            }
            catch (Exception x)
            {
                Console.WriteLine("Error parsing JSON (check your edits): {0}", x.Message);
                return 4;
            }

            try
            {
                using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(formattedJson)))
                {
                    Storage.Write(metadataPath, storagePath, ms, archiveNumber, profileKey, opt.UseOldFormat);
                }
            }
            catch (Exception x)
            {
                Console.WriteLine("Error encrypting save game: {0}", x.Message);
                return 4;
            }

            return 0;
        }

        private int ProcessDecrypt(Options opt)
        {
            string metadataPath = null;
            string storagePath = null;
            uint archiveNumber = 0;
            ulong profileKey = 0;

            try
            {
                gsd.FindLatestGameSaveFiles(opt.GameMode, out metadataPath, out storagePath, out archiveNumber, out profileKey);
            }
            catch (Exception x)
            {
                Console.WriteLine(x.Message);
                return 2;
            }

            string unformattedJson = null;
            string formattedJson = null;

            Console.WriteLine("Decrypting latest {0}-mode save game file from: {1}", opt.GameMode, storagePath);
            Console.WriteLine("Writing formatted JSON to: {0}", opt.DecryptedFilePath);

            try
            {
                unformattedJson = Storage.Read(metadataPath, storagePath, archiveNumber, profileKey);
            }
            catch (Exception x)
            {
                Console.WriteLine("Error decrypting save game: {0}", x.Message);
                return 3;
            }

            try
            {
                formattedJson = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(unformattedJson), Formatting.Indented);
            }
            catch (Exception x)
            {
                Console.WriteLine("Error formatting JSON (invalid save?): {0}", x.Message);
                return 3;
            }

            try
            {
                File.WriteAllText(opt.DecryptedFilePath, formattedJson);
            }
            catch (Exception x)
            {
                Console.WriteLine("Error writing decrypted JSON: {0}", x.Message);
                return 3;
            }

            return 0;
        }
    }
}

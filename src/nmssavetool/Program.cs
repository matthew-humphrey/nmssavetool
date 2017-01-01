using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using nomanssave;
using Newtonsoft.Json;
using CommandLine;


namespace nmssavetool
{
    public enum TechGrp
    {
        exosuit,
        multitool,
        ship,
        freighter
    }

    public class CommonOptions
    {
        [Option('g', "game-mode", Required = true, HelpText = "Use saves for which game mode (normal|surival|creative)")]
        public GameModes GameMode { get; set; }

        [Option('v', "verbose", HelpText = "Displays additional information during execution.")]
        public bool Verbose { get; set; }
    }

    [Verb("decrypt", HelpText = "Decrypt the latest game save slot and write it to a formatted JSON file.")]
    public class DecryptOptions : CommonOptions
    {
        [Option('o', "output", HelpText = "Specifies the file to which the decrypted, formatted game save will be written.")]
        public string OutputPath { get; set; }
    }

    [Verb("encrypt", HelpText = "Encrypt a JSON file and write it to the latest game save slot.")]
    public class EncryptOptions : CommonOptions
    {
        [Option('i', "input", HelpText = "Specifies the JSON input file which will be encrypted and written to the latest game save slot.")]
        public string InputPath { get; set; }

        [Option("v1-format", HelpText = "When encrypting, use the old NMS V1 format")]
        public bool UseOldFormat { get; set; }
    }

    [Verb("modify", HelpText = "Modify one or more attributes of a game save.")]
    public class ModifyOptions : CommonOptions
    {
        [Option('a', "all", HelpText = "Maximize exosuit, multi-tool, ship, and freighter inventory, health, fuel, and energy levels. Repair all damage.")]
        public bool Everything { get; set; }

        [Option('e', "energy", HelpText = "Maximize exosuit, multi-tool, and ship energy and fuel (hyperdrive and launcher) levels.")]
        public bool Energy { get; set; }

        [Option('i', "inventory", HelpText = "Maximize exosuit, multi-tool, ship, and freighter inventory.")]
        public bool Inventory { get; set; }

        [Option('r', "repair", HelpText = "Repair damage to exosuit, multi-tool, and ship.")]
        public bool Repair { get; set; }

        [Option('t', "apply-to", Separator = '+', Max = 4, Default = new TechGrp[] { TechGrp.exosuit, TechGrp.multitool, TechGrp.ship, TechGrp.freighter }, 
            HelpText = "What to apply changes to.")]
        public IEnumerable<TechGrp> TechGroups { get; set; }

        [Option("v1-format", HelpText = "When encrypting, use the old NMS V1 format")]
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

        static void Main(string[] args)
        {            
            Program program = null;
            try
            {
                program = new Program();
                bool success = program.Run(args);
                if (success)
                {
                    Console.WriteLine("Success");
                }
                Environment.Exit(success ? 0 : 1);
            }
            catch (Exception x)
            {
                Console.Error.WriteLine(x.Message);
                Environment.Exit(-1);
            }
        }

        Program()
        {
            this.gsd = new GameSaveDir();
            this.refillableTech = new HashSet<string>(REFILLABLE_TECH);
            LogWriter = Console.Out;
        }

        public TextWriter LogWriter { get; set; }

        public bool Verbose { get; set; }


        public bool Run(IEnumerable<string> args)
        {
            var result = CommandLine.Parser.Default.ParseArguments<DecryptOptions, EncryptOptions, ModifyOptions>(args);

            bool success = result.MapResult(
                (DecryptOptions opt) => RunDecrypt(opt),
                (EncryptOptions opt) => RunEncrypt(opt),
                (ModifyOptions opt) => RunModify(opt),
                _ => false);

            return success;
        }

        private void DoCommon(CommonOptions opt)
        {
            if (!Verbose)
            {
                Verbose = opt.Verbose;
            }
        }

        private bool RunDecrypt(DecryptOptions opt)
        {
            DoCommon(opt);

            object json;
            try
            {
                json = ReadLatestSaveFile(opt.GameMode);
            }
            catch (Exception x)
            {
                LogError("Error loading or parsing save file: {0}", x.Message);
                return false;
            }

            LogVerbose("Parsing and formatting save game JSON");
            string formattedJson;
            try
            {
                formattedJson = JsonConvert.SerializeObject(json, Formatting.Indented);
            }
            catch (Exception x)
            {
                LogError("Error formatting JSON (invalid save?): {0}", x.Message);
                return false;
            }

            LogVerbose("Writing formatted JSON to: {0}", opt.OutputPath);
            try
            {
                File.WriteAllText(opt.OutputPath, formattedJson);
            }
            catch (Exception x)
            {
                LogError("Error writing decrypted JSON: {0}", x.Message);
                return false;
            }

            return true;
        }

        private bool RunEncrypt(EncryptOptions opt)
        {
            DoCommon(opt);

            LogVerbose("Reading JSON save game data from: {0}", opt.InputPath);
            string unformattedJson;
            try
            {
                unformattedJson = File.ReadAllText(opt.InputPath);
            }
            catch (IOException x)
            {
                LogError("Error reading JSON save game file: {0}", x.Message);
                return false;
            }

            LogVerbose("Validating (parsing) JSON save game data");
            object json;
            try
            {
                json = JsonConvert.DeserializeObject(unformattedJson);
            }
            catch (Exception x)
            {
                LogError("Error parsing save game file: {0}", x.Message);
                return false;
            }

            try
            {
                WriteLatestSaveFile(opt.GameMode, json, opt.UseOldFormat);
            }
            catch (Exception x)
            {
                LogError("Error storing save file: {0}", x.Message);
                return false;
            }

            return true;
        }

        private bool RunModify(ModifyOptions opt)
        {
            DoCommon(opt);

            dynamic json;
            try
            {
                json = ReadLatestSaveFile(opt.GameMode);
            }
            catch (Exception x)
            {
                Console.WriteLine("Error loading or parsing save file: {0}", x.Message);
                return false;
            }

            // Now iterate through JSON, maxing out technology, Substance, and Product values in Inventory, ShipInventory, and FreighterInventory

            // Exosuit
            if (opt.TechGroups.Contains(TechGrp.exosuit))
            {
                LogVerbose("Updating Exosuit");
                if (opt.Energy || opt.Everything)
                {
                    json.PlayerStateData.Health = 8;
                    json.PlayerStateData.Energy = 100;
                    json.PlayerStateData.Shield = 100;
                }

                foreach (var slot in json.PlayerStateData.Inventory.Slots)
                {
                    if (opt.Repair || opt.Everything)
                    {
                        slot.DamageFactor = 0.0f;
                    }

                    if ((opt.Energy || opt.Everything) && slot.Type.InventoryType == "Technology" && refillableTech.Contains(slot.Id.Value))
                    {
                        slot.Amount = slot.MaxAmount;
                    }

                    if ((opt.Inventory || opt.Everything) && (slot.Type.InventoryType == "Product" || slot.Type.InventoryType == "Substance"))
                    {
                        slot.Amount = slot.MaxAmount;
                    }
                }
            }

            // Multitool (Weapon)
            if (opt.TechGroups.Contains(TechGrp.multitool))
            {
                LogVerbose("Updating Multitool");
                foreach (var slot in json.PlayerStateData.WeaponInventory.Slots)
                {
                    if (opt.Repair || opt.Everything)
                    {
                        slot.DamageFactor = 0.0f;
                    }

                    if ((opt.Energy || opt.Everything) && refillableTech.Contains(slot.Id.Value))
                    {
                        slot.Amount = slot.MaxAmount;
                    }
                }
            }

            if (opt.TechGroups.Contains(TechGrp.ship))
            {
                LogVerbose("Updating Ship");
                if (opt.Energy || opt.Everything)
                {
                    json.PlayerStateData.ShipHealth = 8;
                    json.PlayerStateData.ShipShield = 200;
                }

                foreach (var slot in json.PlayerStateData.ShipInventory.Slots)
                {
                    if (opt.Repair || opt.Everything)
                    {
                        slot.DamageFactor = 0.0f;
                    }

                    if ((opt.Energy || opt.Everything) && slot.Type.InventoryType == "Technology" && refillableTech.Contains(slot.Id.Value))
                    {
                        slot.Amount = slot.MaxAmount;
                    }

                    if ((opt.Inventory || opt.Everything) && (slot.Type.InventoryType == "Product" || slot.Type.InventoryType == "Substance"))
                    {
                        slot.Amount = slot.MaxAmount;
                    }
                }
            }


            if (opt.TechGroups.Contains(TechGrp.freighter))
            {
                LogVerbose("Updating Freighter");
                foreach (var slot in json.PlayerStateData.FreighterInventory.Slots)
                {
                    if ( (opt.Inventory || opt.Everything) &&
                        // Leave this next line in as protection against future version of NMS allowing other things in Freighter
                        (slot.Type.InventoryType == "Product" || slot.Type.InventoryType == "Substance") 
                       )
                    {
                        slot.Amount = slot.MaxAmount;
                    }
                }
            }

            try
            {
                WriteLatestSaveFile(opt.GameMode, json, opt.UseOldFormat);
            }
            catch (Exception x)
            {
                Console.WriteLine("Error storing save file: {0}", x.Message);
                return false;
            }

            return true;
        }

        private void Log(string format, params object[] arg)
        {
            LogWriter.WriteLine(format, arg);
        }

        private void LogVerbose(string format, params object[] arg)
        {
            if (Verbose)
            {
                LogWriter.WriteLine(format, arg);
            }
        }

        private void LogError(string format, params object[] arg)
        {
            Console.Error.WriteLine(format, arg);
        }

        // TODO: Move all save file handling into a separate class
        private object ReadLatestSaveFile(GameModes gameMode)
        {
            string metadataPath;
            string storagePath;
            uint archiveNumber;
            ulong profileKey;

            gsd.FindLatestGameSaveFiles(gameMode, out metadataPath, out storagePath, out archiveNumber, out profileKey);

            LogVerbose("Reading latest {0}-mode save game file from: {1}", gameMode, storagePath);

            string jsonStr = Storage.Read(metadataPath, storagePath, archiveNumber, profileKey);

            return JsonConvert.DeserializeObject(jsonStr);
        }

        private void WriteLatestSaveFile(GameModes gameMode, object json, bool useOldFormat)
        {
            string formattedJson = JsonConvert.SerializeObject(json, Formatting.None);

            string metadataPath;
            string storagePath;
            uint archiveNumber;
            ulong profileKey;

            gsd.FindLatestGameSaveFiles(gameMode, out metadataPath, out storagePath, out archiveNumber, out profileKey);

            LogVerbose("Writing latest {0}-mode save game file to: {1}", gameMode, storagePath);
            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(formattedJson)))
            {
                Storage.Write(metadataPath, storagePath, ms, archiveNumber, profileKey, useOldFormat);
            }
        }
    }
}

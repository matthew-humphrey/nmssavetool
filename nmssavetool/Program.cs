﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.IO.Compression;
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
        freighter,
        container
    }

    public enum InvGrp
    {
        all,
        exosuit,
        exosuit_general,
        exosuit_tech,
        exosuit_cargo,
        ship,
        ship_general,
        ship_tech,
        freighter,
        freighter_general,
        freighter_tech,
        multitool
    }

    public enum InvType
    {
        all,
        all_but_empty,
        substance,
        product,
        tech,
        non_tech,
        empty
    }

    public class CommonOptions
    {
        [Option('g', "game-mode", Required = true, HelpText = "Use saves for which game mode (normal|survival|creative|permadeath)")]
        public GameModes GameMode { get; set; }

        [Option('s', "save-dir", Required = false, HelpText = "Path to game save folder (optional - determined automatically if not provided)")]
        public string SaveDir { get; set; }

        [Option('v', "verbose", HelpText = "Displays additional information during execution.")]
        public bool Verbose { get; set; }
    }

    public class BackupOptions : CommonOptions
    {
        [Option('b', "backup-dir", HelpText = "If provided, will back up game saves to the specified directory.")]
        public string BackupDir { get; set; }
    }

    [Verb("decrypt", HelpText = "Decrypt the latest game save slot and write it to a formatted JSON file.")]
    public class DecryptOptions : CommonOptions
    {
        [Option('o', "output", Required = true, HelpText = "Specifies the file to which the decrypted, formatted game save will be written.")]
        public string OutputPath { get; set; }
    }

    [Verb("encrypt", HelpText = "Encrypt a JSON file and write it to the latest game save slot.")]
    public class EncryptOptions : BackupOptions
    {
        [Option('i', "input", Required = true, HelpText = "Specifies the JSON input file which will be encrypted and written to the latest game save slot.")]
        public string InputPath { get; set; }

        [Option("v1-format", HelpText = "When encrypting, use the old NMS V1 format")]
        public bool UseOldFormat { get; set; }
    }

    [Verb("modify", HelpText = "Refresh, repair, or refill exosuit, multitool, ship, or freighter inventory.")]
    public class ModifyOptions : BackupOptions
    {
        [Option('a', "all", HelpText = "Maximize exosuit, multi-tool, ship, freighter, and container inventory, health, fuel, and energy levels. Repair all damage.")]
        public bool Everything { get; set; }

        [Option('e', "energy", HelpText = "Maximize exosuit, multi-tool, and ship energy and fuel (hyperdrive and launcher) levels.")]
        public bool Energy { get; set; }

        [Option('i', "inventory", HelpText = "Maximize exosuit, multi-tool, ship, freighter, and container inventory.")]
        public bool Inventory { get; set; }

        [Option('r', "repair", HelpText = "Repair damage to exosuit, multi-tool, and ship.")]
        public bool Repair { get; set; }

        [Option('t', "apply-to", Separator = '+', Max = 5, Default = new TechGrp[] { TechGrp.exosuit, TechGrp.multitool, TechGrp.ship, TechGrp.freighter, TechGrp.container }, 
            HelpText = "What to apply changes to.")]
        public IEnumerable<TechGrp> TechGroups { get; set; }

        [Option("v1-format", HelpText = "When encrypting, use the old NMS V1 format")]
        public bool UseOldFormat { get; set; }

        [Option("randomize-ship-seed", HelpText = "Generate a new seed value for the Ship.")]
        public bool RandomizeShipSeed { get; set; }

        [Option("set-ship-seed", HelpText = "Set the seed value for the Ship.")]
        public string SetShipSeed { get; set; }

        [Option("randomize-multitool-seed", SetName = "multitool-seed", HelpText = "Generate a new seed value for the Multitool.")]
        public bool RandomizeMultitoolSeed { get; set; }

        [Option("set-multitool-seed", SetName = "multitool-seed", HelpText = "Set the seed value for the Multitool.")]
        public string SetMultitoolSeed { get; set; }

        [Option("randomize-freighter-seed", SetName = "freighter-seed", HelpText = "Generate a new seed value for the Freighter.")]
        public bool RandomizeFreighterSeed { get; set; }

        [Option("set-freighter-seed", SetName = "freighter-seed", HelpText = "Set the seed value for the Freighter.")]
        public string SetFreighterSeed { get; set; }

        [Option("set-units", SetName = "units", HelpText = "Set the player Units.")]
        public uint? SetUnits { get; set; }

        [Option("add-units", SetName = "units", HelpText = "Add the specified amount to player Units (negative units will subtract from total).")]
        public int? AddUnits { get; set; }

        [Option("set-galactic-coordinates", SetName="coordinates", HelpText = "Set the player position using the galactic coordinates displayed by signal scanners.")]
        public string SetGalacticCoordinates{ get; set; }

        [Option("set-portal-coordinates", SetName = "coordinates", HelpText = "Set the player position using portal coordinates.")]
        public string SetPortalCoordinates { get; set; }

        [Option("set-voxel-coordinates", SetName = "coordinates", HelpText = "Set the player position using the voxel coordinates used within the save-game file. Format is (x,y,z,ssi).")]
        public string SetVoxelCoordinates { get; set; }

        [Option("set-galaxy", HelpText = "Set the galaxy index (0 = Euclid Galaxy, 1 = Hilbert Dimension, 2 = Calypso Galaxy, etc.)")]
        public int? SetGalaxy { get; set; }
    }

    [Verb("info", HelpText = "Display information about a game save.")]
    public class InfoOptions : CommonOptions
    {
        [Option("no-basic", Default = false, HelpText = "Omits display of basic game-save information such as player stats and position.")]
        public bool NoBasic { get; set; }

        [Option('i', "show-inventory", Default = false, HelpText = "Display the contents of the specified inventory groups.")]
        public bool ShowInventory { get; set; }

        [Option('w', "inventory-groups", Separator = '+', Default = new InvGrp[] { InvGrp.all }, 
            HelpText = "Display the contents of the specified inventory groups.")]
        public IEnumerable<InvGrp> InventoryGroups { get; set; }

        [Option('t', "types", Separator = '+', Default = new InvType[] { InvType.all_but_empty },
            HelpText = "Which inventory types to include (all,all_but_empty,product,substance,tech,non_tech,empty).")]
        public IEnumerable<InvType> InventoryTypes { get; set; }
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

        private HashSet<string> _refillableTech;
        private Random _random;
        private InventoryCodes _invCodes;

        private InventoryCodes InvCodes
        {
            get
            {
                if (_invCodes == null)
                {
                    _invCodes = new InventoryCodes();
                    _invCodes.LoadFromDefaultCsvFile();
                }

                return _invCodes;
            }
        }

        static void Main(string[] args)
        {
            Program program = new Program();

            int exitCode = program.Run(args);
            Environment.Exit(exitCode);
        }

        Program()
        {
            _refillableTech = new HashSet<string>(REFILLABLE_TECH);
            _random = new Random();
            LogWriter = Console.Out;
        }

        public TextWriter LogWriter { get; set; }

        public bool Verbose { get; set; }


        public int Run(IEnumerable<string> args)
        {
            bool success = true;

            try
            {
                var result = CommandLine.Parser.Default.ParseArguments<DecryptOptions, EncryptOptions, ModifyOptions, InfoOptions>(args);

                success = result.MapResult(
                    (DecryptOptions opt) => RunDecrypt(opt),
                    (EncryptOptions opt) => RunEncrypt(opt),
                    (ModifyOptions opt) => RunModify(opt),
                    (InfoOptions opt) => RunInfo(opt),
                    _ => false);
            }
            catch (Exception x)
            {
                Console.Error.WriteLine(x.Message);
                success = false;
            }

            return success ? 0 : -1;
        }

        private bool RunInfo(InfoOptions opt)
        {
            try
            {
                DoCommon(opt);

                GameSaveDir gsd;
                try
                {
                    gsd = new GameSaveDir(opt.SaveDir);
                }
                catch (Exception x)
                {
                    LogError("Error locating game save file:\n{0}", x.Message);
                    return false;
                }

                dynamic json;
                try
                {
                    json = ReadLatestSaveFile(gsd, opt.GameMode);
                }
                catch (Exception x)
                {
                    LogError("Error loading or parsing save file:\n{0}", x.Message);
                    return false;
                }

                // Dump basic player info
                InfoBasic(opt, json);

                // Dump inventory info
                InfoInventory(opt, json);
            }
            catch (Exception x)
            {
                LogError(x.Message);
                return false;
            }

            return true;
        }

        private void InfoBasic(InfoOptions opt, dynamic json)
        {
            if (!opt.NoBasic)
            {
                var galacticAddress = json.PlayerStateData.UniverseAddress.GalacticAddress;
                var coordinates = new NmsVoxelCoordinates((int)galacticAddress.VoxelX, (int)galacticAddress.VoxelY, (int)galacticAddress.VoxelZ, (int)galacticAddress.SolarSystemIndex);
                Log("Save file for game mode: {0}", opt.GameMode);
                Log("  Save file version: {0}", json.Version);
                Log("  Platform: {0}", json.Platform);
                Log("  Health: {0}", json.PlayerStateData.Health);
                Log("  Ship Health: {0}", json.PlayerStateData.ShipHealth);
                Log("  Shield: {0}", json.PlayerStateData.Shield);
                Log("  Ship Shield: {0}", json.PlayerStateData.ShipShield);
                Log("  Energy: {0}", json.PlayerStateData.Energy);
                Log("  Units: {0:N0}", json.PlayerStateData.Units);
                Log("  Coordinates (x,y,z,ssi): {0}", coordinates.ToVoxelCoordinateString());
                Log("  Coordinates (galactic): {0}", coordinates.ToGalacticCoordinateString());
                Log("  Coordinates (portal): {0}", coordinates.ToPortalCoordinateString());
            }
        }

        private void InfoInventory(InfoOptions opt, dynamic json)
        {
            if (opt.ShowInventory)
            {
                if (opt.InventoryGroups.Contains(InvGrp.all) || opt.InventoryGroups.Contains(InvGrp.exosuit) || opt.InventoryGroups.Contains(InvGrp.exosuit_general))
                {
                    // Dump exosuit general inventory
                    InfoInventoryGroup("Exosuit General", SuitInventoryGeneralNode(json), opt.InventoryTypes);
                }
                if (opt.InventoryGroups.Contains(InvGrp.all) || opt.InventoryGroups.Contains(InvGrp.exosuit) || opt.InventoryGroups.Contains(InvGrp.exosuit_cargo))
                {
                    // Dump exosuit cargo inventory
                    InfoInventoryGroup("Exosuit Cargo", SuitInventoryCargoNode(json), opt.InventoryTypes);
                }
                if (opt.InventoryGroups.Contains(InvGrp.all) || opt.InventoryGroups.Contains(InvGrp.exosuit) || opt.InventoryGroups.Contains(InvGrp.exosuit_tech))
                {
                    // Dump exosuit tech inventory
                    InfoInventoryGroup("Exosuit Tech-Only", SuitInventoryTechOnlyNode(json), opt.InventoryTypes);
                }
                if (opt.InventoryGroups.Contains(InvGrp.all) || opt.InventoryGroups.Contains(InvGrp.ship) || opt.InventoryGroups.Contains(InvGrp.ship_general))
                {
                    // Dump ship general inventory
                    InfoInventoryGroup("Ship General", PrimaryShipInventoryGeneralNode(json), opt.InventoryTypes);
                }
                if (opt.InventoryGroups.Contains(InvGrp.all) || opt.InventoryGroups.Contains(InvGrp.ship) || opt.InventoryGroups.Contains(InvGrp.ship_tech))
                {
                    // Dump ship techonly inventory
                    InfoInventoryGroup("Ship Tech-Only", PrimaryShipInventoryTechOnlyNode(json), opt.InventoryTypes);
                }
                if (opt.InventoryGroups.Contains(InvGrp.all) || opt.InventoryGroups.Contains(InvGrp.freighter) || opt.InventoryGroups.Contains(InvGrp.freighter_general))
                {
                    // Dump freighter general inventory
                    InfoInventoryGroup("Freighter General", json.PlayerStateData.FreighterInventory, opt.InventoryTypes);
                }
                if (opt.InventoryGroups.Contains(InvGrp.all) || opt.InventoryGroups.Contains(InvGrp.freighter) || opt.InventoryGroups.Contains(InvGrp.freighter_tech))
                {
                    // Dump freighter tech-only inventory
                    InfoInventoryGroup("Freighter Tech-Only", json.PlayerStateData.FreighterInventory_TechOnly, opt.InventoryTypes);
                }
                if (opt.InventoryGroups.Contains(InvGrp.all) || opt.InventoryGroups.Contains(InvGrp.multitool))
                {
                    // Dump multitool inventory
                    InfoInventoryGroup("Weapon", json.PlayerStateData.WeaponInventory, opt.InventoryTypes);
                }
            }
        }

        private void InfoInventoryGroup(string groupName, dynamic json, IEnumerable<InvType> types)
        {
            if (null == json)
            {
                return;
            }

            int width = (int)json.Width;
            int height = (int)json.Height;

            bool showAllNonEmpty = types.Contains(InvType.all) || types.Contains(InvType.all_but_empty);
            bool showNonTech = types.Contains(InvType.non_tech);
            bool showEmpty = types.Contains(InvType.empty) || types.Contains(InvType.all);
            bool showTech = types.Contains(InvType.tech) || showAllNonEmpty;
            bool showProduct = types.Contains(InvType.product) || showAllNonEmpty || showNonTech;
            bool showSubstance = types.Contains(InvType.substance) || showAllNonEmpty || showNonTech;

            Log("Inventory group: {0}, [H,W]=[{1},{2}]", groupName, height, width);

            dynamic[,] slots = new dynamic[height, width];
            foreach (var slot in json.Slots)
            {
                slots[slot.Index.Y, slot.Index.X] = slot;
            }

            for (int row = 0; row < height; ++row)
            {
                for (int col = 0; col < width; ++col)
                {
                    var slot = slots[row, col];

                    if (slot != null)
                    {
                        InventoryCode invCode = InvCodes[slot.Id.Value];
                        if (slot.Type.InventoryType == "Technology" && showTech)
                        {
                            if ((float)slot.DamageFactor != 0.0)
                            {
                                Log("  [{0},{1}] {3} ({2}), damage: {4:p0} <{5}>", row + 1, col + 1, invCode.Name, FormatSlotId(slot.Id), (float)slot.DamageFactor*100.0, invCode.Type);
                            }
                            else
                            {
                                Log("  [{0},{1}] {3} ({2}) <{4}>", row + 1, col + 1, FormatSlotId(slot.Id), invCode.Name, invCode.Type);
                            }                            
                        }
                        else if (slot.Type.InventoryType == "Substance" && showSubstance)
                        {
                            Log("  [{0},{1}] {3} ({2}), {4}/{5}  <{6}>", row + 1, col + 1, FormatSlotId(slot.Id), invCode.Name, (int)slot.Amount, (int)slot.MaxAmount, invCode.Type);
                        }
                        else if (slot.Type.InventoryType == "Product" && showProduct)
                        {
                            Log("  [{0},{1}] {3} ({2}), {4}/{5} <{6}>", row + 1, col + 1, FormatSlotId(slot.Id), invCode.Name, (int)slot.Amount, (int)slot.MaxAmount, invCode.Type);
                        }
                    }
                    else if (showEmpty)
                    {
                        Log("  [{0},{1}] <Empty>", row + 1, col + 1);
                    }
                }
            }
        }

        private string FormatSlotId(dynamic id)
        {
            string idStr = (string)id;
            if (idStr == null)
            {
                return string.Empty;
            }

            if (idStr.StartsWith("^"))
            {
                return idStr.Substring(1);
            }

            return idStr;
        }

        private string ProductSlotDescription(dynamic slot)
        {
            throw new NotImplementedException();
        }

        private string SubstanceSlotDescription(dynamic slot)
        {
            throw new NotImplementedException();
        }

        private string TechnologySlotDescription(dynamic slot)
        {
            throw new NotImplementedException();
        }

        private void DoCommon(CommonOptions opt)
        {
            if (!Verbose)
            {
                Verbose = opt.Verbose;
            }

            LogVerbose("CLR version: {0}", Environment.Version);
            LogVerbose("APPDATA folder: {0}", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
        }

        private bool RunDecrypt(DecryptOptions opt)
        {
            DoCommon(opt);

            GameSaveDir gsd;
            try
            {
                gsd = new GameSaveDir(opt.SaveDir);
            }
            catch (Exception x)
            {
                LogError("Error locating game save file:\n{0}", x.Message);
                return false;
            }

            return DecryptLatestTo(gsd, opt.GameMode, opt.OutputPath);
        }

        private bool DecryptLatestTo(GameSaveDir gsd, GameModes gameMode, string outputPath)
        {
            object json;
            try
            {
                json = ReadLatestSaveFile(gsd, gameMode);
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

            LogVerbose("Writing formatted JSON to:\n   {0}", outputPath);
            try
            {
                File.WriteAllText(outputPath, formattedJson);
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

            GameSaveDir gsd;
            try
            {
                gsd = new GameSaveDir(opt.SaveDir);
            }
            catch (Exception x)
            {
                LogError("Error locating game save file:\n{0}", x.Message);
                return false;
            }

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

            BackupSave(gsd, opt);

            try
            {
                WriteLatestSaveFile(gsd, opt.GameMode, json, opt.UseOldFormat);
            }
            catch (Exception x)
            {
                LogError("Error storing save file: {0}", x.Message);
                return false;
            }

            return true;
        }

        private static ulong ParseUlongOption(string str)
        {
            if (str.StartsWith("0x") || str.StartsWith("0X"))
            {
                return Convert.ToUInt64(str, 16);
            }
            else
            {
                return Convert.ToUInt64(str);
            }
        }

        private dynamic PrimaryShipNode(dynamic json)
        {
            int primaryShipIndex = json.PlayerStateData.PrimaryShip;
            return json.PlayerStateData.ShipOwnership[primaryShipIndex];
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
        private object ReadLatestSaveFile(GameSaveDir gsd, GameModes gameMode)
        {
            string metadataPath;
            string storagePath;
            uint archiveNumber;
            ulong? profileKey;

            gsd.FindLatestGameSaveFiles(gameMode, out metadataPath, out storagePath, out archiveNumber, out profileKey);

            LogVerbose("Reading latest {0}-mode save game file from:\n   {1}", gameMode, storagePath);

            string jsonStr = Storage.Read(metadataPath, storagePath, archiveNumber, profileKey);

            return JsonConvert.DeserializeObject(jsonStr);
        }

        private void WriteLatestSaveFile(GameSaveDir gsd, GameModes gameMode, object json, bool useOldFormat)
        {
            string formattedJson = JsonConvert.SerializeObject(json, Formatting.None);

            string metadataPath;
            string storagePath;
            uint archiveNumber;
            ulong? profileKey;

            gsd.FindLatestGameSaveFiles(gameMode, out metadataPath, out storagePath, out archiveNumber, out profileKey);

            LogVerbose("Writing latest {0}-mode save game file to:\n   {1}", gameMode, storagePath);
            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(formattedJson)))
            {
                Storage.Write(metadataPath, storagePath, ms, archiveNumber, profileKey, useOldFormat);
                var now = DateTime.Now;
                File.SetLastWriteTime(metadataPath, now);
                File.SetLastWriteTime(storagePath, now);
            }
        }

        private void BackupSave(GameSaveDir gsd, BackupOptions opt)
        {
            if (null != opt.BackupDir)
            {
                try
                {
                    var baseName = string.Format("nmssavetool-backup-{0}-{1}", opt.GameMode, gsd.FindMostRecentSaveDateTime().ToString("yyyyMMdd-HHmmss"));
                    var basePath = Path.Combine(opt.BackupDir, baseName);
                    var zipPath = basePath + ".zip";
                                        
                    if (gsd.ArchiveTo(zipPath))
                    {
                        Log("Backed up save game files to: {0}", zipPath);

                        var jsonPath = basePath + ".json";
                        DecryptLatestTo(gsd, opt.GameMode, jsonPath);
                        Log("Backed up decrypted JSON to: {0}", jsonPath);
                    }
                    else
                    {
                        Log("Skipping backup because backup file already exists: {0}", zipPath);
                    }
                }
                catch (Exception x)
                {
                    throw new Exception(string.Format("Error backing up save game files: {0}", x.Message), x);
                }
            }
        }

        private dynamic SuitInventoryGeneralNode(dynamic json)
        {
            return json.PlayerStateData.Inventory;
        }
        private dynamic SuitInventoryTechOnlyNode(dynamic json)
        {
            return json.PlayerStateData.Inventory_TechOnly;
        }
        private dynamic SuitInventoryCargoNode(dynamic json)
        {
            return json.PlayerStateData.Inventory_Cargo;
        }

        private dynamic WeaponInventoryNode(dynamic json)
        {
            return json.PlayerStateData.WeaponInventory;
        }

        private dynamic PrimaryShipInventoryGeneralNode(dynamic json)
        {
            return PrimaryShipNode(json).Inventory;
        }

        private dynamic PrimaryShipInventoryTechOnlyNode(dynamic json)
        {
            return PrimaryShipNode(json).Inventory_TechOnly;
        }

        private dynamic FreighterInventoryNode(dynamic json)
        {
            return json.PlayerStateData.FreighterInventory;
        }

        private bool RunModify(ModifyOptions opt)
        {
            try
            {
                DoCommon(opt);

                GameSaveDir gsd;
                try
                {
                    gsd = new GameSaveDir(opt.SaveDir);
                }
                catch (Exception x)
                {
                    LogError("Error locating game save file:\n{0}", x.Message);
                    return false;
                }

                dynamic json;
                try
                {
                    json = ReadLatestSaveFile(gsd, opt.GameMode);
                }
                catch (Exception x)
                {
                    LogError("Error loading or parsing save file: {0}", x.Message);
                    return false;
                }

                // Now iterate through JSON, maxing out technology, Substance, and Product values in Inventory, ShipInventory, and FreighterInventory

                ModifyExosuitSlots(opt, json);
                ModifyMultitoolSlots(opt, json);
                ModifyShipSlots(opt, json);
                ModifyFreighterSlots(opt, json);
                ModifyContainerSlots(opt, json);
                ModifyShipSeed(opt, json);
                ModifyMultitoolSeed(opt, json);
                ModifyFreighterSeed(opt, json);
                ModifyUnits(opt, json);
                ModifyCoordinates(opt, json);
                ModifyGalaxy(opt, json);

                BackupSave(gsd, opt);

                try
                {
                    WriteLatestSaveFile(gsd, opt.GameMode, json, opt.UseOldFormat);
                }
                catch (Exception x)
                {
                    throw new Exception(string.Format("Error storing save file: {0}", x.Message), x);
                }
            }
            catch (Exception x)
            {
                LogError(x.Message);
                return false;
            }

            return true;
        }

        private void ModifySlot(ModifyOptions opt, dynamic slot)
        {
            if (opt.Repair || opt.Everything)
            {
                slot.DamageFactor = 0.0f;
            }

            if ((opt.Energy || opt.Everything) && slot.Type.InventoryType == "Technology" && _refillableTech.Contains(slot.Id.Value))
            {
                slot.Amount = slot.MaxAmount;
            }

            if ((opt.Inventory || opt.Everything) && (slot.Type.InventoryType == "Product" || slot.Type.InventoryType == "Substance"))
            {
                slot.Amount = slot.MaxAmount;
            }
        }

        private void ModifySlots(ModifyOptions opt, dynamic slots)
        {
            foreach (var slot in slots)
            {
                ModifySlot(opt, slot);
            }
        }

        private void ModifyExosuitSlots(ModifyOptions opt, dynamic json)
        {
            if (opt.TechGroups.Contains(TechGrp.exosuit))
            {
                LogVerbose("Updating Exosuit");
                if (opt.Energy || opt.Everything)
                {
                    json.PlayerStateData.Health = 8;
                    json.PlayerStateData.Energy = 100;
                    json.PlayerStateData.Shield = 100;
                }

                ModifySlots(opt, SuitInventoryGeneralNode(json).Slots);
                ModifySlots(opt, SuitInventoryTechOnlyNode(json).Slots);
                ModifySlots(opt, SuitInventoryCargoNode(json).Slots);
            }
        }

        private void ModifyMultitoolSlots(ModifyOptions opt, dynamic json)
        {
            if (opt.TechGroups.Contains(TechGrp.multitool))
            {
                LogVerbose("Updating Multitool");
                foreach (var slot in WeaponInventoryNode(json).Slots)
                {
                    if (opt.Repair || opt.Everything)
                    {
                        slot.DamageFactor = 0.0f;
                    }

                    if ((opt.Energy || opt.Everything) && _refillableTech.Contains(slot.Id.Value))
                    {
                        slot.Amount = slot.MaxAmount;
                    }
                }
            }
        }

        private void ModifyShipSlots(ModifyOptions opt, dynamic json)
        {
            if (opt.TechGroups.Contains(TechGrp.ship))
            {
                LogVerbose("Updating Ship");
                if (opt.Energy || opt.Everything)
                {
                    json.PlayerStateData.ShipHealth = 8;
                    json.PlayerStateData.ShipShield = 200;
                }

                ModifySlots(opt, PrimaryShipInventoryGeneralNode(json).Slots);
                ModifySlots(opt, PrimaryShipInventoryTechOnlyNode(json).Slots);
            }
        }

        private void ModifyFreighterSlots(ModifyOptions opt, dynamic json)
        {
            if (opt.TechGroups.Contains(TechGrp.freighter))
            {
                LogVerbose("Updating Freighter");
                foreach (var slot in FreighterInventoryNode(json).Slots)
                {
                    if ((opt.Inventory || opt.Everything) &&
                        // Leave this next line in as protection against future version of NMS allowing other things in Freighter
                        (slot.Type.InventoryType == "Product" || slot.Type.InventoryType == "Substance")
                       )
                    {
                        slot.Amount = slot.MaxAmount;
                    }
                }
            }
        }

        private void ModifyContainerSlots(ModifyOptions opt, dynamic json)
        {
            if (opt.TechGroups.Contains(TechGrp.container))
            {
                LogVerbose("Updating Containers");
                for (int containerNum = 1; containerNum <= 10; ++containerNum)
                {
                    string containerName = string.Format("Chest{0}Inventory", containerNum);
                    var container = json.PlayerStateData[containerName];
                    if (container != null)
                    {
                        ModifySlots(opt, container.Slots);
                    }
                }
            }
        }

        private void ModifyShipSeed(ModifyOptions opt, dynamic json)
        {
            ulong? seed = null;

            if (opt.SetShipSeed != null)
            {
                try
                {
                    seed = ParseUlongOption(opt.SetShipSeed);
                }
                catch(Exception x)
                {
                    throw new ArgumentException(string.Format("Invalid value for option {0}: {1}", "--set-ship-seed", opt.SetShipSeed), x);
                }
            }
            else if (opt.RandomizeShipSeed)
            {
                byte[] randBytes = new byte[8];
                _random.NextBytes(randBytes);
                seed = BitConverter.ToUInt64(randBytes, 0);
            }

            if (seed != null)
            {
                string seedStr = string.Format("0x{0:X16}", seed);
                LogVerbose("Setting ship seed to: {0}", seedStr);
                PrimaryShipNode(json).Resource.Seed[1] = seedStr;
            }
        }

        private void ModifyMultitoolSeed(ModifyOptions opt, dynamic json)
        {
            ulong? seed = null;

            if (opt.SetMultitoolSeed != null)
            {
                try
                {
                    seed = ParseUlongOption(opt.SetShipSeed);
                }
                catch (Exception x)
                {
                    throw new ArgumentException(string.Format("Invalid value for option {0}: {1}", "--nodify-multitool-seed", opt.SetMultitoolSeed), x);
                }
            }
            else if (opt.RandomizeMultitoolSeed)
            {
                byte[] randBytes = new byte[8];
                _random.NextBytes(randBytes);
                seed = BitConverter.ToUInt64(randBytes, 0);
            }

            if (seed != null)
            {
                string seedStr = string.Format("0x{0:X16}", seed);
                LogVerbose("Setting multitool seed to: {0}", seedStr);
                json.PlayerStateData.CurrentWeapon.GenerationSeed[1] = seedStr;
            }
        }

        private void ModifyUnits(ModifyOptions opt, dynamic json)
        {
            if (opt.SetUnits.HasValue)
            {
                int newUnits = (int)Math.Min((uint)int.MaxValue, opt.SetUnits.Value);
                LogVerbose("Setting Units to: {0}", newUnits);
                json.PlayerStateData.Units = newUnits;
            }
            else if (opt.AddUnits.HasValue && opt.AddUnits.Value != 0)
            {
                int currentUnits = json.PlayerStateData.Units;
                if (opt.AddUnits.Value < 0)
                {
                    int newUnits = (int)Math.Max(0L, (Int64)currentUnits + (Int64)opt.AddUnits.Value);
                    LogVerbose("Adding {0} Units. New value: {1}", opt.AddUnits.Value, newUnits);
                    json.PlayerStateData.Units = newUnits;
                }
                else
                {
                    int newUnits = (int)Math.Min((uint)int.MaxValue, (uint)currentUnits + (uint)opt.AddUnits.Value);
                    LogVerbose("Adding {0} Units. New value: {1}", opt.AddUnits.Value, newUnits);
                    json.PlayerStateData.Units = newUnits;
                }
            }
        }

        private void ModifyFreighterSeed(ModifyOptions opt, dynamic json)
        {
            ulong? seed = null;

            if (opt.SetFreighterSeed != null)
            {
                try
                {
                    seed = ParseUlongOption(opt.SetFreighterSeed);
                }
                catch (Exception x)
                {
                    throw new ArgumentException(string.Format("Invalid value for option {0}: {1}", "--modify-freighter-seed", opt.SetFreighterSeed), x);
                }
            }
            else if (opt.RandomizeFreighterSeed)
            {
                byte[] randBytes = new byte[8];
                _random.NextBytes(randBytes);
                seed = BitConverter.ToUInt64(randBytes, 0);
            }

            if (seed != null)
            {
                string seedStr = string.Format("0x{0:X16}", seed);
                LogVerbose("Setting freightert seed to: {0}", seedStr);
                json.PlayerStateData.CurrentFreighter.Seed[1] = seedStr;
            }
        }

        private void ModifyCoordinates(ModifyOptions opt, dynamic json)
        {
            if (opt.SetGalacticCoordinates != null)
            {
                SetCoordinates(opt, json, NmsVoxelCoordinates.FromGalacticCoordinateString(opt.SetGalacticCoordinates), opt.SetGalacticCoordinates);
            }
            else if (opt.SetPortalCoordinates != null)
            {
                SetCoordinates(opt, json, NmsVoxelCoordinates.FromPortalCoordinateString(opt.SetPortalCoordinates), opt.SetPortalCoordinates);
            }
            else if (opt.SetVoxelCoordinates != null)
            {
                SetCoordinates(opt, json, NmsVoxelCoordinates.FromVoxelCoordinateString(opt.SetVoxelCoordinates), opt.SetVoxelCoordinates);
            }
        }

        private void ModifyGalaxy(ModifyOptions opt, dynamic json)
        {
            if (opt.SetGalaxy.HasValue)
            {
                LogVerbose("Changing player galaxy from {0} to {1}", json.PlayerStateData.UniverseAddress.RealityIndex, opt.SetGalaxy.Value);
                json.PlayerStateData.UniverseAddress.RealityIndex = opt.SetGalaxy.Value;
                LogVerbose("Changing LastKnownPlayerState from {0} to InShip", json.SpawnStateData.LastKnownPlayerState);
                json.SpawnStateData.LastKnownPlayerState = "InShip";
            }       
        }

        private void SetCoordinates(ModifyOptions opt, dynamic json, NmsVoxelCoordinates coordinates, string cordinatesStr)
        {
            var galacticAddress = json.PlayerStateData.UniverseAddress.GalacticAddress;
            if (opt.Verbose)
            {
                var originalCoordinates = new NmsVoxelCoordinates((int)galacticAddress.VoxelX, (int)galacticAddress.VoxelY, (int)galacticAddress.VoxelZ, (int)galacticAddress.SolarSystemIndex);
                LogVerbose("Changing player coordinates from:\n  {0}\nto:\n  {1}\n  {2}", originalCoordinates, cordinatesStr, coordinates);
            }
            galacticAddress.VoxelX = coordinates.X;
            galacticAddress.VoxelY = coordinates.Y;
            galacticAddress.VoxelZ = coordinates.Z;
            galacticAddress.SolarSystemIndex = coordinates.SolarSystemIndex;
            galacticAddress.PlanetIndex = 0;
            LogVerbose("Changing LastKnownPlayerState from {0} to InShip", json.SpawnStateData.LastKnownPlayerState);
            json.SpawnStateData.LastKnownPlayerState = "InShip";
        }
    }
}
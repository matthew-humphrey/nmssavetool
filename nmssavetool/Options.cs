using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using CommandLine;

namespace nmssavetool
{
    public enum InvGrps
    {
        all,
        exosuit,
        multitool,
        ship,
        freighter,
        vehicle,
        container
    }

    public enum InvSubGrps
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
        vehicle,
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

    public enum ItemInvGroups
    {
        exosuit_general,
        exosuit_cargo,
        exosuit_techonly,
        ship_general,
        ship_techonly,
        freighter_general,
        freighter_techonly,
        vehicle,
        multitool
    }

    public enum SeedTargets
    {
        ship,
        multitool,
        freighter
    }


    public class InvCoorOpt
    {
        public int X { get; }
        public int Y { get; }

        public InvCoorOpt(string option)
        {
            string[] parts = option.Split(',');
            if (parts.Length != 2)
            {
                throw new ArgumentException(string.Format("Invalid inventory coordinates value: {0}", option));
            }

            int row;
            if (!int.TryParse(parts[0], out row) || row < 1)
            {
                throw new ArgumentException(string.Format("Invalid row value in coordinates: {0}", parts[0]));
            }
            Y = row - 1;

            int col;
            if (!int.TryParse(parts[1], out col) || col < 1)
            {
                throw new ArgumentException(string.Format("Invalid col value in coordinates: {0}", parts[1]));
            }
            X = col - 1;
        }

        public override string ToString()
        {
            return string.Format("{0},{1}", Y + 1, X + 1);
        }
    }

    public class InvCoorPairOpt
    {
        public int X1 { get; }
        public int Y1 { get; }

        public int X2 { get; }
        public int Y2 { get; }

        public InvCoorPairOpt(string option)
        {
            var match = Regex.Match(option, @"(?<R1>\d+),(?<C1>\d+):(?<R2>\d+),(?<C2>\d+)");

            if (!match.Success)
            {
                throw new ArgumentException(string.Format("Invalid inventory coordinate pair value: {0}", option));
            }

            int intVal;
            string strVal;

            strVal = match.Groups["R1"].Value;
            if (!int.TryParse(strVal, out intVal) || intVal < 1)
            {
                throw new ArgumentException(string.Format("Invalid value for the first row value in a coordinate pair: {0}", strVal));
            }
            Y1 = intVal - 1;

            strVal = match.Groups["C1"].Value;
            if (!int.TryParse(strVal, out intVal) || intVal < 1)
            {
                throw new ArgumentException(string.Format("Invalid value for the first column value in a coordinate pair: {0}", strVal));
            }
            X1 = intVal - 1;

            strVal = match.Groups["R2"].Value;
            if (!int.TryParse(strVal, out intVal) || intVal < 1)
            {
                throw new ArgumentException(string.Format("Invalid value for the second row value in a coordinate pair: {0}", strVal));
            }
            Y2 = intVal - 1;

            strVal = match.Groups["C2"].Value;
            if (!int.TryParse(strVal, out intVal) || intVal < 1)
            {
                throw new ArgumentException(string.Format("Invalid value for the second column value in a coordinate pair: {0}", strVal));
            }
            X2 = intVal - 1;
        }

        public override string ToString()
        {
            return string.Format("{0},{1}:{2},{3}", Y1 + 1, X1 + 1, Y2 + 1, X2 + 1);
        }
    }

    public class CommonOptions
    {
        [Option("save-dir", Required = false, HelpText = "Path to game save folder (optional - determined automatically if not provided)")]
        public string SaveDir { get; set; }

        [Option('v', "verbose", HelpText = "Displays additional information during execution.")]
        public bool Verbose { get; set; }
    }

    public class GameSlotOptions : CommonOptions
    {
        [Option('g', "game-slot", Required = true, HelpText = "Use saves for which game slot (1-5)")]
        public uint GameSlot { get; set; }
    }

    [Verb("backupall", HelpText = "Backup all game saves")]
    public class BackupAllOptions : CommonOptions
    {
        [Option('b', "backup-to", Required = true, HelpText = "Specifies the directory or file to which the backup will be written. The backup will be saved as a zip archive.")]
        public string BackupPath { get; set; }
    }

    [Verb("backup", HelpText = "Back up the latest game save.")]
    public class BackupOptions : GameSlotOptions
    {
        [Option('b', "backup-dir", Required = true, HelpText = "Specifies the directory to backup the specified game slot. The backup will be created as a decrypted JSON file in the specified directory.")]
        public string BackupDir { get; set; }

        [Option("full-backup", HelpText = "If provided (along with -b/--backup-dir), will archive the full game-save directory in addition to the decrypted JSON game-save file.")]
        public bool FullBackup { get; set; }
    }

    [Verb("restore", HelpText = "Restore the latest game save from the specified back-up file.")]
    public class RestoreOptions : GameSlotOptions
    {
        [Option('f', "restore-from", HelpText = "Specifies the full path to a back-up file to restore from. The back-up file should be a decrypted JSON file created by this program.", Required = true)]
        public string RestorePath { get; set; }
    }

    public class UpdateOptions : GameSlotOptions
    {
        [Option('b', "backup-dir", HelpText = "If provided, will write the selected game-save to a decrypted JSON file in the specified directory.")]
        public string BackupDir { get; set; }

        [Option("full-backup", HelpText = "If provided (along with -b/--backup-dir), will archive the full game-save directory in addition to the decrypted JSON game-save file.")]
        public bool FullBackup { get; set; }
    }

    [Verb("decrypt", HelpText = "Decrypt the latest game save slot and write it to a formatted JSON file.")]
    public class DecryptOptions : GameSlotOptions
    {
        [Option('f', "output-file", HelpText = "Specifies the file to which the decrypted, formatted game save will be written.", Required = true)]
        public string OutputPath { get; set; }
    }

    [Verb("encrypt", HelpText = "Encrypt a previously decrypted save-game JSON file and replace the latest game save.")]
    public class EncryptOptions : UpdateOptions
    {
        [Option('f', "input-file", HelpText = "Specifies the JSON input file which will be encrypted and written to the latest game save slot.", Required = true)]
        public string InputPath { get; set; }
    }

    [Verb("relocate", HelpText = "Set the player position within the NMS universe using various coordinate systems.")]
    public class RelocateOptions : UpdateOptions
    {
        [Option('c', "galactic-coordinates", SetName = "coordinates", HelpText = "Set the player position using the galactic coordinates displayed by signal scanners.")]
        public string GalacticCoordinates { get; set; }

        [Option('p', "portal-coordinates", SetName = "coordinates", HelpText = "Set the player position using portal coordinates.")]
        public string PortalCoordinates { get; set; }

        [Option("voxel-coordinates", SetName = "coordinates", HelpText = "Set the player position using the voxel coordinates used within the save-game file. Format is (x,y,z,ssi).")]
        public string VoxelCoordinates { get; set; }

        [Option("galaxy", HelpText = "Set the galaxy index (0 = Euclid Galaxy, 1 = Hilbert Dimension, 2 = Calypso Galaxy, etc.)")]
        public int? Galaxy { get; set; }

        [Option("planet", SetName = "planet", HelpText = "Set the planet index")]
        public int? Planet { get; set; }

        [Option("no-reset-planet", SetName = "planet", HelpText = "Normally when relocating the player, the player's planet value is set to zero, so that the player's position is compatible with all star systems. Set this flag to disable that behavior.")]
        public bool SkipPlanetZero { get; set; }

        [Option("no-reset-to-ship", HelpText = "Normally when relocating the player, the player's last known state value is set to 'InShip' so the player spawns inside his ship. Set this flag to disable that behavior.")]
        public bool SkipInShip { get; set; }
    }

    [Verb("repair", HelpText = "Repair damaged technology in the exosuit, multitool, ship, vehicle, or freighter inventories.")]
    public class RepairOptions : UpdateOptions
    {
        [Option('c', "inventory-groups", Separator = '+', Max = 5, Default = new InvGrps[] { InvGrps.exosuit, InvGrps.ship, InvGrps.multitool },
            HelpText = "Which inventories to repair.")]
        public IEnumerable<InvGrps> Groups { get; set; }
    }

    [Verb("refill", HelpText = "Maximize amounts of product and substance items in the exosuit, multitool, ship, vehicle, or freighter inventories.")]
    public class RefillOptions : UpdateOptions
    {
        [Option('c', "inventory-groups", Separator = '+', Max = 5, Default = new InvGrps[] { InvGrps.exosuit, InvGrps.ship, InvGrps.freighter, InvGrps.vehicle, InvGrps.container },
            HelpText = "Which inventories to refill.")]
        public IEnumerable<InvGrps> Groups { get; set; }
    }

    [Verb("recharge", HelpText = "Recharge shield, energy and fuel levels in the exosuit, multitool, ship, freighter, or vehicle inventories.")]
    public class RechargeOptions : UpdateOptions
    {
        [Option('c', "inventory-groups", Separator = '+', Max = 5, Default = new InvGrps[] { InvGrps.exosuit, InvGrps.ship, InvGrps.multitool, InvGrps.freighter, InvGrps.vehicle},
            HelpText = "Which inventories to recharge.")]
        public IEnumerable<InvGrps> Groups { get; set; }
    }

    [Verb("refurbish", HelpText = "Recharge, repair, and refill items in the exosuit, multitool, ship, and vehicle inventories.")]
    public class RefurbishOptions : UpdateOptions
    {
        [Option('c', "inventory-groups", Separator = '+', Max = 5, Default = new InvGrps[] { InvGrps.exosuit, InvGrps.ship, InvGrps.multitool, InvGrps.freighter, InvGrps.vehicle, InvGrps.container },
            HelpText = "Which inventories to refurbish.")]
        public IEnumerable<InvGrps> Groups { get; set; }
    }

    [Verb("units", HelpText = "Change the amount of units (in-game money).")]
    public class UnitsOptions : UpdateOptions
    {
        [Option('s', "set-units", SetName = "units", HelpText = "Set the player Units.")]
        public uint? SetUnits { get; set; }

        [Option('a', "add-units", SetName = "units", HelpText = "Add the specified amount to player Units (negative units will subtract from total).")]
        public int? AddUnits { get; set; }
    }

    [Verb("seed", HelpText = "Change the RNG seed value that is used to determine the appearance of the ship, multitool, or freighter.")]
    public class SeedOptions : UpdateOptions
    {
        [Option('c', "apply-to", Required = true, HelpText = "Specifies which object whose RNG seed will be changed: ship, multitool, or freighter")]
        public SeedTargets Target { get; set; }

        [Option('r', "randomize-ship-seed", SetName = "seed", HelpText = "Generate a random seed.")]
        public bool RandomSeed { get; set; }

        [Option('s', "set-ship-seed", SetName = "seed", HelpText = "Set the seed value.")]
        public string SetSeed { get; set; }
    }

    [Verb("maxslots", HelpText = "Maximize the number of inventory slots.")]
    public class MaxSlotsOptions : UpdateOptions
    {
        [Option('c', "inventory-groups", HelpText = "The inventory groups whose slots will be maximized", Default = new InvSubGrps[] { InvSubGrps.all})]
        public IEnumerable<InvSubGrps> Group { get; set; }
    }

    [Verb("addinv", HelpText = "Adds an inventory item.")]
    public class AddInventoryOptions : UpdateOptions
    {
        [Option('i', "item", Required = true, HelpText = "Specifies the inventory item to add. You may specify a portion of the name and the program will try and match with one of the valid items. Surround the name in quotes, and prefix the name with a '^' to match against item IDs instead of item descriptions. Choose which inventory to add the item to with the add-item-to option.")]
        public string Item { get; set; }

        [Option('c', "inventory-group", Required = true, Default = ItemInvGroups.exosuit_general, HelpText = "Specifies the inventory group to which the item will be added: exosuit_general, exosuit_cargo, ship_general, freighter_general, or vehicle")]
        public ItemInvGroups Group { get; set; }
    }

    [Verb("setinv", HelpText = "Adds an inventory item to a specific position.")]
    public class SetInventoryOptions : UpdateOptions
    {
        [Option('i', "item", Required = true, HelpText = "Specifies the inventory item to add. You may specify a portion of the name and the program will try and match with one of the valid items. Surround the name in quotes, and prefix the name with a '^' to match against item IDs instead of item descriptions. Choose which inventory to add the item to with the add-item-to option.")]
        public string Item { get; set; }

        [Option('p', "position", HelpText = "Specifies the position, as row,col, at which the inventory item will be placed. Valid row and column value start at 1.")]
        public InvCoorOpt Position { get; set; }

        [Option('c', "inventory-group", Required = true, Default = ItemInvGroups.exosuit_general, HelpText = "Specifies the inventory group to which the item will be added: exosuit_general, exosuit_cargo, ship_general, freighter_general, or vehicle")]
        public ItemInvGroups Group { get; set; }
    }

    [Verb("moveinv", HelpText = "Moves an inventory item from one slot to another. Anything in the destination slot is destroyed.")]
    public class MoveInventoryOptions : UpdateOptions
    {
        [Option('p', "position", Required = true, HelpText = "Specifies the from- and to-position as, \"from_row,from_col:to_row,to_col\", of the item which will be moved. Valid row and column values start at '1'")]
        public InvCoorPairOpt Position { get; set; }

        [Option('c', "inventory-group", Required = true, Default = ItemInvGroups.exosuit_general, HelpText = "Specifies the inventory group within which the item will be moved: exosuit_general, exosuit_cargo, ship_general, freighter_general, or vehicle")]
        public ItemInvGroups Group { get; set; }
    }

    [Verb("swapinv", HelpText = "Swaps the contents of two inventory slots.")]
    public class SwapInventoryOptions : UpdateOptions
    {
        [Option('p', "position", Required = true, HelpText = "Specifies the positions as, \"row1,col1:row2,col2\", of the items which will be swapped. Valid row and column values start at '1'")]
        public InvCoorPairOpt Position { get; set; }

        [Option('c', "inventory-group", Required = true, Default = ItemInvGroups.exosuit_general, HelpText = "Specifies the inventory group within which the items will be swapped: exosuit_general, exosuit_cargo, ship_general, freighter_general, or vehicle")]
        public ItemInvGroups Group { get; set; }
    }

    [Verb("delinv", HelpText = "Deletes an inventory item.")]
    public class DelInventoryOptions : UpdateOptions
    {
        [Option('s', "position", Required = true, HelpText = "Specifies the position, as row,col, of the inventory item which will be deleted. Valid row and column value start at 1.")]
        public InvCoorOpt Position { get; set; }

        [Option('c', "inventory-group", Required = true, Default = ItemInvGroups.exosuit_general, HelpText = "Specifies the inventory group from which the inventory item will be deleted: exosuit_general, exosuit_cargo, ship_general, freighter_general, or vehicle")]
        public ItemInvGroups Group { get; set; }
    }

    [Verb("info", HelpText = "Display information about a game save including player stats and inventory contents.")]
    public class InfoOptions : GameSlotOptions
    {
        [Option("no-basic", Default = false, HelpText = "Omits display of basic game-save information such as player stats and position.")]
        public bool NoBasic { get; set; }

        [Option('i', "show-inventory", Default = false, HelpText = "Display inventory contents.")]
        public bool ShowInventory { get; set; }

        [Option('c', "inventory-groups", Separator = '+', Default = new InvSubGrps[] { InvSubGrps.all },
            HelpText = "The inventory groups whose contents will be displayed.")]
        public IEnumerable<InvSubGrps> InventoryGroups { get; set; }

        [Option('t', "types", Separator = '+', Default = new InvType[] { InvType.all_but_empty },
            HelpText = "Which inventory types to include (all,all_but_empty,product,substance,tech,non_tech,empty).")]
        public IEnumerable<InvType> InventoryTypes { get; set; }
    }

}

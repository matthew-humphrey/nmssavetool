using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using CommandLine;


namespace nmssavetool
{
    class Program
    {
        private Random _random;
        private GameSave _gs;
        private GameSaveManager _gsm;
        private uint _gameSlot;
        private TextWriter _log;
        private TextWriter _logVerbose;

        static void Main(string[] args)
        {
            Program program = new Program();

            int exitCode = program.Run(args);
            Environment.Exit(exitCode);
        }

        Program()
        {
            _random = new Random();
            _log = Console.Out;
            _logVerbose = Console.Out;
        }

        public TextWriter LogWriter { get; set; }

        public bool Verbose { get; set; }

        public int Run(IEnumerable<string> args)
        {
            bool success = true;

            try
            {
                var result = CommandLine.Parser.Default.ParseArguments(args, 
                    typeof(AddInventoryOptions),
                    typeof(BackupOptions),
                    typeof(BackupAllOptions),
                    typeof(DecryptOptions),
                    typeof(DelInventoryOptions),
                    typeof(EncryptOptions),
                    typeof(InfoOptions),
                    typeof(MaxSlotsOptions),
                    typeof(MoveInventoryOptions),
                    typeof(RechargeOptions),
                    typeof(RefillOptions),
                    typeof(RefurbishOptions),
                    typeof(RelocateOptions),
                    typeof(RepairOptions),
                    typeof(RestoreOptions),
                    typeof(SeedOptions),
                    typeof(SetInventoryOptions),
                    typeof(SwapInventoryOptions),
                    typeof(UnitsOptions));

                result
                    .WithParsed<AddInventoryOptions>(opt => LoadRunSave(opt, o => RunAddInventory(o)))
                    .WithParsed<BackupOptions>(opt => LoadRun(opt, o => RunBackup(o)))
                    .WithParsed<BackupAllOptions>(opt => RunBackupAll(opt))
                    .WithParsed<DecryptOptions>(opt => LoadRun(opt, o => RunDecrypt(o)))
                    .WithParsed<DelInventoryOptions>(opt => LoadRunSave(opt, o => RunDelInventory(o)))
                    .WithParsed<EncryptOptions>(opt => RunEncrypt(opt))
                    .WithParsed<InfoOptions>(opt => LoadRun(opt, o => RunInfo(o)))
                    .WithParsed<MaxSlotsOptions>(opt => LoadRunSave(opt, o => RunMaxSlots(o)))
                    .WithParsed<MoveInventoryOptions>(opt => LoadRunSave(opt, o => RunMoveInventory(o)))
                    .WithParsed<RechargeOptions>(opt => LoadRunSave(opt, o => RunRecharge(o)))
                    .WithParsed<RefillOptions>(opt => LoadRunSave(opt, o => RunRefill(o)))
                    .WithParsed<RefurbishOptions>(opt => LoadRunSave(opt, o => RunRefurbish(o)))
                    .WithParsed<RelocateOptions>(opt => LoadRunSave(opt, o => RunRelocate(o)))
                    .WithParsed<RepairOptions>(opt => LoadRunSave(opt, o => RunRepair(o)))
                    .WithParsed<RestoreOptions>(opt => RunRestore(opt))
                    .WithParsed<SeedOptions>(opt => LoadRunSave(opt, o => RunSeed(o)))
                    .WithParsed<SetInventoryOptions>(opt => LoadRunSave(opt, o => RunSetInventory(o)))
                    .WithParsed<SwapInventoryOptions>(opt => LoadRunSave(opt, o => RunSwapInventory(o)))
                    .WithParsed<UnitsOptions>(opt => LoadRunSave(opt, o => RunUnits(o)))
                    .WithNotParsed(_ => success = false);
            }
            catch (Exception x)
            {
                LogError(x.Message);
                success = false;
            }

            return success ? 0 : -1;
        }

        private void DoCommon(CommonOptions opt)
        {
            Verbose = opt.Verbose;

            if (!Verbose)
            {
                _logVerbose = TextWriter.Null;
            }

            LogVerbose("CLR version: {0}", Environment.Version);
            LogVerbose("APPDATA folder: {0}", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));

            _gsm = new GameSaveManager(opt.SaveDir, _log, _logVerbose);
        }

        private void DoGameSlotCommon(GameSlotOptions opt)
        {
            DoCommon(opt);
            _gameSlot= opt.GameSlot;
        }

        private void LoadRunSave<T>(T opt, Action<T> action) where T : UpdateOptions
        {
            DoGameSlotCommon(opt);

            GameSave gs;
            try
            {
                _gs = _gsm.ReadSaveFile(_gameSlot);
            }
            catch (Exception x)
            {
                throw new Exception(string.Format("Error loading or parsing save file: {0}", x.Message));
            }

            action(opt);

            if (opt.BackupDir != null)
            {
                try
                {
                    BackupSave(opt.BackupDir, opt.FullBackup);
                }
                catch (Exception x)
                {
                    throw new Exception(string.Format("Error backing up save game: {0}", x.Message));
                }
            }

            try
            {
                _gsm.WriteSaveFile(_gs, opt.GameSlot);

                Log("Wrote latest game save for game slot \"{0}\"", opt.GameSlot);
            }
            catch (Exception x)
            {
                throw new Exception(string.Format("Error storing save file: {0}", x.Message));
            }
        }

        private void LoadRun<T>(T opt, Action<T> action) where T : GameSlotOptions
        {
            DoGameSlotCommon(opt);

            try
            {
                _gs = _gsm.ReadSaveFile(opt.GameSlot);
            }
            catch (Exception x)
            {
                throw new Exception(string.Format("Error loading or parsing save file: {0}", x.Message));
            }

            action(opt);
        }


        #region Modify Verbs

        private void RunMaxSlots(MaxSlotsOptions opt)
        {
            var groups = new HashSet<InvSubGrps>(opt.Group);

            if (groups.Contains(InvSubGrps.all) || groups.Contains(InvSubGrps.exosuit) || groups.Contains(InvSubGrps.exosuit_general))
            {
                _gs.InventoryExosuitGeneral.MaximizeSlots();
            }
            if (groups.Contains(InvSubGrps.all) || groups.Contains(InvSubGrps.exosuit) || groups.Contains(InvSubGrps.exosuit_cargo))
            {
                _gs.InventoryExosuitCargo.MaximizeSlots();
            }
            if (groups.Contains(InvSubGrps.all) || groups.Contains(InvSubGrps.exosuit) || groups.Contains(InvSubGrps.exosuit_tech))
            {
                _gs.InventoryExosuitTechOnly.MaximizeSlots();
            }
            if (groups.Contains(InvSubGrps.all) || groups.Contains(InvSubGrps.ship) || groups.Contains(InvSubGrps.ship_general))
            {
                _gs.InventoryPrimaryShipGeneral.MaximizeSlots();
            }
            if (groups.Contains(InvSubGrps.all) || groups.Contains(InvSubGrps.ship) || groups.Contains(InvSubGrps.ship_tech))
            {
                _gs.InventoryPrimaryShipTechOnly.MaximizeSlots();
            }
            if (groups.Contains(InvSubGrps.all) || groups.Contains(InvSubGrps.freighter) || groups.Contains(InvSubGrps.freighter_general))
            {
                _gs.InventoryFreighterGeneral.MaximizeSlots();
            }
            if (groups.Contains(InvSubGrps.all) || groups.Contains(InvSubGrps.freighter) || groups.Contains(InvSubGrps.freighter_tech))
            {
                _gs.InventoryFreighterTechOnly.MaximizeSlots();
            }
            if (groups.Contains(InvSubGrps.all) || groups.Contains(InvSubGrps.vehicle))
            {
                _gs.InventoryPrimaryVehicle.MaximizeSlots();
            }
            if (groups.Contains(InvSubGrps.all) || groups.Contains(InvSubGrps.multitool))
            {
                _gs.InventoryMultitool.MaximizeSlots();
            }
        }

        private void RunSeed(SeedOptions opt)
        {
            ulong seed = 0;

            if (opt.RandomSeed)
            {
                if (opt.Target == SeedTargets.freighter)
                {
                    seed = _gs.RandomizeFreighterSeed();                    
                }
                else if (opt.Target == SeedTargets.multitool)
                {
                    seed = _gs.RandomizeMultitoolSeed();
                }
                else if (opt.Target == SeedTargets.ship)
                {
                    seed = _gs.RandomizeMultitoolSeed(); ;
                }

            }
            else if (opt.SetSeed != null)
            {
                try
                {
                    seed = ParseUlongOption(opt.SetSeed);
                }
                catch (Exception)
                {
                    throw new ArgumentException(string.Format("Invalid seed value: {0}", opt.SetSeed));
                }

                if (opt.Target == SeedTargets.freighter)
                {
                    _gs.FreighterSeed = seed;
                }
                else if (opt.Target == SeedTargets.multitool)
                {
                    _gs.MultitoolSeed = seed;
                }
                else if (opt.Target == SeedTargets.ship)
                {
                    _gs.ShipSeed = seed;
                }
            }

            Log("{0} seed set to: 0x{1:X16}", opt.Target, seed);
        }


        private void RunUnits(UnitsOptions opt)
        {
            if (opt.AddUnits.HasValue)
            {
                _gs.AddUnits(opt.AddUnits.Value);
                Log("Added {0} units. New unit total: {1}", opt.AddUnits.Value, _gs.Units);
            }
            else if (opt.SetUnits.HasValue)
            {
                if (opt.SetUnits.Value > int.MaxValue)
                {
                    throw new ArgumentException(string.Format("Invalid units value, {0}. Valid values are in the range [{1},{2}]", opt.SetUnits.Value, 0, int.MaxValue));
                }
                _gs.Units = (int)opt.SetUnits.Value;
                Log("Set units. New unit total: {0}", _gs.Units);
            }
        }

        private void RunRecharge(RechargeOptions opt)
        {
            var groups = new HashSet<InvGrps>(opt.Groups);
            var processedGroups = new List<InvGrps>(groups.Count);

            if (groups.Contains(InvGrps.all) || groups.Contains(InvGrps.exosuit))
            {
                _gs.InventoryExosuitGeneral.Recharge();
                _gs.InventoryExosuitTechOnly.Recharge();
                processedGroups.Add(InvGrps.exosuit);
            }

            if (groups.Contains(InvGrps.all) || groups.Contains(InvGrps.freighter))
            {
                _gs.InventoryFreighterGeneral.Recharge();
                _gs.InventoryFreighterTechOnly.Recharge();
                processedGroups.Add(InvGrps.freighter);
            }

            if (groups.Contains(InvGrps.all) || groups.Contains(InvGrps.multitool))
            {
                _gs.InventoryMultitool.Recharge();
                processedGroups.Add(InvGrps.multitool);
            }

            if (groups.Contains(InvGrps.all) || groups.Contains(InvGrps.ship))
            {
                _gs.InventoryPrimaryShipGeneral.Recharge();
                _gs.InventoryPrimaryShipTechOnly.Recharge();
                processedGroups.Add(InvGrps.ship);
            }

            if (groups.Contains(InvGrps.all) || groups.Contains(InvGrps.vehicle))
            {
                _gs.InventoryPrimaryVehicle.Recharge();
                processedGroups.Add(InvGrps.vehicle);
            }

            Log("Recharged items in the following inventory groups: {0}.", string.Join(", ", processedGroups));
        }

        private void RunRefill(RefillOptions opt)
        {
            var groups = new HashSet<InvGrps>(opt.Groups);
            var processedGroups = new List<InvGrps>(groups.Count);

            if (groups.Contains(InvGrps.all) || groups.Contains(InvGrps.container))
            {
                foreach (var inventory in _gs.InventoryContainers)
                {
                    inventory.Refill();
                }
                processedGroups.Add(InvGrps.container);
            }

            if (groups.Contains(InvGrps.all) || groups.Contains(InvGrps.exosuit))
            {
                _gs.InventoryExosuitGeneral.Refill();
                _gs.InventoryExosuitCargo.Refill();
                processedGroups.Add(InvGrps.exosuit);
            }

            if (groups.Contains(InvGrps.all) || groups.Contains(InvGrps.freighter))
            {
                _gs.InventoryFreighterGeneral.Refill();
                processedGroups.Add(InvGrps.freighter);
            }

            if (groups.Contains(InvGrps.all) || groups.Contains(InvGrps.ship))
            {
                _gs.InventoryPrimaryShipGeneral.Refill();
                processedGroups.Add(InvGrps.ship);
            }

            if (groups.Contains(InvGrps.all) || groups.Contains(InvGrps.vehicle))
            {
                _gs.InventoryPrimaryVehicle.Refill();
                processedGroups.Add(InvGrps.vehicle);
            }

            Log("Refilled items in the following inventory groups: {0}.", string.Join(", ", processedGroups));
        }

        private void RunRepair(RepairOptions opt)
        {
            var groups = new HashSet<InvGrps>(opt.Groups);
            var processedGroups = new List<InvGrps>(groups.Count);

            if (groups.Contains(InvGrps.all) || groups.Contains(InvGrps.exosuit))
            {
                _gs.InventoryExosuitGeneral.Repair();
                _gs.InventoryExosuitTechOnly.Repair();
                processedGroups.Add(InvGrps.exosuit);
            }

            if (groups.Contains(InvGrps.all) || groups.Contains(InvGrps.freighter))
            {
                _gs.InventoryFreighterTechOnly.Repair();
                processedGroups.Add(InvGrps.freighter);
            }

            if (groups.Contains(InvGrps.all) || groups.Contains(InvGrps.multitool))
            {
                _gs.InventoryMultitool.Repair();
                processedGroups.Add(InvGrps.multitool);
            }

            if (groups.Contains(InvGrps.all) || groups.Contains(InvGrps.ship))
            {
                _gs.InventoryPrimaryShipGeneral.Repair();
                _gs.InventoryPrimaryShipTechOnly.Repair();
                processedGroups.Add(InvGrps.ship);
            }

            if (groups.Contains(InvGrps.all) || groups.Contains(InvGrps.vehicle))
            {
                _gs.InventoryPrimaryVehicle.Repair();
                processedGroups.Add(InvGrps.vehicle);
            }

            Log("Repaired items in the following inventory groups: {0}.", string.Join(", ", processedGroups));
        }

        private void RunRefurbish(RefurbishOptions opt)
        {
            var groups = new HashSet<InvGrps>(opt.Groups);
            var processedGroups = new List<InvGrps>(groups.Count);

            if (groups.Contains(InvGrps.all) || groups.Contains(InvGrps.container))
            {
                foreach (var inventory in _gs.InventoryContainers)
                {
                    inventory.Refill();
                }
                processedGroups.Add(InvGrps.container);
            }

            if (groups.Contains(InvGrps.all) || groups.Contains(InvGrps.exosuit))
            {
                _gs.InventoryExosuitGeneral.Recharge();
                _gs.InventoryExosuitGeneral.Repair();
                _gs.InventoryExosuitGeneral.Refill();

                _gs.InventoryExosuitTechOnly.Recharge();
                _gs.InventoryExosuitTechOnly.Repair();

                processedGroups.Add(InvGrps.exosuit);
            }

            if (groups.Contains(InvGrps.all) || groups.Contains(InvGrps.freighter))
            {
                _gs.InventoryFreighterGeneral.Refill();

                _gs.InventoryFreighterTechOnly.Recharge();
                _gs.InventoryFreighterTechOnly.Repair();

                processedGroups.Add(InvGrps.freighter);
            }

            if (groups.Contains(InvGrps.all) || groups.Contains(InvGrps.multitool))
            {
                _gs.InventoryMultitool.Recharge();
                _gs.InventoryMultitool.Repair();

                processedGroups.Add(InvGrps.multitool);
            }

            if (groups.Contains(InvGrps.all) || groups.Contains(InvGrps.ship))
            {
                _gs.InventoryPrimaryShipGeneral.Recharge();
                _gs.InventoryPrimaryShipGeneral.Refill();
                _gs.InventoryPrimaryShipGeneral.Repair();

                _gs.InventoryPrimaryShipTechOnly.Recharge();
                _gs.InventoryPrimaryShipTechOnly.Repair();

                processedGroups.Add(InvGrps.ship);
            }

            if (groups.Contains(InvGrps.all) || groups.Contains(InvGrps.vehicle))
            {
                _gs.InventoryPrimaryVehicle.Recharge();
                _gs.InventoryPrimaryVehicle.Refill();
                _gs.InventoryPrimaryVehicle.Repair();

                processedGroups.Add(InvGrps.vehicle);
            }

            Log("Refurbished items in the following inventory groups: {0}.", string.Join(", ", processedGroups));
        }

        private void RunRelocate(RelocateOptions opt)
        {
            bool starSystemChanged = false;

            if (opt.GalacticCoordinates != null)
            {
                SetCoordinates(VoxelCoordinates.FromGalacticCoordinateString(opt.GalacticCoordinates), opt.GalacticCoordinates);
                starSystemChanged = true;
            }
            else if (opt.PortalCoordinates != null)
            {
                SetCoordinates(VoxelCoordinates.FromPortalCoordinateString(opt.PortalCoordinates), opt.PortalCoordinates);
                starSystemChanged = true;
            }
            else if (opt.VoxelCoordinates != null)
            {
                SetCoordinates(VoxelCoordinates.FromVoxelCoordinateString(opt.VoxelCoordinates), opt.VoxelCoordinates);
                starSystemChanged = true;
            }

            if (opt.Galaxy.HasValue)
            {
                _gs.PlayerGalaxy = opt.Galaxy.Value;
                starSystemChanged = true;
            }

            if (opt.Planet.HasValue)
            {
                _gs.PlayerPlanet = opt.Planet.Value;
            }
            else if (starSystemChanged  && !opt.SkipPlanetZero)
            {
                _gs.PlayerPlanet = 0;
            }

            if (starSystemChanged && !opt.SkipInShip)
            {
                _gs.PlayerState = GameSave.PlayerStates.InShip;
            }

            Log("Player relocated. Current position: {0}. Planet Index = {1}", _gs.PlayerCoordinates, _gs.PlayerPlanet);
        }

        private void RunAddInventory(AddInventoryOptions opt)
        {
            Inventory inventory = MapAddItemInvGroupToInventory(opt.Group);
            IList<InventoryItemType> matchingItemTypes = inventory.FindMatchingItemTypes(opt.Item, inventory.AllowedCategories);

            if (matchingItemTypes.Count == 0)
            {
                throw new Exception(string.Format("No inventory items matching \"{0}\" were found.", opt.Item));
            }

            if (matchingItemTypes.Count > 1)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var code in matchingItemTypes)
                {
                    sb.Append(string.Format("\n  {0}:{1}", code.Id, code.Name));
                }
                throw new Exception(string.Format("Multiple inventory items matched \"{0}\". Please narrow your search criteria. The following items matched:{1}", opt.Item, sb.ToString()));
            }

            int x, y;
            if (!inventory.AddItemToFreeSlot(matchingItemTypes[0], out x, out y))
            {
                throw new Exception(string.Format("Inventory item not added (no free slots?)"));
            }

            Log("Added to {0} inventory position {1},{2}: {3}", opt.Group, y+1, x+1, matchingItemTypes[0].Name);
        }

        private void RunSetInventory(SetInventoryOptions opt)
        {
            Inventory inventory = MapAddItemInvGroupToInventory(opt.Group);
            IList<InventoryItemType> matchingItemTypes = inventory.FindMatchingItemTypes(opt.Item, inventory.AllowedCategories);

            if (matchingItemTypes.Count == 0)
            {
                throw new Exception(string.Format("No inventory items matching \"{0}\" were found.", opt.Item));
            }

            if (matchingItemTypes.Count > 1)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var code in matchingItemTypes)
                {
                    sb.Append(string.Format("\n  {0}:{1}", code.Id, code.Name));
                }
                throw new Exception(string.Format("Multiple inventory items matched \"{0}\". Please narrow your search criteria. The following items matched:{1}", opt.Item, sb.ToString()));
            }

            inventory.SetSlot(matchingItemTypes[0], opt.Position.X, opt.Position.Y);

            Log("Populated {0} inventory position {1} with {2} '{3}'", opt.Group, opt.Position, matchingItemTypes[0].Name);
        }

        private void RunMoveInventory(MoveInventoryOptions opt)
        {
            Inventory inventory = MapAddItemInvGroupToInventory(opt.Group);
            inventory.MoveSlot(opt.Position.X1, opt.Position.Y1, opt.Position.X2, opt.Position.Y2);

            Log("Moved {0} inventory item from:to => {1}", opt.Group, opt.Position);
        }

        private void RunSwapInventory(SwapInventoryOptions opt)
        {
            Inventory inventory = MapAddItemInvGroupToInventory(opt.Group);
            inventory.SwapSlot(opt.Position.X1, opt.Position.Y1, opt.Position.X2, opt.Position.Y2);

            Log("Swapped {0} inventory items at positions {1}", opt.Group, opt.Position);
        }

        private void RunDelInventory(DelInventoryOptions opt)
        {
            Inventory inventory = MapAddItemInvGroupToInventory(opt.Group);
            inventory.DeleteSlot(opt.Position.X, opt.Position.Y);

            Log("Deleted {0} inventory item at position {1}", opt.Group, opt.Position);
        }

        #endregion

        #region Read-Only Verbs

        private void RunInfo(InfoOptions opt)
        {
            // Dump basic player info
            RunInfoBasic(opt);

            // Dump inventory info
            RunInfoInventory(opt);
        }

        private void RunInfoBasic(InfoOptions opt)
        {
            if (!opt.NoBasic)
            {
                Log("Save file for game slot: {0}", opt.GameSlot);
                Log("  Save file version: {0}", _gs.Version);
                Log("  Platform: {0}", _gs.Platform);
                Log("  Health: {0}", _gs.PlayerHealth);
                Log("  Player Health: {0}", _gs.PlayerHealth);
                Log("  Exosuit Shield: {0}", _gs.ExosuitShield);
                Log("  Ship Health: {0}", _gs.ShipHealth);
                Log("  Ship Shield: {0}", _gs.ShipShield);
                Log("  Units: {0:N0}", _gs.Units);

                var coordinates = _gs.PlayerCoordinates;
                Log("  Coordinates (x,y,z,ssi): {0}", coordinates.ToVoxelCoordinateString());
                Log("  Coordinates (galactic): {0}", coordinates.ToGalacticCoordinateString());
                Log("  Coordinates (portal): {0}", coordinates.ToPortalCoordinateString());
            }
        }

        private void RunInfoInventory(InfoOptions opt)
        {
            if (opt.ShowInventory)
            {
                if (opt.InventoryGroups.Contains(InvSubGrps.all) || opt.InventoryGroups.Contains(InvSubGrps.exosuit) || opt.InventoryGroups.Contains(InvSubGrps.exosuit_general))
                {
                    // Dump exosuit general inventory
                    InfoInventoryGroup(_gs.InventoryExosuitGeneral, opt.InventoryTypes);
                }
                if (opt.InventoryGroups.Contains(InvSubGrps.all) || opt.InventoryGroups.Contains(InvSubGrps.exosuit) || opt.InventoryGroups.Contains(InvSubGrps.exosuit_cargo))
                {
                    // Dump exosuit cargo inventory
                    InfoInventoryGroup(_gs.InventoryExosuitCargo, opt.InventoryTypes);
                }
                if (opt.InventoryGroups.Contains(InvSubGrps.all) || opt.InventoryGroups.Contains(InvSubGrps.exosuit) || opt.InventoryGroups.Contains(InvSubGrps.exosuit_tech))
                {
                    // Dump exosuit tech inventory
                    InfoInventoryGroup(_gs.InventoryExosuitTechOnly, opt.InventoryTypes);
                }
                if (opt.InventoryGroups.Contains(InvSubGrps.all) || opt.InventoryGroups.Contains(InvSubGrps.ship) || opt.InventoryGroups.Contains(InvSubGrps.ship_general))
                {
                    // Dump ship general inventory
                    InfoInventoryGroup(_gs.InventoryPrimaryShipGeneral, opt.InventoryTypes);
                }
                if (opt.InventoryGroups.Contains(InvSubGrps.all) || opt.InventoryGroups.Contains(InvSubGrps.ship) || opt.InventoryGroups.Contains(InvSubGrps.ship_tech))
                {
                    // Dump ship techonly inventory
                    InfoInventoryGroup(_gs.InventoryPrimaryShipTechOnly, opt.InventoryTypes);
                }
                if (opt.InventoryGroups.Contains(InvSubGrps.all) || opt.InventoryGroups.Contains(InvSubGrps.freighter) || opt.InventoryGroups.Contains(InvSubGrps.freighter_general))
                {
                    // Dump freighter general inventory
                    InfoInventoryGroup(_gs.InventoryFreighterGeneral, opt.InventoryTypes);
                }
                if (opt.InventoryGroups.Contains(InvSubGrps.all) || opt.InventoryGroups.Contains(InvSubGrps.freighter) || opt.InventoryGroups.Contains(InvSubGrps.freighter_tech))
                {
                    // Dump freighter tech-only inventory
                    InfoInventoryGroup(_gs.InventoryFreighterTechOnly, opt.InventoryTypes);
                }
                if (opt.InventoryGroups.Contains(InvSubGrps.all) || opt.InventoryGroups.Contains(InvSubGrps.vehicle))
                {
                    // Dump primary vehicle inventory
                    InfoInventoryGroup(_gs.InventoryPrimaryVehicle, opt.InventoryTypes);
                }
                if (opt.InventoryGroups.Contains(InvSubGrps.all) || opt.InventoryGroups.Contains(InvSubGrps.multitool))
                {
                    // Dump multitool inventory
                    InfoInventoryGroup(_gs.InventoryMultitool, opt.InventoryTypes);
                }
            }
        }

        private void RunBackup(BackupOptions opt)
        {
            try
            {
                BackupSave(opt.BackupDir, opt.FullBackup);
            }
            catch (Exception x)
            {
                throw new Exception(string.Format("Error backing up save game: {0}", x.Message));
            }
        }

        private void RunBackupAll(BackupAllOptions opt)
        {
            DoCommon(opt);

            try
            {
                string archivePath = opt.BackupPath;

                if (Directory.Exists(opt.BackupPath))
                {
                    var baseName = string.Format("nmssavetool-backupall-{0}", _gsm.FindMostRecentSaveDateTime().ToString("yyyyMMdd-HHmmss"));
                    var basePath = Path.Combine(opt.BackupPath, baseName);
                    archivePath = basePath + ".zip";
                }

                _gsm.ArchiveSaveDirTo(archivePath);
                Log("Backed up save game files to: {0}", archivePath);
            }
            catch (Exception x)
            {
                throw new Exception(string.Format("Error backing up all save games: {0}", x.Message));
            }
        }

        private void RunDecrypt(DecryptOptions opt)
        {
            LogVerbose("Parsing and formatting save game JSON");
            string formattedJson;
            try
            {
                formattedJson = _gs.ToFormattedJsonString();
            }
            catch (Exception x)
            {
                throw new Exception(string.Format("Error formatting JSON (invalid save?): {0}", x.Message));
            }

            LogVerbose("Writing formatted JSON to:\n   {0}", opt.OutputPath);
            try
            {
                File.WriteAllText(opt.OutputPath, formattedJson);
            }
            catch (Exception x)
            {
                throw new Exception(string.Format("Error writing decrypted JSON: {0}", x.Message));
            }

            Log("Wrote save game to formatted JSON file: {0}", opt.OutputPath);
        }

        #endregion

        #region Update Verbs

        private void RunEncrypt(EncryptOptions opt)
        {
            DoGameSlotCommon(opt);

            LogVerbose("Loading JSON save game data from: {0}", opt.InputPath);

            try
            {
                _gs = _gsm.ReadUnencryptedGameSave(opt.InputPath);
            }
            catch (Exception x)
            {
                throw new Exception(string.Format("Error reading or parsing save game file: {0}", x.Message));
            }

            if (opt.BackupDir != null)
            {
                try
                {
                    BackupSave(opt.BackupDir, opt.FullBackup);
                }
                catch(Exception x)
                {
                    throw new Exception(string.Format("Error backing up save game: {0}", x.Message));
                }
            }

            try
            {
                _gsm.WriteSaveFile(_gs, _gameSlot);
            }
            catch (Exception x)
            {
                throw new Exception(string.Format("Error storing save file: {0}", x.Message));
            }

            Log("Encrypted game save file \"{0}\" and wrote to latest game save for game slot {1}", opt.InputPath, _gameSlot);
        }

        private void RunRestore(RestoreOptions opt)
        {
            DoGameSlotCommon(opt);

            LogVerbose("Loading JSON save game data from: {0}", opt.RestorePath);

            try
            {
                _gs = _gsm.ReadUnencryptedGameSave(opt.RestorePath);
            }
            catch (Exception x)
            {
                throw new Exception(string.Format("Error reading or parsing back-up save-game file: {0}", x.Message));
            }

            try
            {
                _gsm.WriteSaveFile(_gs, _gameSlot);
            }
            catch (Exception x)
            {
                throw new Exception(string.Format("Error restoring save file: {0}", x.Message));
            }

            Log("Restored file \"{0}\" to latest game save for game mode \"{1}\"", opt.RestorePath, _gameSlot);
        }

        #endregion

        #region Private Helper Methods

        private Inventory MapAddItemInvGroupToInventory(ItemInvGroups group)
        {
            Inventory inventory = null;

            switch (group)
            {
                case ItemInvGroups.exosuit_cargo:
                    inventory = _gs.InventoryExosuitCargo;
                    break;
                case ItemInvGroups.exosuit_general:
                    inventory = _gs.InventoryExosuitGeneral;
                    break;
                case ItemInvGroups.exosuit_techonly:
                    inventory = _gs.InventoryExosuitTechOnly;
                    break;
                case ItemInvGroups.ship_general:
                    inventory = _gs.InventoryPrimaryShipGeneral;
                    break;
                case ItemInvGroups.ship_techonly:
                    inventory = _gs.InventoryPrimaryShipTechOnly;
                    break;
                case ItemInvGroups.freighter_general:
                    inventory = _gs.InventoryFreighterGeneral;
                    break;
                case ItemInvGroups.freighter_techonly:
                    inventory = _gs.InventoryFreighterTechOnly;
                    break;
                case ItemInvGroups.multitool:
                    inventory = _gs.InventoryMultitool;
                    break;
                case ItemInvGroups.vehicle:
                    inventory = _gs.InventoryPrimaryVehicle;
                    break;
            }

            return inventory;
        }

        private void SetCoordinates(VoxelCoordinates coordinates, string cordinatesStr)
        {
            LogVerbose("Changing player coordinates from:\n  {0}\nto:\n  {1}\n  {2}", _gs.PlayerCoordinates, cordinatesStr, coordinates);
            _gs.PlayerCoordinates = coordinates;
        }

        private void InfoInventoryGroup(Inventory inventory, IEnumerable<InvType> types)
        {
            int width = inventory.Width;
            int height = inventory.Height;

            bool showAllNonEmpty = types.Contains(InvType.all) || types.Contains(InvType.all_but_empty);
            bool showNonTech = types.Contains(InvType.non_tech);
            bool showEmpty = types.Contains(InvType.empty) || types.Contains(InvType.all);
            bool showTech = types.Contains(InvType.tech) || showAllNonEmpty;
            bool showProduct = types.Contains(InvType.product) || showAllNonEmpty || showNonTech;
            bool showSubstance = types.Contains(InvType.substance) || showAllNonEmpty || showNonTech;

            Log("Inventory group: {0}, [H,W]=[{1},{2}]", inventory.GroupName, height, width);

            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    if ((inventory.IsSlotAtPosEmpty(x, y) && showEmpty) || (inventory.IsSlotAtPosTechnology(x, y) && showTech) || (inventory.IsSlotAtPosProduct(x, y) && showProduct) || (inventory.IsSlotAtPosSubstance(x, y) && showSubstance))
                    {
                        Log("  [{0},{1}] {2}", y + 1, x + 1, inventory.DescribeSlot(x, y));
                    }
                }
            }
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

        private void Log(string format, params object[] arg)
        {
            _log.WriteLine(format, arg);
        }

        private void LogVerbose(string format, params object[] arg)
        {
            _logVerbose.WriteLine(format, arg);
        }

        private void LogError(string format, params object[] arg)
        {
            Console.Error.WriteLine(format, arg);
        }

        private void BackupSave(string backupDir, bool fullBackup)
        {
            try
            {
                var baseName = string.Format("nmssavetool-backup-{0}-{1}", _gameSlot, _gsm.FindMostRecentSaveDateTime().ToString("yyyyMMdd-HHmmss"));
                var basePath = Path.Combine(backupDir, baseName);
                var jsonPath = basePath + ".json";
                _gsm.BackupLatestJsonTo(_gameSlot, jsonPath);
                Log("Backed up decrypted JSON to: {0}", jsonPath);
            }
            catch (Exception x)
            {
                throw new Exception(string.Format("Error backing up save game file: {0}", x.Message), x);
            }
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace nmssavetool
{
    public class GameSave
    {
        public enum PlayerStates
        {
            Unknown,
            InShip,
            OnFoot
                // TODO: What are the other states?
        }

        public enum GameModes
        {
            Unknown,
            Normal,
            Creative,
            Survival,
            Permadeath
        }

        #region Public Constants
        public const int NumContainers = 10;
        public const int MaxHealth = 8;
        public const int MaxLifeSupport = 100;
        public const int MaxExosuitShield = 100;
        public const int MaxShipShield = 200;
        public const int MaxShipHealth = 8;
        public const int MaxUnits = int.MaxValue;
        #endregion

        #region Private Member Variables
        private dynamic _json;
        private Inventory _inventoryExosuitGeneral;
        private Inventory _inventoryExosuitCargo;
        private Inventory _inventoryExosuitTechOnly;
        private Inventory _inventoryMultitool;
        private Inventory _inventoryPrimaryShipGeneral;
        private Inventory _inventoryPrimaryShipTechOnly;
        private Inventory _inventoryFreighterGeneral;
        private Inventory _inventoryFreighterTechOnly;
        private Inventory _inventoryPrimaryVehicle;
        private Inventory[] _inventoryContainer = new Inventory[NumContainers];
        private InventoryItemTypes _invItemTypes;
        private Random _random = new Random();
        #endregion

        #region Public Constructors

        public GameSave(string jsonStr, InventoryItemTypes invItemTypes)
        {
            _json = JObject.Parse(jsonStr);
            _invItemTypes = invItemTypes;
        }

        #endregion

        #region Public Properties

        public int Version
        {
            get
            {
                return (int)_json.Version;
            }
        }

        public GameModes GameMode
        {
            get
            {
                return VersionToGameMode(Version);
            }
        }

        public string Platform
        {
            get
            {
                return _json.Platform;
            }
        }

        public int PlayerHealth
        {
            get
            {
                return _json.PlayerStateData.Health;
            }

            set
            {
                if (value < 1 || value > MaxHealth)
                {
                    throw new ArgumentException(string.Format("Invalid value for player health: {0}. Health must be in the range [{1},{2}]", value, 1, MaxHealth));
                }

                _json.PlayerStateData.Health = value;
            }
        }

        public int LifeSupport
        {
            get
            {
                return _json.PlayerStateData.Energy;
            }

            set
            {
                if (value < 1 || value > MaxLifeSupport)
                {
                    throw new ArgumentException(string.Format("Invalid value for life support: {0}. Life support must be in the range [{1},{2}]", value, 1, MaxLifeSupport));
                }

                _json.PlayerStateData.Energy = value;
            }
        }

        public int ExosuitShield
        {
            get
            {
                return _json.PlayerStateData.Shield;
            }

            set
            {
                if (value < 1 || value > MaxLifeSupport)
                {
                    throw new ArgumentException(string.Format("Invalid value for exosuit shield: {0}. Exosuit shield level must be in the range [{1},{2}]", value, 1, MaxExosuitShield));
                }

                _json.PlayerStateData.Shield = value;
            }
        }

        public int ShipHealth
        {
            get
            {
                return _json.PlayerStateData.ShipHealth;
            }

            set
            {
                if (value < 1 || value > MaxShipHealth)
                {
                    throw new ArgumentException(string.Format("Invalid value for ship health: {0}. Ship health must be in the range [{1},{2}]", value, 1, MaxShipHealth));
                }
            }
        }

        public int ShipShield
        {
            get
            {
                return _json.PlayerStateData.ShipShield;
            }

            set
            {
                if (value < 1 || value > MaxShipShield)
                {
                    throw new ArgumentException(string.Format("Invalid value for ship shield: {0}. Ship shield must be in the range [{1},{2}]", value, 1, MaxShipShield));
                }
            }
        }

        public int Units
        {
            get
            {
                return _json.PlayerStateData.Units;
            }

            set
            {
                if (value < 0)
                {
                    throw new ArgumentException(string.Format("Invalid value for units: {0}. Units must be in the range [{1},{2}]", value, 0, MaxUnits));
                }
                _json.PlayerStateData.Units = value;
            }
        }

        public VoxelCoordinates PlayerCoordinates
        {
            get
            {
                var galacticAddress = _json.PlayerStateData.UniverseAddress.GalacticAddress;
                var coordinates = new VoxelCoordinates((int)galacticAddress.VoxelX, (int)galacticAddress.VoxelY, (int)galacticAddress.VoxelZ, (int)galacticAddress.SolarSystemIndex);
                return coordinates;
            }

            set
            {
                var galacticAddress = _json.PlayerStateData.UniverseAddress.GalacticAddress;
                galacticAddress.VoxelX = value.X;
                galacticAddress.VoxelY = value.Y;
                galacticAddress.VoxelZ = value.Z;
                galacticAddress.SolarSystemIndex = value.SolarSystemIndex;
            }
        }

        public int PlayerGalaxy
        {
            get
            {
                return _json.PlayerStateData.UniverseAddress.RealityIndex;
            }

            set
            {
                if (value != (int)_json.PlayerStateData.UniverseAddress.RealityIndex)
                {
                    _json.PlayerStateData.UniverseAddress.RealityIndex = value;
                }                
            }
        }

        public int PlayerPlanet
        {
            get
            {
                return _json.PlayerStateData.UniverseAddress.GalacticAddress.PlanetIndex;
            }

            set
            {
                if (value < 0)
                {
                    throw new ArgumentException(string.Format("Invalid planet index: {0}", value));
                }

                _json.PlayerStateData.UniverseAddress.GalacticAddress.PlanetIndex = value;
            }
            //_json.SpawnStateData.LastKnownPlayerState = "InShip";
        }

        public PlayerStates PlayerState
        {
            get
            {
                PlayerStates result;
                if (Enum.TryParse<PlayerStates>(_json.SpawnStateData.LastKnownPlayerState, true, out result))
                {
                    return result;
                }

                return PlayerStates.Unknown;
            }

            set
            {
                _json.SpawnStateData.LastKnownPlayerState = value.ToString();
            }            
        }


        public ulong ShipSeed
        {
            get
            {
                return Convert.ToUInt64((string)PrimaryShipNode.Resource.Seed[1], 16);
            }

            set
            {
                PrimaryShipNode.Resource.Seed[1] = string.Format("0x{0:X16}", value);
            }
        }

        public ulong MultitoolSeed
        {
            get
            {
                return Convert.ToUInt64((string)_json.PlayerStateData.CurrentWeapon.GenerationSeed[1], 16);
            }

            set
            {                
                _json.PlayerStateData.CurrentWeapon.GenerationSeed[1] = string.Format("0x{0:X16}", value);
            }
        }

        public ulong FreighterSeed
        {
            get
            {
                return Convert.ToUInt64((string)_json.PlayerStateData.CurrentFreighter.Seed[1], 16);
            }

            set
            {
                _json.PlayerStateData.CurrentFreighter.Seed[1] = string.Format("0x{0:X16}", value);
            }
        }

        public Inventory InventoryExosuitGeneral
        {
            get
            {
                if (_inventoryExosuitGeneral == null)
                {
                    _inventoryExosuitGeneral = 
                        new Inventory(
                            _json.PlayerStateData.Inventory, 
                            "Exosuit General", 
                            _invItemTypes, new InventoryItemCategory[] { InventoryItemCategory.SuitTech, InventoryItemCategory.Product, InventoryItemCategory.Substance },
                            8, 6);
                }
                return _inventoryExosuitGeneral;
            }
        }

        public Inventory InventoryExosuitTechOnly
        {
            get
            {
                if (_inventoryExosuitTechOnly == null)
                {
                    _inventoryExosuitTechOnly = 
                        new Inventory(
                            _json.PlayerStateData.Inventory_TechOnly, 
                            "Exosuit Tech-Only", 
                            _invItemTypes, new InventoryItemCategory[] { InventoryItemCategory.SuitTech },
                            6, 2);
                }
                return _inventoryExosuitTechOnly;
            }
        }

        public Inventory InventoryExosuitCargo
        {
            get
            {
                if (_inventoryExosuitCargo == null)
                {
                    _inventoryExosuitCargo = 
                        new Inventory(
                            _json.PlayerStateData.Inventory_Cargo, 
                            "Exosuit Cargo", 
                            _invItemTypes, new InventoryItemCategory[] { InventoryItemCategory.Product, InventoryItemCategory.Substance },
                            5, 6);
                }
                return _inventoryExosuitCargo;
            }
        }

        public Inventory InventoryMultitool
        {
            get
            {
                if (_inventoryMultitool == null)
                {
                    _inventoryMultitool = 
                        new Inventory(
                            _json.PlayerStateData.WeaponInventory, 
                            "Multitool", 
                            _invItemTypes, new InventoryItemCategory[] { InventoryItemCategory.GunTech },
                            8, 3);
                }
                return _inventoryMultitool;
            }
        }

        public Inventory InventoryPrimaryShipGeneral
        {
            get
            {
                if (_inventoryPrimaryShipGeneral == null)
                {
                    _inventoryPrimaryShipGeneral = 
                        new Inventory(
                            PrimaryShipNode.Inventory, 
                            "Ship General", _invItemTypes, 
                            new InventoryItemCategory[] { InventoryItemCategory.ShipTech, InventoryItemCategory.Product, InventoryItemCategory.Substance },
                            8, 6);
                }
                return _inventoryPrimaryShipGeneral;
            }
        }

        public Inventory InventoryPrimaryShipTechOnly
        {
            get
            {
                if (_inventoryPrimaryShipTechOnly == null)
                {
                    _inventoryPrimaryShipTechOnly = 
                        new Inventory(
                            PrimaryShipNode.Inventory_TechOnly, 
                            "Ship Tech-Only", 
                            _invItemTypes, 
                            new InventoryItemCategory[] { InventoryItemCategory.ShipTech },
                            7, 1);
                }
                return _inventoryPrimaryShipTechOnly;
            }
        }

        public Inventory InventoryFreighterGeneral
        {
            get
            {
                if (_inventoryFreighterGeneral == null)
                {
                    _inventoryFreighterGeneral = 
                        new Inventory(
                            _json.PlayerStateData.FreighterInventory, 
                            "Freighter General", 
                            _invItemTypes, 
                            new InventoryItemCategory[] { InventoryItemCategory.Product, InventoryItemCategory.Substance },
                            8, 6);
                }
                return _inventoryFreighterGeneral;
            }
        }

        public Inventory InventoryFreighterTechOnly
        {
            get
            {
                if (_inventoryFreighterTechOnly == null)
                {
                    _inventoryFreighterTechOnly = 
                        new Inventory(
                            _json.PlayerStateData.FreighterInventory_TechOnly, 
                            "Freighter Tech-Only", 
                            _invItemTypes, 
                            new InventoryItemCategory[] { InventoryItemCategory.FreighterTech },
                            7, 3);
                }
                return _inventoryFreighterTechOnly;
            }
        }

        public Inventory InventoryPrimaryVehicle
        {
            get
            {
                if (_inventoryPrimaryVehicle == null)
                {
                    _inventoryPrimaryVehicle = 
                        new Inventory(
                            PrimaryVehicleNode.Inventory, 
                            "Vehicle", _invItemTypes, 
                            new InventoryItemCategory[] { InventoryItemCategory.VehicleTech, InventoryItemCategory.Product, InventoryItemCategory.Substance },
                            7, 5);
                }
                return _inventoryPrimaryVehicle;
            }
        }

        public Inventory InventoryContainer(int containerNum)
        {
            if (containerNum < 0 || containerNum >= NumContainers)
            {
                throw new ArgumentException(string.Format("Invalid container number: {0}. Valid container numbers must fall in the range [{1},{2}]", containerNum, 0, NumContainers-1), "containerNum");
            }

            if (_inventoryContainer[containerNum] == null)
            {
                string containerName = string.Format("Chest{0}Inventory", containerNum + 1);
                var container = _json.PlayerStateData[containerName];

                _inventoryContainer[containerNum] = 
                    new Inventory(
                        container, 
                        string.Format("Container {0}", containerNum + 1), 
                        _invItemTypes, 
                        new InventoryItemCategory[] { InventoryItemCategory.Product, InventoryItemCategory.Substance },
                        3,3);
            }

            return _inventoryContainer[containerNum];
        }

        public IEnumerable<Inventory> InventoryContainers
        {
            get
            {
                for (int i = 0; i < NumContainers; ++i)
                {
                    var inv = InventoryContainer(i);
                    if (inv != null)
                    {
                        yield return inv;
                    }
                }
            }
        }

        #endregion

        #region Public Methods

        public void SetMaxPlayerHealth()
        {
            PlayerHealth = MaxHealth;
        }

        public void SetMaxLifeSupport()
        {
            LifeSupport = MaxLifeSupport;
        }

        public void SetMaxExosuitShield()
        {
            ExosuitShield = MaxExosuitShield;
        }

        public void SetMaxShipHealth()
        {
            ShipHealth = MaxShipHealth;
        }

        public void SetMaxShipShield()
        {
            ShipShield = MaxShipShield;
        }

        public void SetMaxUnits()
        {
            Units = MaxUnits;
        }

        public void AddUnits(int unitsDelta)
        {
            long newUnits = Units + unitsDelta;
            newUnits = Math.Max(0, Math.Min(int.MaxValue, newUnits));
            Units = (int)newUnits;
        }

        public ulong RandomizeShipSeed()
        {
            byte[] randBytes = new byte[8];
            _random.NextBytes(randBytes);
            ulong seed = BitConverter.ToUInt64(randBytes, 0);
            ShipSeed = seed;
            return seed;
        }

        public ulong RandomizeMultitoolSeed()
        {
            byte[] randBytes = new byte[8];
            _random.NextBytes(randBytes);
            ulong seed = BitConverter.ToUInt64(randBytes, 0);
            MultitoolSeed = seed;
            return seed;
        }

        public ulong RandomizeFreighterSeed()
        {
            byte[] randBytes = new byte[8];
            _random.NextBytes(randBytes);
            ulong seed = BitConverter.ToUInt64(randBytes, 0);
            FreighterSeed = seed;
            return seed;
        }

        public void RepairAll()
        {
            SetMaxPlayerHealth();
            SetMaxExosuitShield();
            SetMaxShipHealth();
            SetMaxShipShield();

            InventoryExosuitGeneral.Repair();
            InventoryExosuitTechOnly.Repair();
            InventoryMultitool.Repair();
            InventoryPrimaryShipGeneral.Repair();
            InventoryPrimaryShipTechOnly.Repair();
            InventoryFreighterTechOnly.Repair();
            InventoryPrimaryVehicle.Repair();
        }

        public void RechargeAll()
        {
            SetMaxLifeSupport();

            InventoryExosuitGeneral.Recharge();
            InventoryExosuitTechOnly.Recharge();
            InventoryMultitool.Recharge();
            InventoryPrimaryShipGeneral.Recharge();
            InventoryPrimaryShipTechOnly.Recharge();
            InventoryFreighterTechOnly.Recharge();
            InventoryPrimaryVehicle.Recharge();
        }

        public void RefillAll()
        {
            InventoryExosuitGeneral.Refill();
            InventoryExosuitCargo.Refill();
            InventoryPrimaryShipGeneral.Refill();
            InventoryFreighterGeneral.Refill();
            InventoryPrimaryVehicle.Refill();
            for (int i = 0; i < NumContainers; ++i)
            {
                InventoryContainer(i).Refill();
            }
        }

        public string ToFormattedJsonString()
        {
            string json = JsonConvert.SerializeObject(_json, Formatting.Indented);
            return json;
        }

        public string ToUnformattedJsonString()
        {
            string json = JsonConvert.SerializeObject(_json, Formatting.None);
            return json;
        }

        #endregion

        #region Private Methods and Properties

        private dynamic PrimaryShipNode
        {
            get
            {
                int primaryShipIndex = _json.PlayerStateData.PrimaryShip;
                return _json.PlayerStateData.ShipOwnership[primaryShipIndex];
            }
        }

        private dynamic PrimaryVehicleNode
        {
            get
            {
                int primaryVehicleIndex = _json.PlayerStateData.PrimaryVehicle;
                return _json.PlayerStateData.VehicleOwnership[primaryVehicleIndex];
            }
        }

        private static GameModes VersionToGameMode(int version)
        {
            switch (version)
            {
                case 4616: return GameModes.Normal;
                case 5128: return GameModes.Creative;
                case 5640: return GameModes.Survival;
                case 6664: return GameModes.Permadeath;
                default: return GameModes.Unknown;
            }
        }

        private static int GameModeToVersion(GameModes gameMode)
        {
            switch(gameMode)
            {
                case GameModes.Normal: return 4616;
                case GameModes.Creative: return 5128;
                case GameModes.Survival: return 5640;
                case GameModes.Permadeath: return 6664;
                default: throw new ArgumentException("Unknown game mode");
            }
        }

        #endregion

    }
}

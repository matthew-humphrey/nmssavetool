using System;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using System.IO.Compression;
using System.Text;
using System.Collections.Generic;
using nomanssave;
using CsvHelper;

namespace nmssavetool
{
    /// <summary>
    /// Represents the available NMS game modes.
    /// </summary>
    public enum GameModes
    {
        normal,
        survival,
        creative,
        permadeath
    }

    /// <summary>
    /// Provides an abstraction over the default NMS game save directory and the naming
    /// of files within this directory.
    /// </summary>
    public class GameSaveManager
    {
        #region Public Constants
        const uint MaxGameSlots = 5;
        #endregion

        #region Private Member Variables
        private string _savePath;
        private ulong? _profileKey;
        private InventoryItemTypes _inventoryItemTypes;
        private TextWriter _log;
        private TextWriter _logVerbose;
        #endregion

        #region Public Constructors

        /// <summary>
        /// Creates a GameSaveManager attached to the specified NMS game save directory.
        /// </summary>
        public GameSaveManager(string saveDir, TextWriter log, TextWriter logVerbose)
        {
            _log = log;
            _logVerbose = logVerbose;

            if (saveDir != null)
            {
                if (Directory.EnumerateFiles(saveDir, "storage*.hg").Count() > 0)
                {
                    _savePath = saveDir;
                }
                else
                {
                    throw new FileNotFoundException(string.Format("Specified save game directory does not contain any save game files: {0}", saveDir));
                }
            }
            else
            {
                var nmsPath = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HelloGames"), "NMS");
                if (!Directory.Exists(nmsPath))
                {
                    throw new FileNotFoundException(string.Format("No Man's Sky save game folder not found at expected location: {0}", nmsPath));
                }

                LogVerbose("Using NMS AppData folder: {0}", nmsPath);

                // Check for GoG version of the game (hat tip to Reddit user, Yarmoshuk)
                var gogDir = Path.Combine(nmsPath, "DefaultUser");
                if (Directory.Exists(gogDir) && Directory.EnumerateFiles(gogDir, "storage*.hg").Count() > 0)
                {
                    _savePath = gogDir;
                }

                if (null == _savePath)
                {
                    foreach (var dir in Directory.EnumerateDirectories(nmsPath))
                    {
                        _profileKey = GetProfileKeyFromPath(dir);
                        if (null != _profileKey)
                        {
                            _savePath = dir;
                            break;
                        }
                    }
                }

                if (null == _savePath)
                {
                    foreach (var dir in Directory.EnumerateDirectories(nmsPath))
                    {
                        if (Directory.EnumerateFiles(dir, "storage*.hg").Count() > 0)
                        {
                            _savePath = dir;
                        }
                    }
                }

                if (null == _savePath)
                {
                    throw new FileNotFoundException(string.Format("No save game profile folder found in NMS save game folder: {0}", nmsPath));
                }
            }

            LogVerbose("Using save path: {0}", _savePath);

            // Attempt to load list of valid item types
            _inventoryItemTypes = new InventoryItemTypes(LoadInventoryItemTypesFromDefaultCsvFile());
        }

        #endregion

        #region Public Properties
        public InventoryItemTypes InventoryItemTypes
        {
            get { return _inventoryItemTypes; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Read the latest game save for the specified game mode, and return a 
        /// GameSave object that can be used to manipulate various attributes of that save.
        /// </summary>
        /// <param name="gameSlot">The NMS game slot (1 - 5) whose latest save will be read.</param>
        /// <returns></returns>
        public GameSave ReadSaveFile(uint gameSlot)
        {
            string metadataPath;
            string storagePath;
            uint archiveNumber;

            LogVerbose("Checking for save games for slot {0}", gameSlot);

            GameSavePathsForRead(gameSlot, out metadataPath, out storagePath, out archiveNumber);

            LogVerbose("Reading game save for slot {0} at metadata path = '{1}', storage path = {2}, and archive number = {3}", gameSlot, metadataPath, storagePath, archiveNumber);

            string jsonStr = Storage.Read(metadataPath, storagePath, archiveNumber, _profileKey);

            return new GameSave(jsonStr, _inventoryItemTypes);
        }

        /// <summary>
        /// Read a game save from an unencrypted JSON game save file, and return a GameSave object.
        /// </summary>
        /// <param name="path">Path to the file.</param>
        /// <returns>A GameSave object that abstracts all access to the game save information.</returns>
        public GameSave ReadUnencryptedGameSave(string path)
        {
            string json = File.ReadAllText(path);
            GameSave gs = new GameSave(json, _inventoryItemTypes);
            return gs;
        }

        /// <summary>
        /// Serialize the provided GameSave object and encrypt and write it to the latest game save slot for the specified game mode.
        /// </summary>
        /// <param name="gameSave">The Game Save wrapper object, as returned by ReadLatestSaveFile.</param>
        /// <param name="gameSlot">The game slot (1-5) for which the save will be written.</param>
        /// <param name="useOldFormat">true to use the NMS 1.0 game save format, false otherwise.</param>
        public void WriteSaveFile(GameSave gameSave, uint gameSlot)
        {
            string metadataPath;
            string storagePath;
            uint archiveNumber;

            ValidateGameSlot(gameSlot);

            LogVerbose("Determining save games file locations for slot {0}", gameSlot);

            GameSavePathsForWrite(gameSlot, out metadataPath, out storagePath, out archiveNumber);

            LogVerbose("Writing game save for slot {0} to metadata path = '{1}', storage path = {2}, and archive number = {3}", gameSlot, metadataPath, storagePath, archiveNumber);

            string json = gameSave.ToUnformattedJsonString();

            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                Storage.Write(metadataPath, storagePath, ms, archiveNumber, _profileKey, false);
                var now = DateTime.Now;
                File.SetLastWriteTime(metadataPath, now);
                File.SetLastWriteTime(storagePath, now);
            }
        }

        /// <summary>
        /// Zip and copy the entire NMS game save directory, including all game modes and the game cache.
        /// </summary>
        /// <param name="archivePath">The path to which the archive will be written.</param>
        public void ArchiveSaveDirTo(string archivePath)
        {
            LogVerbose("Attempting to create a save game archive at path: ", archivePath);

            using (var zipArchive = ZipFile.Open(archivePath, ZipArchiveMode.Update))
            {
                var di = new DirectoryInfo(_savePath);
                LogVerbose("Archive save game files from: {0}", _savePath);
                var filesToArchive = di.GetFiles("*.hg");
                foreach(var fileToArchive in filesToArchive)
                {
                    LogVerbose("Archiving: {0}", fileToArchive.Name);
                    zipArchive.CreateEntryFromFile(fileToArchive.FullName, fileToArchive.Name);
                }
            }
        }

        /// <summary>
        /// Decrypt and save a copy of the latest NMS game save for the specified game mode. 
        /// The file is written as unencrypted, formatted JSON.
        /// </summary>
        /// <param name="gameSlot">The game slot (1-5) for which the latest save will be backed up.</param>
        /// <param name="jsonBackupPath">The path to which the JSON file will be written.</param>
        public void BackupLatestJsonTo(uint gameSlot, string jsonBackupPath)
        {
            var gameSave = ReadSaveFile(gameSlot);
            LogVerbose("Backing up decrypted save game file to: {0}", jsonBackupPath);
            File.WriteAllText(jsonBackupPath, gameSave.ToFormattedJsonString());
        }

        public DateTime FindMostRecentSaveDateTime()
        {
            var saveFiles = Directory.EnumerateFiles(_savePath, "*.hg");
            return (from saveFile in saveFiles select File.GetLastWriteTime(saveFile)).Max();
        }
        #endregion

        #region Private Methods

        public sealed class InventoryItemTypeCsvMap : CsvHelper.Configuration.CsvClassMap<InventoryItemType>
        {
            public InventoryItemTypeCsvMap()
            {
                Map(m => m.Id).Name("Id");
                Map(m => m.Name).Name("Name").Default("?");
                Map(m => m.Category).Name("Category").Default(InventoryItemCategory.Unknown);
                Map(m => m.IsRechargeable).Name("IsRechargeable").Default(false);
                Map(m => m.DefaultAmount).Name("DefaultAmount").Default(0);
                Map(m => m.MaxAmount).Name("MaxAmount").Default(0);
            }
        }

        private IList<InventoryItemType> LoadInventoryItemTypesFromCsvFile(string filePath)
        {
            LogVerbose("Reading inventory item list from: {0}", filePath);

            var sr = new StreamReader(filePath);
            var csv = new CsvReader(sr);
            csv.Configuration.AllowComments = true;
            csv.Configuration.HasHeaderRecord = true;
            csv.Configuration.IgnoreHeaderWhiteSpace = true;
            csv.Configuration.IsHeaderCaseSensitive = false;
            csv.Configuration.TrimHeaders = true;
            csv.Configuration.SkipEmptyRecords = true;
            csv.Configuration.TrimFields = false;
            csv.Configuration.IgnorePrivateAccessor = true;
            csv.Configuration.RegisterClassMap<InventoryItemTypeCsvMap>();
            csv.Configuration.IgnoreReadingExceptions = true;

            csv.Configuration.ReadingExceptionCallback = (ex, row) =>
            {
                Console.WriteLine("Inventory Item Types CSV file error at line {0}. Skipping.", row.Row);
            };

            var invItemTypes = csv.GetRecords<InventoryItemType>().ToList();
            return invItemTypes;
        }

        public IList<InventoryItemType> LoadInventoryItemTypesFromDefaultCsvFile()
        {
            string dir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string itemTypesFile = Path.Combine(dir, "InventoryItemTypes.csv");
            return LoadInventoryItemTypesFromCsvFile(itemTypesFile);
        }


        static private ulong? GetProfileKeyFromPath(string path)
        {
            var parts = path.Split(Path.DirectorySeparatorChar);
            if (parts.Length > 0)
            {
                var folderName = parts[parts.Length - 1];
                var match = Regex.Match(folderName, @"st_(\d+)");

                if (match.Success)
                {
                    ulong pk;                    
                    if (ulong.TryParse(match.Groups[1].Value, out pk))
                    {
                        return pk;
                    }
                }
            }

            return null;
        }

        private void Log(string format, params object[] arg)
        {
            _log.WriteLine(format, arg);
        }

        private void LogVerbose(string format, params object[] arg)
        {
            _logVerbose.WriteLine(format, arg);
        }


        private string ArchiveNumberToMetadataFileName(uint archiveNumber)
        {
            if (archiveNumber == 0)
            {
                return "mf_storage.hg";
            }
            else
            {
                return string.Format("mf_storage{0}.hg", archiveNumber + 1);
            }
        }

        private string ArchiveNumberToStorageFileName(uint archiveNumber)
        {
            if (archiveNumber == 0)
            {
                return "storage.hg";
            }
            else
            {
                return string.Format("storage{0}.hg", archiveNumber + 1);
            }
        }

        private void GameSavePathsForWrite(uint gameSlot, out string metadataPath, out string storagePath, out uint archiveNumber)
        {
            ValidateGameSlot(gameSlot);

            archiveNumber = 2 * (gameSlot - 1);
            metadataPath = Path.Combine(_savePath, ArchiveNumberToMetadataFileName(archiveNumber));
            storagePath = Path.Combine(_savePath, ArchiveNumberToStorageFileName(archiveNumber));
        }

        private void GameSavePathsForRead(uint gameSlot, out string metadataPath, out string storagePath, out uint archiveNumber)
        {
            metadataPath = null;
            storagePath = null;
            archiveNumber = 0;

            uint[] archiveNumbers;

            ValidateGameSlot(gameSlot);

            archiveNumbers = new uint[] { 2 * (gameSlot - 1), 2 * (gameSlot - 1) + 1 };

            // Find the newest metadata file.
            DateTime newestMdWriteTime = DateTime.MinValue;
            foreach (var i in archiveNumbers)
            {
                var mdp = Path.Combine(_savePath, ArchiveNumberToMetadataFileName(i));
                var stp = Path.Combine(_savePath, ArchiveNumberToStorageFileName(i));

                if (File.Exists(mdp) && File.Exists(stp) && File.GetLastWriteTime(mdp) > newestMdWriteTime)
                {
                    metadataPath = mdp;
                    storagePath = stp;
                    archiveNumber = i;
                    newestMdWriteTime = File.GetLastWriteTime(mdp);
                }
            }

            if (null == metadataPath)
            {
                throw new FileNotFoundException(string.Format("No save games found for game slot {0}", gameSlot));
            }
        }

        private static void ValidateGameSlot(uint gameSlot)
        {
            if (gameSlot < 1 || gameSlot > MaxGameSlots)
            {
                throw new ArgumentException(string.Format("Invalid game slot: {0}", gameSlot));
            }
        }
        #endregion
    }
}

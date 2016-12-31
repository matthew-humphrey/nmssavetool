using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace nmssavetool
{
    public enum GameModes
    {
        Normal,
        Survival,
        Creative
    }


    /// <summary>
    /// Provides an abstraction over the default NMS game save directory and the naming
    /// of files within this directory.
    /// </summary>
    public class GameSaveDir
    {
        private string _savePath;
        public string SavePath
        {
            get { return _savePath; }
        }

        private ulong _profileKey;
        public ulong ProfileKey
        {
            get { return _profileKey; }
        }

        public GameSaveDir()
        {
            var nmsPath = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HelloGames"), "NMS");
            if (!Directory.Exists(nmsPath))
            {
                throw new FileNotFoundException("No Man's Sky save game folder not found at expected location: {0}", nmsPath);
            }

            ulong? pk = null;

            foreach (var dir in Directory.EnumerateDirectories(nmsPath))
            {
                pk = GetProfileKeyFromPath(dir);
                if (null != pk)
                {
                    _savePath = dir;
                    _profileKey = pk.Value;
                    break;
                }
            }   

            if (null == pk)
            {
                throw new FileNotFoundException("No save game profile folder found in NMS save game folder: {0}", nmsPath);
            }
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

        public void FindLatestGameSaveFiles(GameModes gameMode, out string metadataPath, out string storagePath, out uint archiveNumber, out ulong profileKey)
        {
            metadataPath = null;
            storagePath = null;
            archiveNumber = 0;
            profileKey = this._profileKey;

            uint[] archiveNumbers;

            switch (gameMode)
            {
                case GameModes.Normal:
                    archiveNumbers = new uint[] { 0, 1, 2 };
                    break;
                case GameModes.Survival:
                    archiveNumbers = new uint[] { 3, 4, 5 };
                    break;
                case GameModes.Creative:
                    archiveNumbers = new uint[] { 6, 7, 8 };
                    break;
                default:
                    throw new InvalidOperationException();
            }

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
                throw new FileNotFoundException(string.Format("No save games found for game mode {0}", gameMode)); 
            }
        }

    }
}

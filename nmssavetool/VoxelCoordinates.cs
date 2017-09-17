using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace nmssavetool
{
    public class VoxelCoordinates
    {
        private int x;

        public int X
        {
            get
            {
                return x;
            }

            set
            {
                if (x < -2048 || x > 2047)
                {
                    throw new ArgumentException(string.Format("Invalid X value, {0}. Valid NMS X voxels are in the range [-2048, 2047]", x));
                }
                x = value;
            }
        }

        private int y;
        public int Y
        {
            get
            {
                return y;
            }

            set
            {
                if (value < -128 || value > 127)
                {
                    throw new ArgumentException(string.Format("Invalid Y value, {0}. Valid NMS Y voxels are in the range [-128, 127]", value));
                }
                y = value;
            }
        }

        private int z;
        public int Z
        {
            get
            {
                return z;
            }

            set
            {
                if (value < -2048 || value > 2047)
                {
                    throw new ArgumentException(string.Format("Invalid Z value, {0}. Valid NMS Z voxels are in the range [-2048, 2047]", value));
                }
                z = value;
            }
        }

        private int solarSystemIndex;
        public int SolarSystemIndex
        {
            get
            {
                return solarSystemIndex;
            }

            set
            {
                if (value < 0 || value > 600)
                {
                    throw new ArgumentException(string.Format("Invalid Solar System Index value, {0}. Valid values are in the range [0, 600]", value));
                }
                solarSystemIndex = value;
            }
        }

        public VoxelCoordinates(int x, int y, int z, int solarSystemIndex)
        {
            X = x;
            Y = y;
            Z = z;
            SolarSystemIndex = solarSystemIndex;
        }

        public VoxelCoordinates()
        {
            X = 0;
            Y = 0;
            Z = 0;
            SolarSystemIndex = 0;
        }

        private static void InvalidGalacticCoordinateString(string galacticCoordinateStr)
        {
            throw new ArgumentException(string.Format("Invalid galactic coordinate value: {0}. Galactic coordinates should be of the form XXXX:XXXX:XXXX:XXXX, where each 'X' represents a hexidecimal digit.", galacticCoordinateStr));
        }

        public string ToGalacticCoordinateString()
        {
            return string.Format("{0:X4}:{1:X4}:{2:X4}:{3:X4}", (x + 2047), (y + 127), (z + 2047), solarSystemIndex);
        }

        public static VoxelCoordinates FromGalacticCoordinateString(string nmsHexCoordinates)
        {
            VoxelCoordinates coordinates = null;

            string[] parts = nmsHexCoordinates.Split(':');
            if (parts.Length == 5)
            {
                parts = parts.Skip(1).Take(4).ToArray();
            }
            else if (parts.Length != 4)
            {
                InvalidGalacticCoordinateString(nmsHexCoordinates);
            }

            try
            {
                coordinates = new VoxelCoordinates(
                    Convert.ToInt32(parts[0], 16) - 2047,
                    Convert.ToInt32(parts[1], 16) - 127,
                    Convert.ToInt32(parts[2], 16) - 2047,
                    Convert.ToInt32(parts[3], 16));
            }
            catch (ArgumentException)
            {
                InvalidGalacticCoordinateString(nmsHexCoordinates);
            }

            return coordinates;
        }

        private static void InvalidVoxelCoordinateString(string voxelCoordinateStr)
        {
            throw new ArgumentException(string.Format("Invalid voxel coordinate value: {0}. Valid voxel coordinates are specified as x,y,z,ssi or (x,y,z,ssi), and must obey the range rules for NMS voxels.", voxelCoordinateStr));
        }

        public string ToVoxelCoordinateString()
        {
            return string.Format("({0},{1},{2},{3})", X, Y, Z, SolarSystemIndex);
        }

        public static VoxelCoordinates FromVoxelCoordinateString(string voxelCoordinateStr)
        {
            VoxelCoordinates coordinates = null;

            var match = Regex.Match(voxelCoordinateStr, @"\(?(?<X>[+-]?\d+),(?<Y>[+-]?\d+),(?<Z>[+-]?\d+),(?<SSI>[+-]?\d+)\)?");

            if (!match.Success)
            {
                InvalidVoxelCoordinateString(voxelCoordinateStr);
            }

            try
            {
                coordinates = new VoxelCoordinates(
                    Convert.ToInt32(match.Groups["X"].Value),
                    Convert.ToInt32(match.Groups["Y"].Value),
                    Convert.ToInt32(match.Groups["Z"].Value),
                    Convert.ToInt32(match.Groups["SSI"].Value));
            }
            catch (ArgumentException)
            {
                InvalidVoxelCoordinateString(voxelCoordinateStr);
            }

            return coordinates;
        }

        private static void InvalidPortalCoordinateString(string portalCoordinateStr)
        {
            throw new ArgumentException(string.Format("Invalid portal coordinate value: {0}. Valid portal coordinates are specified as 12 hexidecimal digits.", portalCoordinateStr));
        }

        public string ToPortalCoordinateString()
        {
            return string.Format("0{0:X3}{1:X2}{2:X3}{3:X3}", SolarSystemIndex, (Y & 0xFF), (Z & 0xFFF), (X & 0xFFF));
        }

        public static VoxelCoordinates FromPortalCoordinateString(string portalCoordinateStr)
        {
            VoxelCoordinates coordinates = new VoxelCoordinates();

            var match = Regex.Match(portalCoordinateStr, @"[A-Za-z0-9]{12}");
            if (!match.Success)
            {
                InvalidPortalCoordinateString(portalCoordinateStr);
            }

            try
            {
                coordinates.X = ((Convert.ToInt32(portalCoordinateStr.Substring(9, 3), 16) + 2047) & 0xFFF) - 2047;
                coordinates.Y = ((Convert.ToInt32(portalCoordinateStr.Substring(4, 2), 16) + 127) & 0xFF) - 127;
                coordinates.Z = ((Convert.ToInt32(portalCoordinateStr.Substring(6, 3), 16) + 2047) & 0xFFF) - 2047;
                coordinates.SolarSystemIndex = Convert.ToInt32(portalCoordinateStr.Substring(1, 3), 16);
            }
            catch (ArgumentException)
            {
                InvalidPortalCoordinateString(portalCoordinateStr);
            }

            return coordinates;
        }

        public override string ToString()
        {
            return string.Format("Galactic coordinates: {0} ; Portal coordinates: {1} ; Voxel coordinates: {2}",
                ToGalacticCoordinateString(), ToPortalCoordinateString(), ToVoxelCoordinateString());
        }


    }
}

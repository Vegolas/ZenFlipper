using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Collections.Generic;

namespace Gothic2ZenFlipper
{
    class ZenFlipper
    {
        // Windows-1250 encoding for Central European characters (Gothic 2)
        private static readonly Encoding Windows1250 = Encoding.GetEncoding(1250);

        public static void Run(string[] args)
        {
            Console.WriteLine("=== Gothic 2 .ZEN File Coordinate Flipper ===\n");

            if (args.Length < 1)
            {
                ShowUsage();
                return;
            }

            string inputFile = args[0];
            string outputFile = args.Length > 1 ? args[1] : null;

            // Default: flip X axis
            bool flipX = true;
            bool flipY = false;
            bool flipZ = false;
            bool previewMode = false;
            bool flipRotation = false;
            bool verbose = false;

            // Parse additional arguments for axis selection
            for (int i = (outputFile != null ? 2 : 1); i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "-x":
                        flipX = true;
                        flipY = false;
                        flipZ = false;
                        break;
                    case "-y":
                        flipX = false;
                        flipY = true;
                        flipZ = false;
                        break;
                    case "-z":
                        flipX = false;
                        flipY = false;
                        flipZ = true;
                        break;
                    case "-xy":
                        flipX = true;
                        flipY = true;
                        flipZ = false;
                        break;
                    case "-xz":
                        flipX = true;
                        flipY = false;
                        flipZ = true;
                        break;
                    case "-yz":
                        flipX = false;
                        flipY = true;
                        flipZ = true;
                        break;
                    case "-xyz":
                        flipX = true;
                        flipY = true;
                        flipZ = true;
                        break;
                    case "-preview":
                    case "-p":
                        previewMode = true;
                        break;
                    case "-rotation":
                    case "-r":
                        flipRotation = true;
                        break;
                    case "-verbose":
                    case "-v":
                        verbose = true;
                        break;
                }
            }

            if (!File.Exists(inputFile))
            {
                Console.WriteLine($"Error: Input file '{inputFile}' not found!");
                return;
            }

            Console.WriteLine($"Input file: {inputFile}");
            if (!previewMode && outputFile != null)
                Console.WriteLine($"Output file: {outputFile}");
            Console.WriteLine($"Flipping axes: X={flipX}, Y={flipY}, Z={flipZ}");
            Console.WriteLine($"Flip rotation matrices: {flipRotation}");
            Console.WriteLine($"Mode: {(previewMode ? "PREVIEW ONLY" : "APPLY CHANGES")}\n");

            try
            {
                if (previewMode)
                {
                    PreviewChanges(inputFile, flipX, flipY, flipZ, flipRotation);
                }
                else
                {
                    if (outputFile == null)
                    {
                        Console.WriteLine("Error: Output file required when not in preview mode!");
                        ShowUsage();
                        return;
                    }
                    ProcessZenFile(inputFile, outputFile, flipX, flipY, flipZ, flipRotation, verbose);
                    Console.WriteLine("\n✓ Conversion completed successfully!");
                    Console.WriteLine($"Modified file saved to: {outputFile}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n✗ Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        static void ShowUsage()
        {
            Console.WriteLine("Usage: ZenFileCoordinateFlipper <input.zen> [output.zen] [options]\n");
            Console.WriteLine("Options:");
            Console.WriteLine("  -x         Flip X axis only (default)");
            Console.WriteLine("  -y         Flip Y axis only");
            Console.WriteLine("  -z         Flip Z axis only");
            Console.WriteLine("  -xy        Flip X and Y axes");
            Console.WriteLine("  -xz        Flip X and Z axes");
            Console.WriteLine("  -yz        Flip Y and Z axes");
            Console.WriteLine("  -xyz       Flip all three axes");
            Console.WriteLine("  -rotation  Also flip rotation matrices (experimental)");
            Console.WriteLine("  -r         Short for -rotation");
            Console.WriteLine("  -preview   Show what would change without modifying file");
            Console.WriteLine("  -p         Short for -preview");
            Console.WriteLine("  -verbose   Show detailed changes");
            Console.WriteLine("  -v         Short for -verbose");
            Console.WriteLine("\nExamples:");
            Console.WriteLine("  Preview changes:");
            Console.WriteLine("    ZenFileCoordinateFlipper world.zen -preview -x");
            Console.WriteLine("  Apply X-axis flip:");
            Console.WriteLine("    ZenFileCoordinateFlipper world.zen world_flipped.zen -x");
            Console.WriteLine("  Apply with rotation flip:");
            Console.WriteLine("    ZenFileCoordinateFlipper world.zen world_flipped.zen -x -rotation");
        }

        static void PreviewChanges(string inputFile, bool flipX, bool flipY, bool flipZ, bool flipRotation)
        {
            string content = File.ReadAllText(inputFile, Windows1250);
            int sampleCount = 0;
            int maxSamples = 3;

            Console.WriteLine("=== PREVIEW MODE - Showing first few transformations ===\n");

            // Preview object position changes
            string posPattern = @"trafoOSToWSPos=vec3:([-+]?[0-9]*\.?[0-9]+(?:[eE][-+]?[0-9]+)?)\s+([-+]?[0-9]*\.?[0-9]+(?:[eE][-+]?[0-9]+)?)\s+([-+]?[0-9]*\.?[0-9]+(?:[eE][-+]?[0-9]+)?)";

            Console.WriteLine("Object Positions (trafoOSToWSPos):");
            foreach (Match match in Regex.Matches(content, posPattern))
            {
                if (sampleCount >= maxSamples) break;

                float x = float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                float y = float.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                float z = float.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);

                float newX = flipX ? -x : x;
                float newY = flipY ? -y : y;
                float newZ = flipZ ? -z : z;

                Console.WriteLine($"  #{sampleCount + 1}: ({x:F2}, {y:F2}, {z:F2}) → ({newX:F2}, {newY:F2}, {newZ:F2})");
                sampleCount++;
            }

            int totalPositions = Regex.Matches(content, posPattern).Count;
            Console.WriteLine($"  Total: {totalPositions} object positions\n");

            // Preview waypoint position changes
            sampleCount = 0;
            string waypointPattern = @"position=vec3:([-+]?[0-9]*\.?[0-9]+(?:[eE][-+]?[0-9]+)?)\s+([-+]?[0-9]*\.?[0-9]+(?:[eE][-+]?[0-9]+)?)\s+([-+]?[0-9]*\.?[0-9]+(?:[eE][-+]?[0-9]+)?)";

            Console.WriteLine("Waypoint Positions:");
            foreach (Match match in Regex.Matches(content, waypointPattern))
            {
                if (sampleCount >= maxSamples) break;

                float x = float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                float y = float.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                float z = float.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);

                float newX = flipX ? -x : x;
                float newY = flipY ? -y : y;
                float newZ = flipZ ? -z : z;

                Console.WriteLine($"  #{sampleCount + 1}: ({x:F2}, {y:F2}, {z:F2}) → ({newX:F2}, {newY:F2}, {newZ:F2})");
                sampleCount++;
            }

            int totalWaypoints = Regex.Matches(content, waypointPattern).Count;
            Console.WriteLine($"  Total: {totalWaypoints} waypoint positions\n");

            // Preview waypoint direction changes
            sampleCount = 0;
            string directionPattern = @"direction=vec3:([-+]?[0-9]*\.?[0-9]+(?:[eE][-+]?[0-9]+)?)\s+([-+]?[0-9]*\.?[0-9]+(?:[eE][-+]?[0-9]+)?)\s+([-+]?[0-9]*\.?[0-9]+(?:[eE][-+]?[0-9]+)?)";

            Console.WriteLine("Waypoint Directions:");
            foreach (Match match in Regex.Matches(content, directionPattern))
            {
                if (sampleCount >= maxSamples) break;

                float x = float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                float y = float.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                float z = float.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);

                float newX = flipX ? -x : x;
                float newY = flipY ? -y : y;
                float newZ = flipZ ? -z : z;

                Console.WriteLine($"  #{sampleCount + 1}: ({x:F3}, {y:F3}, {z:F3}) → ({newX:F3}, {newY:F3}, {newZ:F3})");
                sampleCount++;
            }

            int totalDirections = Regex.Matches(content, directionPattern).Count;
            Console.WriteLine($"  Total: {totalDirections} waypoint directions\n");

            // Count other elements
            int bboxCount = Regex.Matches(content, @"bbox3DWS=rawFloat:").Count;
            Console.WriteLine($"Bounding boxes: {bboxCount}");

            if (flipRotation)
            {
                int rotCount = Regex.Matches(content, @"trafoOSToWSRot=raw:").Count;
                Console.WriteLine($"Rotation matrices: {rotCount}");
            }

            Console.WriteLine("\n=== Preview complete. Use without -preview to apply changes ===");
        }

        static void ProcessZenFile(string inputFile, string outputFile, bool flipX, bool flipY, bool flipZ, bool flipRotation, bool verbose)
        {
            string content = File.ReadAllText(inputFile, Windows1250);
            int modifiedCount = 0;

            // Pattern 1: trafoOSToWSPos (regular objects)
            string trafoPattern = @"trafoOSToWSPos=vec3:([-+]?[0-9]*\.?[0-9]+(?:[eE][-+]?[0-9]+)?)\s+([-+]?[0-9]*\.?[0-9]+(?:[eE][-+]?[0-9]+)?)\s+([-+]?[0-9]*\.?[0-9]+(?:[eE][-+]?[0-9]+)?)";

            content = Regex.Replace(content, trafoPattern, match =>
            {
                float x = float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                float y = float.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                float z = float.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);

                float origX = x, origY = y, origZ = z;

                if (flipX) x = -x;
                if (flipY) y = -y;
                if (flipZ) z = -z;

                modifiedCount++;

                if (verbose && modifiedCount <= 10)
                {
                    Console.WriteLine($"trafoOSToWSPos #{modifiedCount}: ({origX:F2},{origY:F2},{origZ:F2}) -> ({x:F2},{y:F2},{z:F2})");
                }
                else if (modifiedCount % 100 == 0)
                {
                    Console.WriteLine($"Processing... {modifiedCount} positions modified");
                }

                return $"trafoOSToWSPos=vec3:{x.ToString("G", CultureInfo.InvariantCulture)} {y.ToString("G", CultureInfo.InvariantCulture)} {z.ToString("G", CultureInfo.InvariantCulture)}";
            });

            // Pattern 2: position (waypoints)
            int waypointCount = 0;
            string positionPattern = @"position=vec3:([-+]?[0-9]*\.?[0-9]+(?:[eE][-+]?[0-9]+)?)\s+([-+]?[0-9]*\.?[0-9]+(?:[eE][-+]?[0-9]+)?)\s+([-+]?[0-9]*\.?[0-9]+(?:[eE][-+]?[0-9]+)?)";

            content = Regex.Replace(content, positionPattern, match =>
            {
                float x = float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                float y = float.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                float z = float.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);

                float origX = x, origY = y, origZ = z;

                if (flipX) x = -x;
                if (flipY) y = -y;
                if (flipZ) z = -z;

                waypointCount++;

                if (verbose && waypointCount <= 10)
                {
                    Console.WriteLine($"Waypoint pos #{waypointCount}: ({origX:F2},{origY:F2},{origZ:F2}) -> ({x:F2},{y:F2},{z:F2})");
                }

                return $"position=vec3:{x.ToString("G", CultureInfo.InvariantCulture)} {y.ToString("G", CultureInfo.InvariantCulture)} {z.ToString("G", CultureInfo.InvariantCulture)}";
            });

            // Pattern 3: direction (waypoints)
            int directionCount = 0;
            string directionPattern = @"direction=vec3:([-+]?[0-9]*\.?[0-9]+(?:[eE][-+]?[0-9]+)?)\s+([-+]?[0-9]*\.?[0-9]+(?:[eE][-+]?[0-9]+)?)\s+([-+]?[0-9]*\.?[0-9]+(?:[eE][-+]?[0-9]+)?)";

            content = Regex.Replace(content, directionPattern, match =>
            {
                float x = float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                float y = float.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                float z = float.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);

                float origX = x, origY = y, origZ = z;

                // For direction vectors, we also flip them
                if (flipX) x = -x;
                if (flipY) y = -y;
                if (flipZ) z = -z;

                directionCount++;

                if (verbose && directionCount <= 10)
                {
                    Console.WriteLine($"Waypoint dir #{directionCount}: ({origX:F2},{origY:F2},{origZ:F2}) -> ({x:F2},{y:F2},{z:F2})");
                }

                return $"direction=vec3:{x.ToString("G", CultureInfo.InvariantCulture)} {y.ToString("G", CultureInfo.InvariantCulture)} {z.ToString("G", CultureInfo.InvariantCulture)}";
            });

            // Handle rotation matrices if requested
            // trafoOSToWSRot is a 3x3 rotation matrix stored as raw hex floats
            // Format: trafoOSToWSRot=raw:XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX (12 floats as hex)
            if (flipRotation)
            {
                int rotCount = 0;
                string rotPattern = @"trafoOSToWSRot=raw:([0-9a-fA-F]{96})";

                content = Regex.Replace(content, rotPattern, match =>
                {
                    string hexData = match.Groups[1].Value;
                    float[] matrix = new float[12]; // 3x3 rotation matrix + padding

                    // Parse hex to floats (each float is 8 hex chars = 4 bytes)
                    for (int i = 0; i < 12; i++)
                    {
                        string hexFloat = hexData.Substring(i * 8, 8);
                        // Convert little-endian hex to float
                        byte[] bytes = new byte[4];
                        for (int b = 0; b < 4; b++)
                        {
                            bytes[b] = Convert.ToByte(hexFloat.Substring(b * 2, 2), 16);
                        }
                        matrix[i] = BitConverter.ToSingle(bytes, 0);
                    }

                    // The matrix is stored as: [m00 m01 m02 pad] [m10 m11 m12 pad] [m20 m21 m22 pad]
                    // For X-axis flip: negate first column (m00, m10, m20)
                    // For Y-axis flip: negate second column (m01, m11, m21)
                    // For Z-axis flip: negate third column (m02, m12, m22)

                    if (flipX)
                    {
                        matrix[0] = -matrix[0];  // m00
                        matrix[4] = -matrix[4];  // m10
                        matrix[8] = -matrix[8];  // m20
                    }
                    if (flipY)
                    {
                        matrix[1] = -matrix[1];  // m01
                        matrix[5] = -matrix[5];  // m11
                        matrix[9] = -matrix[9];  // m21
                    }
                    if (flipZ)
                    {
                        matrix[2] = -matrix[2];  // m02
                        matrix[6] = -matrix[6];  // m12
                        matrix[10] = -matrix[10]; // m22
                    }

                    // Convert back to hex
                    string newHex = "";
                    for (int i = 0; i < 12; i++)
                    {
                        byte[] bytes = BitConverter.GetBytes(matrix[i]);
                        foreach (byte b in bytes)
                        {
                            newHex += b.ToString("x2");
                        }
                    }

                    rotCount++;
                    return $"trafoOSToWSRot=raw:{newHex}";
                });

                Console.WriteLine($"Modified {rotCount} rotation matrices");
            }

            // Handle bbox3DWS (bounding boxes)
            string bboxPattern = @"bbox3DWS=rawFloat:([-+]?[0-9]*\.?[0-9]+(?:[eE][-+]?[0-9]+)?)\s+([-+]?[0-9]*\.?[0-9]+(?:[eE][-+]?[0-9]+)?)\s+([-+]?[0-9]*\.?[0-9]+(?:[eE][-+]?[0-9]+)?)\s+([-+]?[0-9]*\.?[0-9]+(?:[eE][-+]?[0-9]+)?)\s+([-+]?[0-9]*\.?[0-9]+(?:[eE][-+]?[0-9]+)?)\s+([-+]?[0-9]*\.?[0-9]+(?:[eE][-+]?[0-9]+)?)";

            int bboxCount = 0;
            content = Regex.Replace(content, bboxPattern, match =>
            {
                float minX = float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                float minY = float.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                float minZ = float.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
                float maxX = float.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture);
                float maxY = float.Parse(match.Groups[5].Value, CultureInfo.InvariantCulture);
                float maxZ = float.Parse(match.Groups[6].Value, CultureInfo.InvariantCulture);

                // Flip the axes (swap and negate min/max)
                if (flipX)
                {
                    float temp = -minX;
                    minX = -maxX;
                    maxX = temp;
                }
                if (flipY)
                {
                    float temp = -minY;
                    minY = -maxY;
                    maxY = temp;
                }
                if (flipZ)
                {
                    float temp = -minZ;
                    minZ = -maxZ;
                    maxZ = temp;
                }

                bboxCount++;

                return $"bbox3DWS=rawFloat:{minX.ToString("G", CultureInfo.InvariantCulture)} {minY.ToString("G", CultureInfo.InvariantCulture)} {minZ.ToString("G", CultureInfo.InvariantCulture)} {maxX.ToString("G", CultureInfo.InvariantCulture)} {maxY.ToString("G", CultureInfo.InvariantCulture)} {maxZ.ToString("G", CultureInfo.InvariantCulture)}";
            });

            File.WriteAllText(outputFile, content, Windows1250);

            Console.WriteLine($"\nSummary:");
            Console.WriteLine($"  Object positions (trafoOSToWSPos): {modifiedCount}");
            Console.WriteLine($"  Waypoint positions: {waypointCount}");
            Console.WriteLine($"  Waypoint directions: {directionCount}");
            Console.WriteLine($"  Bounding boxes: {bboxCount}");
        }
    }
}
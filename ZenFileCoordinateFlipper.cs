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
            Console.WriteLine("  -rotation  Also flip rotation matrices");
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

            // Count keyframe sets
            var keyframeMatches = Regex.Matches(content, @"keyframes=raw:([0-9a-fA-F]+)");
            int kfSetCount = 0;
            int kfTotalCount = 0;
            foreach (Match kfMatch in keyframeMatches)
            {
                string hexData = kfMatch.Groups[1].Value;
                if (hexData.Length > 0 && hexData.Length % 56 == 0)
                {
                    kfSetCount++;
                    kfTotalCount += hexData.Length / 56;
                }
            }
            Console.WriteLine($"Mover keyframe sets: {kfSetCount} ({kfTotalCount} keyframes)");

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
            // trafoOSToWSRot is a 3x3 rotation matrix stored as 9 little-endian floats in hex (72 hex chars)
            // Layout (row-major, no padding): [m00 m01 m02] [m10 m11 m12] [m20 m21 m22]
            //
            // Correct reflection formula: R' = S * R * S, where S = diag(sx, sy, sz)
            // Element R[i][j] is negated when exactly one of axis i or axis j is flipped.
            // This preserves det(R') = +1 (valid rotation) for any input rotation.
            if (flipRotation)
            {
                int rotCount = 0;
                string rotPattern = @"trafoOSToWSRot=raw:([0-9a-fA-F]{72})";

                int[] signs = new int[] { flipX ? -1 : 1, flipY ? -1 : 1, flipZ ? -1 : 1 };

                content = Regex.Replace(content, rotPattern, match =>
                {
                    string hexData = match.Groups[1].Value;
                    float[] matrix = new float[9];

                    // Parse hex to floats (each float is 8 hex chars = 4 bytes, little-endian)
                    for (int i = 0; i < 9; i++)
                    {
                        string hexFloat = hexData.Substring(i * 8, 8);
                        byte[] bytes = new byte[4];
                        for (int b = 0; b < 4; b++)
                        {
                            bytes[b] = Convert.ToByte(hexFloat.Substring(b * 2, 2), 16);
                        }
                        matrix[i] = BitConverter.ToSingle(bytes, 0);
                    }

                    // Apply R' = S * R * S: negate element [i][j] when signs[i] * signs[j] == -1
                    for (int row = 0; row < 3; row++)
                    {
                        for (int col = 0; col < 3; col++)
                        {
                            if (signs[row] * signs[col] == -1)
                            {
                                matrix[row * 3 + col] = -matrix[row * 3 + col];
                            }
                        }
                    }

                    // Convert back to hex
                    string newHex = "";
                    for (int i = 0; i < 9; i++)
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

            // Handle keyframes (zCMover keyframes: 7 floats per keyframe)
            // Layout per keyframe: posX, posY, posZ, quatX, quatY, quatZ, quatW (56 hex chars)
            // Positions are always flipped; quaternion rotation only when -rotation is specified
            int keyframeSetCount = 0;
            int totalKeyframesFlipped = 0;
            string keyframePattern = @"keyframes=raw:([0-9a-fA-F]+)";

            content = Regex.Replace(content, keyframePattern, match =>
            {
                string hexData = match.Groups[1].Value;
                int hexPerKeyframe = 56; // 7 floats * 8 hex chars

                if (hexData.Length == 0 || hexData.Length % hexPerKeyframe != 0)
                {
                    return match.Value;
                }

                int numKeyframes = hexData.Length / hexPerKeyframe;
                StringBuilder newHex = new StringBuilder(hexData.Length);

                for (int kf = 0; kf < numKeyframes; kf++)
                {
                    int offset = kf * hexPerKeyframe;
                    float[] values = new float[7];

                    for (int i = 0; i < 7; i++)
                    {
                        string hexFloat = hexData.Substring(offset + i * 8, 8);
                        byte[] bytes = new byte[4];
                        for (int b = 0; b < 4; b++)
                        {
                            bytes[b] = Convert.ToByte(hexFloat.Substring(b * 2, 2), 16);
                        }
                        values[i] = BitConverter.ToSingle(bytes, 0);
                    }

                    // Flip positions (indices 0=posX, 1=posY, 2=posZ)
                    if (flipX) { values[0] = -values[0]; }
                    if (flipY) { values[1] = -values[1]; }
                    if (flipZ) { values[2] = -values[2]; }

                    // Flip quaternion rotation if requested
                    // Stored as (quatX, quatY, quatZ, quatW) at indices 3-6
                    // Reflection formula: R' = S * R * S where S = diag(sx, sy, sz)
                    if (flipRotation)
                    {
                        int sx = flipX ? -1 : 1;
                        int sy = flipY ? -1 : 1;
                        int sz = flipZ ? -1 : 1;

                        values[3] *= sx;            // quatX
                        values[4] *= sy;            // quatY
                        values[5] *= sz;            // quatZ
                        values[6] *= sx * sy * sz;  // quatW
                    }

                    for (int i = 0; i < 7; i++)
                    {
                        byte[] bytes = BitConverter.GetBytes(values[i]);
                        foreach (byte b in bytes)
                        {
                            newHex.Append(b.ToString("x2"));
                        }
                    }
                }

                keyframeSetCount++;
                totalKeyframesFlipped += numKeyframes;

                if (verbose && keyframeSetCount <= 10)
                {
                    Console.WriteLine($"Keyframe set #{keyframeSetCount}: {numKeyframes} keyframes flipped");
                }

                return $"keyframes=raw:{newHex}";
            });

            Console.WriteLine($"Modified {keyframeSetCount} keyframe sets ({totalKeyframesFlipped} total keyframes)");

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
            Console.WriteLine($"  Mover keyframe sets: {keyframeSetCount} ({totalKeyframesFlipped} keyframes)");
            Console.WriteLine($"  Bounding boxes: {bboxCount}");
        }
    }
}
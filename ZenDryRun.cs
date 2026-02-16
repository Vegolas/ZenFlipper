using System;
using System.IO;
using System.Text;

namespace Gothic2ZenDryRun;

class Program
{
    static void Main2(string[] args)
    {
        Console.WriteLine("=== Gothic 2 .ZEN File Dry Run Test ===\n");
        Console.WriteLine("This tool reads a .zen file and writes it back unchanged.");
        Console.WriteLine("Use this to verify that file encoding/format isn't the issue.\n");

        if (args.Length < 2)
        {
            Console.WriteLine("Usage: ZenDryRun <input.zen> <output.zen>\n");
            return;
        }

        string inputFile = args[0];
        string outputFile = args[1];

        if (!File.Exists(inputFile))
        {
            Console.WriteLine($"Error: Input file '{inputFile}' not found!");
            return;
        }

        try
        {
            Console.WriteLine($"Input:  {inputFile}");
            Console.WriteLine($"Output: {outputFile}\n");

            // Detect encoding
            byte[] bom = new byte[4];
            using (FileStream fs = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
            {
                fs.Read(bom, 0, 4);
            }

            Encoding detectedEncoding;
            if (bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
            {
                detectedEncoding = Encoding.UTF8;
                Console.WriteLine("Detected encoding: UTF-8 with BOM");
            }
            else if (bom[0] == 0xFF && bom[1] == 0xFE)
            {
                detectedEncoding = Encoding.Unicode; // UTF-16 LE
                Console.WriteLine("Detected encoding: UTF-16 LE");
            }
            else if (bom[0] == 0xFE && bom[1] == 0xFF)
            {
                detectedEncoding = Encoding.BigEndianUnicode; // UTF-16 BE
                Console.WriteLine("Detected encoding: UTF-16 BE");
            }
            else
            {
                // Try to detect if it's ASCII/Windows-1252 or UTF-8 without BOM
                detectedEncoding = DetectEncoding(inputFile);
                Console.WriteLine($"Detected encoding: {detectedEncoding.EncodingName}");
            }

            // Read the file
            string content = File.ReadAllText(inputFile, detectedEncoding);
            
            Console.WriteLine($"\nFile statistics:");
            Console.WriteLine($"  Total characters: {content.Length:N0}");
            Console.WriteLine($"  Total lines: {content.Split('\n').Length:N0}");
            
            // Analyze line endings
            int crlfCount = CountOccurrences(content, "\r\n");
            int lfOnlyCount = CountOccurrences(content.Replace("\r\n", ""), "\n");
            int crOnlyCount = CountOccurrences(content.Replace("\r\n", ""), "\r");
            
            Console.WriteLine($"\nLine ending analysis:");
            Console.WriteLine($"  CRLF (\\r\\n - Windows): {crlfCount}");
            Console.WriteLine($"  LF (\\n - Unix): {lfOnlyCount}");
            Console.WriteLine($"  CR (\\r - Old Mac): {crOnlyCount}");

            string lineEndingType = crlfCount > 0 ? "Windows (CRLF)" : 
                                   lfOnlyCount > 0 ? "Unix (LF)" : 
                                   crOnlyCount > 0 ? "Old Mac (CR)" : "Unknown";
            Console.WriteLine($"  Primary format: {lineEndingType}");

            // Check for common Gothic 2 .zen patterns
            Console.WriteLine($"\nContent validation:");
            Console.WriteLine($"  Contains 'zCWorld': {(content.Contains("zCWorld") ? "✓ Yes" : "✗ No")}");
            Console.WriteLine($"  Contains 'VobTree': {(content.Contains("VobTree") ? "✓ Yes" : "✗ No")}");
            Console.WriteLine($"  Contains 'trafoOSToWSPos': {(content.Contains("trafoOSToWSPos") ? "✓ Yes" : "✗ No")}");
            Console.WriteLine($"  Contains 'bbox3DWS': {(content.Contains("bbox3DWS") ? "✓ Yes" : "✗ No")}");

            // Write using the SAME encoding
            Console.WriteLine($"\nWriting to output file using {detectedEncoding.EncodingName}...");
            File.WriteAllText(outputFile, content, detectedEncoding);

            // Verify the output
            byte[] inputBytes = File.ReadAllBytes(inputFile);
            byte[] outputBytes = File.ReadAllBytes(outputFile);

            Console.WriteLine($"\nVerification:");
            Console.WriteLine($"  Input file size:  {inputBytes.Length:N0} bytes");
            Console.WriteLine($"  Output file size: {outputBytes.Length:N0} bytes");
            Console.WriteLine($"  Size match: {(inputBytes.Length == outputBytes.Length ? "✓ Yes" : "✗ No")}");

            bool identical = true;
            if (inputBytes.Length == outputBytes.Length)
            {
                for (int i = 0; i < inputBytes.Length; i++)
                {
                    if (inputBytes[i] != outputBytes[i])
                    {
                        identical = false;
                        Console.WriteLine($"  First difference at byte {i}: input={inputBytes[i]:X2}, output={outputBytes[i]:X2}");
                        break;
                    }
                }
            }
            else
            {
                identical = false;
            }

            if (identical)
            {
                Console.WriteLine($"  Byte-for-byte identical: ✓ Yes");
                Console.WriteLine("\n✓ SUCCESS: File written successfully with no changes.");
                Console.WriteLine("  If Gothic 2 loads this file, then file format is NOT the issue.");
                Console.WriteLine("  If Gothic 2 crashes, the original file may already be corrupted.");
            }
            else
            {
                Console.WriteLine($"  Byte-for-byte identical: ✗ No");
                Console.WriteLine("\n⚠ WARNING: Output differs from input!");
                Console.WriteLine("  This suggests encoding or line ending conversion occurred.");
                Console.WriteLine("  Try different encoding options below.");
            }

            Console.WriteLine("\n=== Encoding Options ===");
            Console.WriteLine("If the file differs, try these encoding overrides:");
            Console.WriteLine("  ZenDryRun input.zen output.zen utf8");
            Console.WriteLine("  ZenDryRun input.zen output.zen windows1252");
            Console.WriteLine("  ZenDryRun input.zen output.zen ascii");

        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n✗ Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    static Encoding DetectEncoding(string filePath)
    {
        // Read a sample to detect encoding
        byte[] buffer = new byte[Math.Min(4096, (int)new FileInfo(filePath).Length)];
        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            fs.Read(buffer, 0, buffer.Length);
        }

        // Check for extended ASCII/Windows-1252 characters
        bool hasExtendedChars = false;
        for (int i = 0; i < buffer.Length; i++)
        {
            if (buffer[i] > 127)
            {
                hasExtendedChars = true;
                break;
            }
        }

        if (!hasExtendedChars)
        {
            return Encoding.ASCII;
        }

        // Gothic 2 typically uses Windows-1252 (Western European)
        // Try to register the code page
        try
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return Encoding.GetEncoding(1252); // Windows-1252
        }
        catch
        {
            // Fall back to UTF-8 if Windows-1252 is not available
            return new UTF8Encoding(false); // UTF-8 without BOM
        }
    }

    static int CountOccurrences(string text, string pattern)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(pattern, index)) != -1)
        {
            count++;
            index += pattern.Length;
        }
        return count;
    }
}

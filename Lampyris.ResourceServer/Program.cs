namespace Lampyris.ResourceServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(
                webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseUrls("http://0.0.0.0:7015"); // 监听所有网络接口
                });
    }

    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();
            string[] args = Environment.GetCommandLineArgs();

            var resourceDirectory = Path.Combine(args.Length > 1 ? args[1] : Directory.GetCurrentDirectory(), "Resources");

            // 确保资源目录存在
            if (!Directory.Exists(resourceDirectory))
            {
                Directory.CreateDirectory(resourceDirectory);
            }

            app.UseEndpoints(endpoints =>
            {
                // 提供版本信息
                endpoints.MapGet("/version/{platform}", async context =>
                {
                    var platform = context.Request.RouteValues["platform"]?.ToString();
                    if (string.IsNullOrEmpty(platform))
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync("Invalid param");
                        return;
                    }

                    var versionFile = Path.Combine(resourceDirectory, platform, "version.json");
                    if (!File.Exists(versionFile))
                    {
                        var responseText = "{}";
                        var responseBytes = System.Text.Encoding.UTF8.GetBytes(responseText);
                        context.Response.ContentType = "text/plain; charset=utf-8";
                        await context.Response.Body.WriteAsync(responseBytes, 0, responseBytes.Length);
                        return;
                    }

                    var versionJson = await File.ReadAllTextAsync(versionFile);
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(versionJson);
                });

                // 提供资源文件下载
                endpoints.MapGet("/download/{platform}/{fileName}", async context =>
                {
                    var fileName = context.Request.RouteValues["fileName"]?.ToString();
                    var platform = context.Request.RouteValues["platform"]?.ToString();

                    if (!string.IsNullOrEmpty(fileName) && !string.IsNullOrEmpty(platform))
                    {
                        var filePath = Path.Combine(resourceDirectory, platform, fileName);

                        if (!File.Exists(filePath))
                        {
                            context.Response.StatusCode = StatusCodes.Status404NotFound;
                            await context.Response.WriteAsync("File not found");
                            return;
                        }

                        context.Response.ContentType = "application/octet-stream";
                        await context.Response.SendFileAsync(filePath);
                    }
                    else
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync("Invalid param");
                    }
                });

                // 提供资源文件上传
                endpoints.MapPost("/upload", async context =>
                {
                    // 检查请求是否包含文件
                    if (!context.Request.HasFormContentType)
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync("Invalid form content type");
                        return;
                    }

                    var form = await context.Request.ReadFormAsync();
                    var file = form.Files["lampyris_resource_file"]; // 获取上传的文件
                    var platform = form["platform"].ToString(); // 获取资源所对应的平台(Standalone/Android/IOS)

                    if (file == null)
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync("No file uploaded");
                        return;
                    }

                    if (string.IsNullOrEmpty(platform))
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync("No specified platform, upload prohibited");
                        return;
                    }

                    // 保存文件到资源目录
                    var filePath = Path.Combine(resourceDirectory, platform, file.FileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    context.Response.StatusCode = StatusCodes.Status200OK;
                    await context.Response.WriteAsync($"File '{file.FileName}' uploaded successfully");
                });
            });
        }
    }
}

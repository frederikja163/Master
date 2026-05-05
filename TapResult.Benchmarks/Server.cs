using System.Net;
using System.Threading;

namespace TapResult.Benchmarks;

internal static class Server
{
    public const string DefaultUrl = "http://localhost:8080";

    private static long _bytesSent = 0;
    private static int _counter = 0;

    public static async Task<byte[]> ReadFileAsync(string serverUrl, string filename)
    {
        using var client = new HttpClient();
        return await client.GetByteArrayAsync($"{serverUrl.TrimEnd('/')}/file/{filename}");
    }

    public static async Task<byte[]> ReadRangeAsync(string serverUrl, string filename, long start, long length)
    {
        using var client = new HttpClient();
        var response = await client.GetAsync($"{serverUrl.TrimEnd('/')}/range/{filename}?start={start}&length={length}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }

    public static async Task<long> GetFileStatsAsync(string serverUrl)
    {
        using var client = new HttpClient();
        var result = await client.GetStringAsync($"{serverUrl.TrimEnd('/')}/stats");
        return long.Parse(result);
    }

    public static async Task ResetStatsAsync(string serverUrl)
    {
        using var client = new HttpClient();
        await client.GetAsync($"{serverUrl.TrimEnd('/')}/reset");
    }

    public static async Task<long> IncrementFileAsync(string serverUrl)
    {
        using var client = new HttpClient();
        var result = await client.GetStringAsync($"{serverUrl.TrimEnd('/')}/increment");
        return long.Parse(result);
    }

    internal static void StartHttpServer()
    {
        string prefix = DefaultUrl + "/";
        string serveDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Read");

        using HttpListener listener = new();
        listener.Prefixes.Add(prefix);
        listener.Start();

        Console.WriteLine($"HTTP Server started on {prefix}");
        Console.WriteLine($"Serving files from: {serveDirectory}");
        Console.WriteLine("Endpoints:");
        Console.WriteLine("  GET /file/{filename} - Get entire file");
        Console.WriteLine("  GET /range/{filename}?start={start}&length={length} - Get byte range");
        Console.WriteLine("  GET /stats - Get total bytes sent");
        Console.WriteLine("  GET /increment - Increment counter");
        Console.WriteLine("  GET /reset - Reset all counters");
        Console.WriteLine("Press Ctrl+C to stop.");

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) => { cts.Cancel(); e.Cancel = true; };

        while (!cts.IsCancellationRequested)
        {
            try
            {
                var contextTask = listener.GetContextAsync();
                contextTask.Wait(cts.Token);
                var context = contextTask.Result;
                Task.Run((Action)(() => HandleRequest(context, serveDirectory)), cts.Token);
            }
            catch (OperationCanceledException) { break; }
            catch (HttpListenerException) when (cts.IsCancellationRequested) { break; }
        }

        listener.Stop();
    }

    private static void HandleRequest(HttpListenerContext context, string serveDirectory)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            Console.WriteLine($"[REQUEST] {request.HttpMethod} {request.Url?.AbsoluteUri}");

            string[] segments = request.Url!.Segments.Select(s => s.Trim('/')).Where(s => !string.IsNullOrEmpty(s)).ToArray();

            if (segments.Length < 1)
            {
                Console.WriteLine($"[WARN] Bad request: no segments in URL");
                response.StatusCode = 400;
                return;
            }

            switch (segments[0])
            {
                case "file" when segments.Length >= 2:
                    Console.WriteLine($"[FILE] Serving file: {segments[1]}");
                    HandleGetFile(response, segments[1], serveDirectory);
                    Console.WriteLine($"[FILE] Completed serving: {segments[1]}");
                    break;
                case "range" when segments.Length >= 2:
                    Console.WriteLine($"[RANGE] Serving range for file: {segments[1]}");
                    HandleGetFileRange(request, response, segments[1], serveDirectory);
                    Console.WriteLine($"[RANGE] Completed range for: {segments[1]}");
                    break;
                case "stats" when segments.Length == 1:
                    Console.WriteLine($"[STATS] Getting stats");
                    HandleGetStats(response);
                    Console.WriteLine($"[STATS] Stats sent");
                    break;
                case "increment" when segments.Length == 1:
                    Console.WriteLine($"[INCREMENT] Incrementing counter");
                    HandleIncrement(response);
                    Console.WriteLine($"[INCREMENT] Counter incremented");
                    break;
                case "reset" when segments.Length == 1:
                    Console.WriteLine($"[RESET] Resetting all counters");
                    HandleReset(response);
                    Console.WriteLine($"[RESET] All counters reset");
                    break;
                default:
                    Console.WriteLine($"[WARN] 404 Not Found: {request.Url.AbsoluteUri}");
                    response.StatusCode = 404;
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] {ex.GetType().Name}: {ex.Message}");
            response.StatusCode = 500;
            byte[] errorBytes = System.Text.Encoding.UTF8.GetBytes(ex.Message);
            response.OutputStream.Write(errorBytes, 0, errorBytes.Length);
        }
        finally
        {
            response.OutputStream.Close();
        }
    }

    private static void HandleGetFile(HttpListenerResponse response, string filename, string serveDirectory)
    {
        string filePath = Path.Combine(serveDirectory, filename);
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"[FILE] File not found: {filePath}");
            response.StatusCode = 404;
            return;
        }

        byte[] fileBytes = File.ReadAllBytes(filePath);
        Interlocked.Add(ref _bytesSent, fileBytes.Length);
        Console.WriteLine($"[FILE] Sending {fileBytes.Length} bytes for {filename} (Total: {Interlocked.Read(ref _bytesSent)} bytes)");
        response.StatusCode = 200;
        response.ContentType = "application/octet-stream";
        response.ContentLength64 = fileBytes.Length;
        response.OutputStream.Write(fileBytes, 0, fileBytes.Length);
    }

    private static void HandleGetFileRange(HttpListenerRequest request, HttpListenerResponse response, string filename, string serveDirectory)
    {
        string filePath = Path.Combine(serveDirectory, filename);
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"[RANGE] File not found: {filePath}");
            response.StatusCode = 404;
            return;
        }

        string? startStr = request.QueryString["start"];
        string? lengthStr = request.QueryString["length"];

        if (string.IsNullOrEmpty(startStr) || string.IsNullOrEmpty(lengthStr) ||
            !long.TryParse(startStr, out long start) || !long.TryParse(lengthStr, out long length))
        {
            Console.WriteLine($"[RANGE] Bad request: start={startStr}, length={lengthStr}");
            response.StatusCode = 400;
            return;
        }

        using FileStream fs = File.OpenRead(filePath);
        fs.Seek(start, start > 0 ? SeekOrigin.Begin : SeekOrigin.End);
        byte[] buffer = new byte[length];
        int bytesRead = fs.Read(buffer, 0, (int)Math.Min(length, buffer.Length));
        Interlocked.Add(ref _bytesSent, bytesRead);
        Console.WriteLine($"[RANGE] Sending {bytesRead} bytes (start={start}, requested={length}) for {filename} (Total: {Interlocked.Read(ref _bytesSent)} bytes)");

        response.StatusCode = 200;
        response.ContentType = "application/octet-stream";
        response.ContentLength64 = bytesRead;
        response.OutputStream.Write(buffer, 0, bytesRead);
    }

    private static void HandleGetStats(HttpListenerResponse response)
    {
        long bytesSent = Interlocked.Read(ref _bytesSent);
        int counter = Interlocked.CompareExchange(ref _counter, 0, 0);
        long average = counter > 0 ? bytesSent / counter : 0;
        Console.WriteLine($"[STATS] BytesSent={bytesSent}, Counter={counter}, Average={average}");
        byte[] result = System.Text.Encoding.UTF8.GetBytes(average.ToString());
        response.StatusCode = 200;
        response.ContentType = "text/plain";
        response.ContentLength64 = result.Length;
        response.OutputStream.Write(result, 0, result.Length);
    }

    private static void HandleIncrement(HttpListenerResponse response)
    {
        int count = Interlocked.Increment(ref _counter);
        Console.WriteLine($"[INCREMENT] Counter now: {count}");
        byte[] result = System.Text.Encoding.UTF8.GetBytes(count.ToString());
        response.StatusCode = 200;
        response.ContentType = "text/plain";
        response.ContentLength64 = result.Length;
        response.OutputStream.Write(result, 0, result.Length);
    }

    private static void HandleReset(HttpListenerResponse response)
    {
        long previousBytesSent = Interlocked.Read(ref _bytesSent);
        int previousCounter = Interlocked.CompareExchange(ref _counter, 0, 0);
        Interlocked.Exchange(ref _bytesSent, 0);
        Interlocked.Exchange(ref _counter, 0);
        Console.WriteLine($"[RESET] Reset complete (was: BytesSent={previousBytesSent}, Counter={previousCounter})");
        byte[] result = System.Text.Encoding.UTF8.GetBytes("All counters reset");
        response.StatusCode = 200;
        response.ContentType = "text/plain";
        response.ContentLength64 = result.Length;
        response.OutputStream.Write(result, 0, result.Length);
    }
}

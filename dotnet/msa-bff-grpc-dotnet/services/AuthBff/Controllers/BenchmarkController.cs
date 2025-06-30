using Auth;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace AuthBff.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BenchmarkController(UserService.UserServiceClient grpcClient, IHttpClientFactory httpClientFactory) : ControllerBase
{
    private readonly UserService.UserServiceClient _grpcClient = grpcClient;
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("UserApi");

    [HttpGet]
    public async Task<IActionResult> CompareGrpcAndHttp()
    {
        var result = new Dictionary<string, object>();

        // gRPC 요청
        var swGrpc = Stopwatch.StartNew();
        var grpcResponse = await _grpcClient.GetUsersAsync(new Empty());
        swGrpc.Stop();
        var grpcSize = grpcResponse.CalculateSize();

        result["gRPC_요청_시간_ms"] = swGrpc.ElapsedMilliseconds;
        result["gRPC_데이터_수"] = grpcResponse.Users.Count;
        result["gRPC_응답_크기_바이트"] = grpcSize;

        // HTTP 요청
        var swHttp = Stopwatch.StartNew();
        var httpResponse = await _httpClient.GetAsync("api/UserHttp");
        var httpBytes = await httpResponse.Content.ReadAsByteArrayAsync(); // 정확한 크기 측정
        var httpData = await httpResponse.Content.ReadFromJsonAsync<UserList>();
        swHttp.Stop();

        result["HTTP_요청_시간_ms"] = swHttp.ElapsedMilliseconds;
        result["HTTP_데이터_수"] = httpData?.Users?.Count ?? 0;
        result["HTTP_응답_크기_바이트"] = httpBytes.Length;

        return Ok(result);
    }

    [HttpGet("stress")]
    public async Task<IActionResult> StressTest([FromQuery] int concurrent = 100)
    {
        var grpcSizes = new ConcurrentBag<long>();
        var grpcStopwatch = Stopwatch.StartNew();

        var grpcTasks = Enumerable.Range(0, concurrent).Select(async _ =>
        {
            var res = await _grpcClient.GetUsersAsync(new Empty());
            grpcSizes.Add(res.CalculateSize());
        });
        await Task.WhenAll(grpcTasks);
        grpcStopwatch.Stop();

        var httpSizes = new ConcurrentBag<long>();
        var httpStopwatch = Stopwatch.StartNew();

        var httpTasks = Enumerable.Range(0, concurrent).Select(async _ =>
        {
            var res = await _httpClient.GetAsync("api/UserHttp");
            var bytes = await res.Content.ReadAsByteArrayAsync();
            httpSizes.Add(bytes.Length);
        });
        await Task.WhenAll(httpTasks);
        httpStopwatch.Stop();

        return Ok(new
        {
            gRPC_요청_수 = concurrent,
            gRPC_총_시간_ms = grpcStopwatch.ElapsedMilliseconds,
            gRPC_총_응답_크기_바이트 = grpcSizes.Sum(),
            HTTP_요청_수 = concurrent,
            HTTP_총_시간_ms = httpStopwatch.ElapsedMilliseconds,
            HTTP_총_응답_크기_바이트 = httpSizes.Sum()
        });
    }

    [HttpGet("metrics")]
    public async Task<IActionResult> BenchmarkWithMetrics([FromQuery] int count = 100)
    {
        var grpcTimes = new List<double>();
        var grpcSizes = new List<long>();

        var httpTimes = new List<double>();
        var httpSizes = new List<long>();

        // gRPC - 첫 요청 따로
        await _grpcClient.GetUsersAsync(new Empty()); // 워밍업

        for (int i = 1; i < count; i++)
        {
            var sw = Stopwatch.StartNew();
            var response = await _grpcClient.GetUsersAsync(new Empty());
            sw.Stop();
            grpcTimes.Add(sw.Elapsed.TotalMilliseconds);
            grpcSizes.Add(response.CalculateSize());
        }

        // HTTP - 첫 요청 따로
        await _httpClient.GetAsync("api/UserHttp"); // 워밍업

        for (int i = 1; i < count; i++)
        {
            var sw = Stopwatch.StartNew();
            var res = await _httpClient.GetAsync("api/UserHttp");
            var bytes = await res.Content.ReadAsByteArrayAsync();
            sw.Stop();
            httpTimes.Add(sw.Elapsed.TotalMilliseconds);
            httpSizes.Add(bytes.Length);
        }

        return Ok(new
        {
            gRPC_결과 = BuildKoreanMetrics(grpcTimes, grpcSizes),
            HTTP_결과 = BuildKoreanMetrics(httpTimes, httpSizes)
        });
    }
    [HttpPost("streaming")]
    public async Task<IActionResult> BenchmarkStreaming([FromBody] List<string> names)
    {
        var times = new List<double>();
        var sizes = new List<long>();

        var stopwatchTotal = Stopwatch.StartNew();

        using var call = _grpcClient.ChatUsersByName();

        // 응답 읽기 태스크
        var receiveTask = Task.Run(async () =>
        {
            await foreach (var reply in call.ResponseStream.ReadAllAsync())
            {
                // 응답 받은 시점 기준
                sizes.Add(reply.CalculateSize());
            }
        });

        // 요청 보내기 (타이밍 측정)
        foreach (var name in names)
        {
            var sw = Stopwatch.StartNew();
            await call.RequestStream.WriteAsync(new GetUserByNameRequest { Name = name });
            sw.Stop();
            times.Add(sw.Elapsed.TotalMilliseconds);
        }

        await call.RequestStream.CompleteAsync();
        await receiveTask;

        stopwatchTotal.Stop();

        return Ok(new
        {
            총_요청_수 = names.Count,
            총_시간_ms = stopwatchTotal.ElapsedMilliseconds,
            결과_메트릭 = BuildKoreanMetrics(times, sizes)
        });
    }


    private object BuildKoreanMetrics(List<double> times, List<long> sizes)
    {
        var sortedTimes = times.OrderBy(t => t).ToList();
        var sortedSizes = sizes.OrderBy(s => s).ToList();

        return new
        {
            요청_횟수 = sortedTimes.Count,
            최초_응답_시간_ms = Math.Round(sortedTimes.First(), 2),
            평균_응답_시간_ms = Math.Round(sortedTimes.Average(), 2),
            최소_응답_시간_ms = Math.Round(sortedTimes.Min(), 2),
            최대_응답_시간_ms = Math.Round(sortedTimes.Max(), 2),
            중앙값_응답_시간_ms = Math.Round(sortedTimes[sortedTimes.Count / 2], 2),
            p90_응답_시간_ms = Math.Round(sortedTimes[(int)(sortedTimes.Count * 0.9)], 2),
            p95_응답_시간_ms = Math.Round(sortedTimes[(int)(sortedTimes.Count * 0.95)], 2),
            평균_응답_크기_바이트 = Math.Round(sortedSizes.Average(), 2),
            최소_응답_크기_바이트 = sortedSizes.Min(),
            최대_응답_크기_바이트 = sortedSizes.Max(),
            총_응답_크기_바이트 = sortedSizes.Sum()
        };
    }

    public class UserReply
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class UserList
    {
        public List<UserReply> Users { get; set; } = [];
    }
}

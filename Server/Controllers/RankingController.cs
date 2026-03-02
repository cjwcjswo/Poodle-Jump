using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace PoodleJump.RankingApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RankingController : ControllerBase
{
    private const string LeaderboardKey = "Leaderboard";
    private const int TopCount = 10;
    private const int MarkersCount = 5;

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RankingController> _logger;

    public RankingController(IConnectionMultiplexer redis, ILogger<RankingController> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    private IDatabase Db => _redis.GetDatabase();

    /// <summary>
    /// 유저 닉네임과 점수를 제출하여 리더보드에 반영합니다. 기존 점수가 없거나 새 점수가 더 클 때만 갱신됩니다.
    /// </summary>
    [HttpPost("SubmitScore")]
    public async Task<IActionResult> SubmitScore([FromBody] SubmitScoreRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request?.Nickname))
            return BadRequest("Nickname is required.");
        if (request.Score < 0)
            return BadRequest("Score must be non-negative.");

        var member = request.Nickname.Trim();
        var score = (double)request.Score;

        RedisValue existingRedis = await Db.SortedSetScoreAsync(LeaderboardKey, member);
        bool hasExisting = !existingRedis.IsNull;
        double existingScore = hasExisting ? (double)existingRedis : 0;
        if (hasExisting && existingScore >= score)
        {
            _logger.LogInformation("Score rejected (existing higher): {Nickname} submitted {Score}, existing {Existing}", member, request.Score, (long)existingScore);
            return Ok(new { nickname = member, score = (long)existingScore, message = "Existing score is higher" });
        }

        await Db.SortedSetAddAsync(LeaderboardKey, member, score, When.Always);

        _logger.LogInformation("Score updated: {Nickname} = {Score}", member, request.Score);
        return Ok(new { nickname = member, score = request.Score, message = "Score updated" });
    }

    /// <summary>
    /// 상위 10명의 유저 리스트(닉네임, 점수)를 반환합니다.
    /// </summary>
    [HttpGet("GetTopRankings")]
    public async Task<ActionResult<IEnumerable<RankingEntry>>> GetTopRankings(CancellationToken cancellationToken = default)
    {
        var entries = await Db.SortedSetRangeByRankWithScoresAsync(LeaderboardKey, 0, TopCount - 1, Order.Descending);

        var result = entries
            .Select((e, i) => new RankingEntry
            {
                Rank = i + 1,
                Nickname = e.Element.ToString(),
                Score = (long)e.Score
            })
            .ToList();

        return Ok(result);
    }

    /// <summary>
    /// 현재 플레이어 점수보다 높은 유저들 중 가장 가까운 5명(마커용)을 반환합니다.
    /// </summary>
    [HttpGet("GetMarkers")]
    public async Task<ActionResult<IEnumerable<RankingEntry>>> GetMarkers([FromQuery] long currentScore, CancellationToken cancellationToken = default)
    {
        if (currentScore < 0)
            return BadRequest("currentScore must be non-negative.");

        // 점수가 currentScore 초과인 멤버 중 점수 오름차순(가장 가까운) 5명
        var minScore = currentScore + 0.01;
        var entries = await Db.SortedSetRangeByScoreWithScoresAsync(LeaderboardKey, minScore, double.PositiveInfinity, skip: 0, take: MarkersCount, order: Order.Ascending);

        var result = new List<RankingEntry>();
        foreach (var e in entries)
        {
            var zeroBasedRank = await Db.SortedSetRankAsync(LeaderboardKey, e.Element, Order.Descending);
            var rank = zeroBasedRank.HasValue ? (int)zeroBasedRank.Value + 1 : 0;
            result.Add(new RankingEntry
            {
                Rank = rank,
                Nickname = e.Element.ToString(),
                Score = (long)e.Score
            });
        }

        return Ok(result);
    }
}

public record SubmitScoreRequest(string? Nickname, long Score);

public record RankingEntry
{
    public int Rank { get; init; }
    public string Nickname { get; init; } = "";
    public long Score { get; init; }
}

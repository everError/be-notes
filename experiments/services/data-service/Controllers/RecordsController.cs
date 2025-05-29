using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using data_service.Data;
using data_service.Models;

namespace data_service.Controllers;

[ApiController]
[Route("api/data/records")]
public class RecordsController(AppDbContext db) : ControllerBase
{
    private readonly AppDbContext _db = db;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Record record)
    {
        // 버전은 기본 생성자로 Guid.NewGuid() 자동 부여됨
        await _db.Records.AddAsync(record);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Record created",
            record
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var records = await _db.Records.ToListAsync();
        return Ok(records);
    }

    [HttpPut("{id}/increment")]
    public async Task<IActionResult> IncrementCount([FromRoute] int id, [FromHeader(Name = "If-Match")] Guid version)
    {
        var record = await _db.Records.FirstOrDefaultAsync(r => r.Id == id);

        if (record == null)
            return NotFound(new { message = "Record not found" });

        if (record.Version != version)
            return Conflict(new { message = "Concurrency conflict", currentVersion = record.Version });

        record.Count += 1;

        try
        {
            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Count incremented",
                newCount = record.Count,
                newVersion = record.Version
            });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Concurrency update failed" });
        }
    }
    [HttpPut("{id}/increment-retry")]
    public async Task<IActionResult> IncrementCountWithRetry([FromRoute] int id)
    {
        const int maxRetry = 5;
        int attempt = 0;
        bool saved = false;

        while (!saved && attempt < maxRetry)
        {
            attempt++;

            var record = await _db.Records.FirstOrDefaultAsync(r => r.Id == id);
            if (record == null)
                return NotFound(new { message = "Record not found" });

            record.Count += 1;

            try
            {
                await _db.SaveChangesAsync();
                saved = true;

                return Ok(new
                {
                    message = $"Count incremented after {attempt} attempt(s)",
                    newCount = record.Count,
                    newVersion = record.Version
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                        // 저장 실패 시 변경내용 무효화 후 재시도
                foreach (var entry in _db.ChangeTracker.Entries())
                {
                    if (entry.State == EntityState.Modified)
                    {
                        await entry.ReloadAsync(); // DB 값으로 다시 로드
                    }
                }
            }
        }

        return Conflict(new
        {
            message = $"Failed to increment after {maxRetry} attempts"
        });
    }

}

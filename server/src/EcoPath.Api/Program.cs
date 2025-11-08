using EcoPath.Infrastructure.Data;
using EcoPath.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using EcoPath.Domain.Entities;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// EF Core (SQL Server)
builder.Services.AddDbContext<EcoPathDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"))
);

// CORS - allow local frontend during development
builder.Services.AddCors(options =>
{
    options.AddPolicy("Dev", policy =>
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseCors("Dev");

// Minimal global exception handler returning problem details
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
        var problem = Results.Problem(title: "An unexpected error occurred.", detail: feature?.Error.Message, statusCode: StatusCodes.Status500InternalServerError);
        await problem.ExecuteAsync(context);
    });
});

// Apply migrations and seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EcoPathDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db);
}

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

// Redirect root to Swagger UI for convenience
app.MapGet("/", () => Results.Redirect("/swagger"));
// Serve report index
app.MapGet("/report", () => Results.Redirect("/report/index.html"));

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

// v1 API
var api = app.MapGroup("/api/v1");

// Emission Factors
api.MapGet("/factors", async (EcoPathDbContext db) =>
{
    var items = await db.EmissionFactors.AsNoTracking().ToListAsync();
    return Results.Ok(items);
})
.WithName("GetFactors")
.WithTags("Factors")
.WithOpenApi();

// Dashboard summary (basic sample using counts)
api.MapGet("/dashboard/summary", async (EcoPathDbContext db) =>
{
    var mines = await db.Mines.CountAsync();
    var factors = await db.EmissionFactors.CountAsync();
    return Results.Ok(new { mines, factors });
})
.WithName("DashboardSummary")
.WithTags("Dashboard")
.WithOpenApi();

// Profile
api.MapGet("/profile", async (EcoPathDbContext db) =>
{
    var p = await db.Profiles.AsNoTracking().FirstOrDefaultAsync();
    return p is null ? Results.NotFound() : Results.Ok(p);
})
.WithTags("Profile")
.WithOpenApi();

api.MapPost("/profile", async (EcoPathDbContext db, ProfileDto dto) =>
{
    if (string.IsNullOrWhiteSpace(dto.MineName) || string.IsNullOrWhiteSpace(dto.MineId))
        return Results.BadRequest("MineName and MineId are required");
    var existing = await db.Profiles.FirstOrDefaultAsync(p => p.MineId == dto.MineId);
    if (existing is null)
    {
        var p = new Profile { MineName = dto.MineName.Trim(), MineId = dto.MineId.Trim(), Location = dto.Location, Area = dto.Area, Email = dto.Email, Phone = dto.Phone };
        db.Profiles.Add(p);
        await db.SaveChangesAsync();
        return Results.Created($"/api/v1/profile", p);
    }
    else
    {
        existing.MineName = dto.MineName.Trim();
        existing.Location = dto.Location;
        existing.Area = dto.Area;
        existing.Email = dto.Email;
        existing.Phone = dto.Phone;
        await db.SaveChangesAsync();
        return Results.Ok(existing);
    }
})
.WithTags("Profile")
.WithOpenApi();

api.MapPut("/profile", async (EcoPathDbContext db, ProfileDto dto) =>
{
    if (string.IsNullOrWhiteSpace(dto.MineId)) return Results.BadRequest("MineId is required");
    var p = await db.Profiles.FirstOrDefaultAsync(x => x.MineId == dto.MineId);
    if (p is null)
    {
        p = new Profile { MineName = dto.MineName ?? string.Empty, MineId = dto.MineId.Trim(), Location = dto.Location, Area = dto.Area, Email = dto.Email, Phone = dto.Phone };
        db.Profiles.Add(p);
    }
    else
    {
        if (!string.IsNullOrWhiteSpace(dto.MineName)) p.MineName = dto.MineName.Trim();
        p.Location = dto.Location;
        p.Area = dto.Area;
        p.Email = dto.Email;
        p.Phone = dto.Phone;
    }
    await db.SaveChangesAsync();
    return Results.Ok(p);
})
.WithTags("Profile")
.WithOpenApi();

// Mines
api.MapGet("/mines", async (EcoPathDbContext db) =>
{
    var items = await db.Mines.AsNoTracking().ToListAsync();
    return Results.Ok(items);
})
.WithName("GetMines")
.WithTags("Mines")
.WithOpenApi();

api.MapPost("/mines", async (EcoPathDbContext db, CreateMineDto dto) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name)) return Results.BadRequest("Name is required");
    var mine = new Mine { Name = dto.Name.Trim(), Type = dto.Type, Degree = dto.Degree, Location = dto.Location };
    db.Mines.Add(mine);
    await db.SaveChangesAsync();
    return Results.Created($"/api/v1/mines/{mine.Id}", mine);
})
.WithName("CreateMine")
.WithTags("Mines")
.WithOpenApi();

// Mines: update
api.MapPut("/mines/{id:int}", async (EcoPathDbContext db, int id, CreateMineDto dto) =>
{
    var mine = await db.Mines.FindAsync(id);
    if (mine is null) return Results.NotFound();
    if (string.IsNullOrWhiteSpace(dto.Name)) return Results.BadRequest("Name is required");
    mine.Name = dto.Name.Trim();
    mine.Type = dto.Type;
    mine.Degree = dto.Degree;
    mine.Location = dto.Location;
    await db.SaveChangesAsync();
    return Results.Ok(mine);
})
.WithName("UpdateMine")
.WithTags("Mines")
.WithOpenApi();

// Mines: delete
api.MapDelete("/mines/{id:int}", async (EcoPathDbContext db, int id) =>
{
    var mine = await db.Mines.FindAsync(id);
    if (mine is null) return Results.NotFound();
    db.Mines.Remove(mine);
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.WithName("DeleteMine")
.WithTags("Mines")
.WithOpenApi();

// Calculator - aggregate
api.MapPost("/calc/aggregate", async (EcoPathDbContext db, CalcRequest req) =>
{
    if (req?.Activities == null || req.Activities.Count == 0)
        return Results.BadRequest("No activities provided");

    var results = new List<ActivityResult>();
    double total = 0;

    foreach (var a in req.Activities)
    {
        var code = (a.FuelType ?? "").Trim().ToLowerInvariant();
        if (code == "electricity" || code == "grid") code = "grid"; // map electricity to grid factor

        var factor = await db.EmissionFactors
            .AsNoTracking()
            .Where(f => f.Code == code)
            .OrderByDescending(f => f.EffectiveFrom)
            .Select(f => f.Factor)
            .FirstOrDefaultAsync();

        // if not found, 0 factor
        var tons = (a.Quantity * factor) / 1000.0; // factors stored in kg per unit → tons
        if (double.IsNaN(tons) || double.IsInfinity(tons)) tons = 0;
        total += tons;
        results.Add(new ActivityResult(a.Activity, code, a.Quantity, a.Unit, Math.Round(tons, 4)));
    }

    return Results.Ok(new { totalTons = Math.Round(total, 4), items = results });
})
.WithName("CalcAggregate")
.WithTags("Calculator")
.WithOpenApi();

// Pathways - aggregate reductions
api.MapPost("/pathways/aggregate", (PathwaysRequest req) =>
{
    var evTons = Math.Max(0, req.EvCount) * 4.0; // EV_DISPLACED_TONS_PER_YEAR
    var mwhYear = Math.Max(0, req.ReMW) * 8760 * 0.35 * Math.Max(0, Math.Min(100, req.RePct)) / 100.0; // RENEW_CF
    var reTons = mwhYear * 0.75; // GRID_TON_PER_MWH (0.75 tCO2e/MWh)
    var mcTons = Math.Max(0, req.McCH4) * 27.0; // GWP_CH4_CAPTURE
    var vamTons = Math.Max(0, req.VamCH4) * 20.0; // GWP_CH4_VAM
    var total = evTons + reTons + mcTons + vamTons;
    return Results.Ok(new { evTons = Math.Round(evTons,2), reTons = Math.Round(reTons,2), mcTons = Math.Round(mcTons,2), vamTons = Math.Round(vamTons,2), totalTons = Math.Round(total,2) });
})
.WithName("PathwaysAggregate")
.WithTags("Pathways")
.WithOpenApi();

// Emission Factors CRUD (admin)
api.MapGet("/factors/{id:int}", async (EcoPathDbContext db, int id) =>
{
    var item = await db.EmissionFactors.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
    return item is null ? Results.NotFound() : Results.Ok(item);
})
.WithName("GetFactorById")
.WithTags("Factors")
.WithOpenApi();

api.MapPost("/factors", async (EcoPathDbContext db, FactorCreateUpdateDto dto) =>
{
    if (string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(dto.Unit))
        return Results.BadRequest("Code and Unit are required");
    var ef = new EmissionFactor
    {
        Category = (FactorCategory)dto.Category,
        Code = dto.Code.Trim().ToLowerInvariant(),
        Unit = dto.Unit.Trim(),
        Factor = dto.Factor,
        EffectiveFrom = dto.EffectiveFrom == default ? DateTime.UtcNow.Date : dto.EffectiveFrom,
        EffectiveTo = dto.EffectiveTo
    };
    db.EmissionFactors.Add(ef);
    await db.SaveChangesAsync();
    return Results.Created($"/api/v1/factors/{ef.Id}", ef);
})
.WithName("CreateFactor")
.WithTags("Factors")
.WithOpenApi();

api.MapPut("/factors/{id:int}", async (EcoPathDbContext db, int id, FactorCreateUpdateDto dto) =>
{
    var ef = await db.EmissionFactors.FindAsync(id);
    if (ef is null) return Results.NotFound();
    if (string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(dto.Unit))
        return Results.BadRequest("Code and Unit are required");
    ef.Category = (FactorCategory)dto.Category;
    ef.Code = dto.Code.Trim().ToLowerInvariant();
    ef.Unit = dto.Unit.Trim();
    ef.Factor = dto.Factor;
    ef.EffectiveFrom = dto.EffectiveFrom == default ? ef.EffectiveFrom : dto.EffectiveFrom;
    ef.EffectiveTo = dto.EffectiveTo;
    await db.SaveChangesAsync();
    return Results.Ok(ef);
})
.WithName("UpdateFactor")
.WithTags("Factors")
.WithOpenApi();

api.MapDelete("/factors/{id:int}", async (EcoPathDbContext db, int id) =>
{
    var ef = await db.EmissionFactors.FindAsync(id);
    if (ef is null) return Results.NotFound();
    db.EmissionFactors.Remove(ef);
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.WithName("DeleteFactor")
.WithTags("Factors")
.WithOpenApi();

// Offset aggregate (afforestation + credits)
api.MapPost("/offsets/aggregate", (OffsetRequest req) =>
{
    double area = Math.Max(0, req.AreaHectares);
    double tph = Math.Max(0, req.TreesPerHectare);
    double years = Math.Max(1, req.Years);
    const double TREE_CO2_TON_PER_YEAR = 0.022; // tons per tree per year
    var trees = area * tph;
    var annualTons = trees * TREE_CO2_TON_PER_YEAR;
    var totalOverYears = annualTons * years;
    var credits = totalOverYears; // 1 credit per ton
    var value = credits * Math.Max(0, req.MarketRate);
    return Results.Ok(new { annualTons = Math.Round(annualTons,2), totalTons = Math.Round(totalOverYears,2), credits = Math.Round(credits,2), value = Math.Round(value,2) });
})
.WithName("OffsetAggregate")
.WithTags("Offset")
.WithOpenApi();

// Health endpoints
app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
   .WithTags("Health").WithOpenApi();
app.MapGet("/ready", () => Results.Ok(new { status = "ready" }))
   .WithTags("Health").WithOpenApi();

// Reports
api.MapPost("/reports", async (EcoPathDbContext db, ReportCreateDto dto) =>
{
    var report = new Report
    {
        Title = string.IsNullOrWhiteSpace(dto.Title) ? "Untitled" : dto.Title.Trim(),
        PeriodStart = dto.PeriodStart,
        PeriodEnd = dto.PeriodEnd,
        CreatedAt = DateTime.UtcNow
    };
    db.Reports.Add(report);
    await db.SaveChangesAsync();
    return Results.Created($"/api/v1/reports/{report.Id}", new { report.Id });
}).WithTags("Reports").WithOpenApi();

// Reports helper: latest summary
api.MapGet("/reports/latest/summary", async (EcoPathDbContext db) =>
{
    var report = await db.Reports
        .OrderByDescending(r => r.CreatedAt)
        .Include(r => r.CalcEntries)
        .Include(r => r.PathwaysEntries)
        .Include(r => r.OffsetEntries)
        .FirstOrDefaultAsync();
    if (report is null) return Results.NotFound();

    double totalEmissions = 0;
    foreach (var a in report.CalcEntries)
    {
        var code = (a.FuelType ?? "").Trim().ToLowerInvariant();
        if (code == "electricity" || code == "grid") code = "grid";
        var factor = await db.EmissionFactors
            .AsNoTracking()
            .Where(f => f.Code == code)
            .OrderByDescending(f => f.EffectiveFrom)
            .Select(f => f.Factor)
            .FirstOrDefaultAsync();
        var tons = (a.Quantity * factor) / 1000.0;
        if (double.IsNaN(tons) || double.IsInfinity(tons)) tons = 0;
        totalEmissions += tons;
    }
    double totalPathways = report.PathwaysEntries.Sum(p => Math.Max(0, p.EvCount) * 4.0
        + (Math.Max(0, p.ReMW) * 8760 * 0.35 * Math.Max(0, Math.Min(100, p.RePct)) / 100.0) * 0.75
        + Math.Max(0, p.McCH4) * 27.0
        + Math.Max(0, p.VamCH4) * 20.0);
    double totalOffsets = report.OffsetEntries.Sum(o => (Math.Max(0, o.AreaHectares) * Math.Max(0, o.TreesPerHectare) * 0.022) * Math.Max(1, o.Years));
    var net = totalEmissions - totalPathways - totalOffsets;
    return Results.Ok(new { reportId = report.Id, totalEmissions = Math.Round(totalEmissions,4), totalPathways = Math.Round(totalPathways,4), totalOffsets = Math.Round(totalOffsets,4), netEmissions = Math.Round(net,4) });
}).WithTags("Reports").WithOpenApi();

// Reports helper: ensure current report (today) exists
api.MapPost("/reports/current", async (EcoPathDbContext db, ReportCreateDto? dto) =>
{
    var today = DateTime.UtcNow.Date;
    var report = await db.Reports.OrderByDescending(r=>r.CreatedAt).FirstOrDefaultAsync(r => r.PeriodStart == today && r.PeriodEnd == today);
    if (report is null)
    {
        report = new Report { Title = dto?.Title ?? "Daily Report", PeriodStart = today, PeriodEnd = today, CreatedAt = DateTime.UtcNow };
        db.Reports.Add(report);
        await db.SaveChangesAsync();
    }
    return Results.Ok(new { report.Id });
}).WithTags("Reports").WithOpenApi();

// Bulk add calc entries to a report
api.MapPost("/reports/{id:int}/calc-entries/bulk", async (EcoPathDbContext db, int id, BulkCalcEntriesDto dto) =>
{
    var exists = await db.Reports.AnyAsync(r => r.Id == id);
    if (!exists) return Results.NotFound();
    if (dto?.Items == null || dto.Items.Count == 0) return Results.BadRequest("No entries provided");
    var list = dto.Items.Select(i => new CalcEntry { ReportId = id, Activity = i.Activity, FuelType = i.FuelType, Quantity = i.Quantity, Unit = i.Unit }).ToList();
    await db.CalcEntries.AddRangeAsync(list);
    await db.SaveChangesAsync();
    return Results.Ok(new { added = list.Count });
}).WithTags("Reports").WithOpenApi();

api.MapGet("/reports/{id:int}/summary", async (EcoPathDbContext db, int id) =>
{
    var report = await db.Reports
        .Include(r => r.CalcEntries)
        .Include(r => r.PathwaysEntries)
        .Include(r => r.OffsetEntries)
        .FirstOrDefaultAsync(r => r.Id == id);
    if (report is null) return Results.NotFound();

    // Emissions from CalcEntries
    double totalEmissions = 0;
    foreach (var a in report.CalcEntries)
    {
        var code = (a.FuelType ?? "").Trim().ToLowerInvariant();
        if (code == "electricity" || code == "grid") code = "grid";
        var factor = await db.EmissionFactors
            .AsNoTracking()
            .Where(f => f.Code == code)
            .OrderByDescending(f => f.EffectiveFrom)
            .Select(f => f.Factor)
            .FirstOrDefaultAsync();
        var tons = (a.Quantity * factor) / 1000.0;
        if (double.IsNaN(tons) || double.IsInfinity(tons)) tons = 0;
        totalEmissions += tons;
    }

    // Pathways totals
    double totalPathways = 0;
    foreach (var p in report.PathwaysEntries)
    {
        var evTons = Math.Max(0, p.EvCount) * 4.0;
        var mwhYear = Math.Max(0, p.ReMW) * 8760 * 0.35 * Math.Max(0, Math.Min(100, p.RePct)) / 100.0;
        var reTons = mwhYear * 0.75;
        var mcTons = Math.Max(0, p.McCH4) * 27.0;
        var vamTons = Math.Max(0, p.VamCH4) * 20.0;
        totalPathways += evTons + reTons + mcTons + vamTons;
    }

    // Offsets totals
    double totalOffsets = 0;
    foreach (var o in report.OffsetEntries)
    {
        const double TREE_CO2_TON_PER_YEAR = 0.022;
        var trees = Math.Max(0, o.AreaHectares) * Math.Max(0, o.TreesPerHectare);
        var annualTons = trees * TREE_CO2_TON_PER_YEAR;
        var totalOverYears = annualTons * Math.Max(1, o.Years);
        totalOffsets += totalOverYears;
    }

    var net = totalEmissions - totalPathways - totalOffsets;
    return Results.Ok(new
    {
        totalEmissions = Math.Round(totalEmissions, 4),
        totalPathways = Math.Round(totalPathways, 4),
        totalOffsets = Math.Round(totalOffsets, 4),
        netEmissions = Math.Round(net, 4)
    });
}).WithTags("Reports").WithOpenApi();

// Entries
api.MapPost("/reports/{id:int}/calc-entries", async (EcoPathDbContext db, int id, CalcEntryCreateDto dto) =>
{
    var exists = await db.Reports.AnyAsync(r => r.Id == id);
    if (!exists) return Results.NotFound();
    var e = new CalcEntry { ReportId = id, Activity = dto.Activity, FuelType = dto.FuelType, Quantity = dto.Quantity, Unit = dto.Unit };
    db.CalcEntries.Add(e);
    await db.SaveChangesAsync();
    return Results.Created($"/api/v1/reports/{id}/calc-entries/{e.Id}", e);
}).WithTags("Reports").WithOpenApi();

api.MapPost("/reports/{id:int}/pathways-entries", async (EcoPathDbContext db, int id, PathwaysEntryCreateDto dto) =>
{
    var exists = await db.Reports.AnyAsync(r => r.Id == id);
    if (!exists) return Results.NotFound();
    var e = new PathwaysEntry { ReportId = id, EvCount = dto.EvCount, ReMW = dto.ReMW, RePct = dto.RePct, McCH4 = dto.McCH4, VamCH4 = dto.VamCH4 };
    db.PathwaysEntries.Add(e);
    await db.SaveChangesAsync();
    return Results.Created($"/api/v1/reports/{id}/pathways-entries/{e.Id}", e);
}).WithTags("Reports").WithOpenApi();

api.MapPost("/reports/{id:int}/offset-entries", async (EcoPathDbContext db, int id, OffsetEntryCreateDto dto) =>
{
    var exists = await db.Reports.AnyAsync(r => r.Id == id);
    if (!exists) return Results.NotFound();
    var e = new OffsetEntry { ReportId = id, AreaHectares = dto.AreaHectares, TreesPerHectare = dto.TreesPerHectare, Years = dto.Years, MarketRate = dto.MarketRate };
    db.OffsetEntries.Add(e);
    await db.SaveChangesAsync();
    return Results.Created($"/api/v1/reports/{id}/offset-entries/{e.Id}", e);
}).WithTags("Reports").WithOpenApi();

app.Run();

// DTOs
public record CreateMineDto(string Name, MineType Type, MineDegree Degree, string? Location);
public record CalcRequest(List<ActivityInput> Activities);
public record ActivityInput(string Activity, string? FuelType, double Quantity, string Unit);
public record ActivityResult(string Activity, string FuelType, double Quantity, string Unit, double Tons);
public record PathwaysRequest(double EvCount, double ReMW, double RePct, double McCH4, double VamCH4);
public record FactorCreateUpdateDto(int Category, string Code, string Unit, double Factor, DateTime EffectiveFrom, DateTime? EffectiveTo);
public record OffsetRequest(double AreaHectares, double TreesPerHectare, double Years, double MarketRate);

// Report DTOs
public record ReportCreateDto(string Title, DateTime? PeriodStart, DateTime? PeriodEnd);
public record CalcEntryCreateDto(string Activity, string? FuelType, double Quantity, string Unit);
public record PathwaysEntryCreateDto(double EvCount, double ReMW, double RePct, double McCH4, double VamCH4);
public record OffsetEntryCreateDto(double AreaHectares, double TreesPerHectare, double Years, double MarketRate);
public record ProfileDto(string MineName, string MineId, string? Location, string? Area, string? Email, string? Phone);
public record BulkCalcEntriesDto(List<CalcEntryCreateDto> Items);

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

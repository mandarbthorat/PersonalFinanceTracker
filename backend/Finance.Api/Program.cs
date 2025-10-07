using Finance.Domain.Transactions;
using Finance.Domain.Categories;
using Finance.Domain.Budgets;
using Finance.Infrastructure;
using Finance.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;       // for [FromBody]
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;   // for IOptions<T>
using Microsoft.IdentityModel.Tokens;
using System.Text;




var builder = WebApplication.CreateBuilder(args);

// CORS
var allowedOrigins = builder.Configuration.GetValue<string>("AllowedOrigins")?.Split(',') ?? new[] { "http://localhost:5173" };

// EF Core
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// JWT
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
 .AddJwtBearer(o => {
     o.TokenValidationParameters = new()
     {
         ValidateIssuer = true,
         ValidateAudience = true,
         ValidateIssuerSigningKey = true,
         ValidIssuer = jwt.Issuer,
         ValidAudience = jwt.Audience,
         IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key))
     };
 });

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()); // requires explicit origins (no "*")
});
var app = builder.Build();

//app.UseSwagger(); app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseCors(c => c.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials());
app.UseAuthentication(); app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { ok = true }));

// --- Auth (demo-level) ---
app.MapPost("/api/auth/register", async (AppDbContext db, [FromBody] AuthRequest req) => {
    if (await db.Users.AnyAsync(u => u.Email == req.Email)) return Results.Conflict("Email already registered.");
    var hash = BCrypt.Net.BCrypt.HashPassword(req.Password);
    db.Users.Add(new Finance.Domain.Users.User { Email = req.Email, PasswordHash = hash });
    await db.SaveChangesAsync(); return Results.Ok();
});

app.MapPost("/api/auth/login", async (AppDbContext db, IOptions<JwtOptions> opts, [FromBody] AuthRequest req) => {
    var user = await db.Users.SingleOrDefaultAsync(u => u.Email == req.Email);
    if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash)) return Results.Unauthorized();

    var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opts.Value.Key));
    var token = handler.CreateJwtSecurityToken(
        issuer: opts.Value.Issuer, audience: opts.Value.Audience,
        subject: new System.Security.Claims.ClaimsIdentity(new[] {
            new System.Security.Claims.Claim("sub", user.Id.ToString()),
            new System.Security.Claims.Claim("email", user.Email)
        }),
        expires: DateTime.UtcNow.AddHours(12),
        signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
    );
    return Results.Ok(new { token = handler.WriteToken(token) });
});

// Sample: list monthly summary
app.MapGet("/api/transactions/summary/monthly", async (AppDbContext db, Guid userId, int year) => {
    var data = await db.Transactions
        .Where(t => t.UserId == userId && t.OccurredOn.Year == year)
        .GroupBy(t => t.OccurredOn.Month)
        .Select(g => new {
            Month = g.Key,
            Income = g.Where(x => x.Type == Finance.Domain.Transactions.TransactionType.Income).Sum(x => x.Amount),
            Expense = g.Where(x => x.Type == Finance.Domain.Transactions.TransactionType.Expense).Sum(x => x.Amount)
        }).OrderBy(x => x.Month).ToListAsync();
    return Results.Ok(data);
}).RequireAuthorization();
// Move all top-level statements above any namespace or type declarations.
// In your file, the only type declaration is the static method and record types at the end.
// Move the following code block to the very end of the file, after all top-level statements:
var api = app.MapGroup("/api").RequireAuthorization();
static bool TryParseTxType(string? v, out TransactionType t)
    => Enum.TryParse<TransactionType>(v, ignoreCase: true, out t);


// -------------------- Protected API group --------------------


//
// ========== CATEGORIES ==========
// GET /api/categories?userId=...&includeArchived=false
api.MapGet("/categories", async (AppDbContext db, Guid userId, bool? includeArchived) =>
{
    var q = db.Categories.AsNoTracking().Where(c => c.UserId == userId);
    if (!(includeArchived ?? false)) q = q.Where(c => !c.IsArchived);
    var items = await q.OrderBy(c => c.IsIncome ? 0 : 1).ThenBy(c => c.Name)
                       .Select(c => new { c.Id, c.Name, c.IsIncome, c.IsArchived })
                       .ToListAsync();
    return Results.Ok(items);
});

// POST /api/categories
api.MapPost("/categories", async (AppDbContext db, [FromBody] CreateCategoryRequest req) =>
{
    if (string.IsNullOrWhiteSpace(req.Name)) return Results.BadRequest("Name required.");

    var exists = await db.Categories.AnyAsync(c => c.UserId == req.UserId && c.Name == req.Name);
    if (exists) return Results.Conflict("Category already exists.");

    var cat = new Category { UserId = req.UserId, Name = req.Name.Trim(), IsIncome = req.IsIncome, IsArchived = false };
    db.Categories.Add(cat);
    await db.SaveChangesAsync();
    return Results.Created($"/api/categories/{cat.Id}", new { cat.Id });
});

// PUT /api/categories/{id}
api.MapPut("/categories/{id:guid}", async (AppDbContext db, Guid id, [FromBody] UpdateCategoryRequest req) =>
{
    var cat = await db.Categories.FindAsync(id);
    if (cat is null) return Results.NotFound();

    if (!string.IsNullOrWhiteSpace(req.Name)) cat.Name = req.Name.Trim();
    if (req.IsIncome.HasValue) cat.IsIncome = req.IsIncome.Value;
    if (req.IsArchived.HasValue) cat.IsArchived = req.IsArchived.Value;

    await db.SaveChangesAsync();
    return Results.NoContent();
});

// DELETE /api/categories/{id}  (soft-delete => archive)
api.MapDelete("/categories/{id:guid}", async (AppDbContext db, Guid id) =>
{
    var cat = await db.Categories.FindAsync(id);
    if (cat is null) return Results.NotFound();
    cat.IsArchived = true;
    await db.SaveChangesAsync();
    return Results.NoContent();
});

//
// ========== TRANSACTIONS ==========
//
// GET /api/transactions?userId=...&from=ISO&to=ISO&categoryId=...&type=Income|Expense&page=1&pageSize=20
api.MapGet("/transactions", async (HttpContext http, AppDbContext db,
    Guid userId, DateTime? from, DateTime? to, Guid? categoryId, string? type, int page = 1, int pageSize = 20) =>
{
    if (page <= 0 || pageSize <= 0) return Results.BadRequest("page and pageSize must be positive.");

    IQueryable<Finance.Domain.Transactions.Transaction> q = db.Transactions.AsNoTracking()
        .Where(t => t.UserId == userId);

    if (from.HasValue) q = q.Where(t => t.OccurredOn >= from.Value);
    if (to.HasValue) q = q.Where(t => t.OccurredOn < to.Value);
    if (categoryId.HasValue && categoryId.Value != Guid.Empty) q = q.Where(t => t.CategoryId == categoryId.Value);
    if (!string.IsNullOrWhiteSpace(type) && TryParseTxType(type, out var tt)) q = q.Where(t => t.Type == tt);

    var total = await q.CountAsync();
    var items = await q.OrderByDescending(t => t.OccurredOn)
        .Skip((page - 1) * pageSize).Take(pageSize)
        .Select(t => new { t.Id, t.UserId, t.CategoryId, t.Type, t.Amount, t.OccurredOn, t.Note })
        .ToListAsync();

    http.Response.Headers["X-Total-Count"] = total.ToString();
    return Results.Ok(items);
});

// GET /api/transactions/{id}?userId=...
api.MapGet("/transactions/{id:guid}", async (AppDbContext db, Guid id, Guid userId) =>
{
    var t = await db.Transactions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
    return t is null ? Results.NotFound() : Results.Ok(t);
});

// POST /api/transactions
api.MapPost("/transactions", async (AppDbContext db, [FromBody] CreateTransactionRequest req) =>
{
    if (!TryParseTxType(req.Type, out var tt))
        return Results.BadRequest("Type must be 'Income' or 'Expense'.");

    var cat = await db.Categories.FirstOrDefaultAsync(c => c.Id == req.CategoryId && c.UserId == req.UserId);
    if (cat is null) return Results.BadRequest("Invalid category.");
    if (cat.IsIncome != (tt == TransactionType.Income))
        return Results.BadRequest("Category type mismatch for the transaction.");
    var occurredUtc =
        req.OccurredOn.Kind switch
        {
            DateTimeKind.Utc => req.OccurredOn,
            DateTimeKind.Local => req.OccurredOn.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(req.OccurredOn, DateTimeKind.Utc), // treat as midnight UTC of that date
            _ => DateTime.SpecifyKind(req.OccurredOn, DateTimeKind.Utc)
        };
    var tx = new Finance.Domain.Transactions.Transaction
    {
        UserId = req.UserId,
        CategoryId = req.CategoryId,
        Type = tt,
        Amount = req.Amount,
        OccurredOn = occurredUtc,
        Note = req.Note
    };
    db.Transactions.Add(tx);
    await db.SaveChangesAsync();
    return Results.Created($"/api/transactions/{tx.Id}", new { tx.Id });
});

// PUT /api/transactions/{id}
api.MapPut("/transactions/{id:guid}", async (AppDbContext db, Guid id, [FromBody] UpdateTransactionRequest req) =>
{
    var tx = await db.Transactions.FindAsync(id);
    if (tx is null) return Results.NotFound();

    if (req.Type is not null)
    {
        if (!TryParseTxType(req.Type, out var tt)) return Results.BadRequest("Type must be 'Income' or 'Expense'.");
        tx.Type = tt;
    }
    if (req.CategoryId.HasValue && req.CategoryId.Value != Guid.Empty)
    {
        var cat = await db.Categories.FindAsync(req.CategoryId.Value);
        if (cat is null) return Results.BadRequest("Invalid category.");
        if (req.Type is not null)
        {
            // when both changed, we already set tx.Type above
            if (cat.IsIncome != (tx.Type == TransactionType.Income))
                return Results.BadRequest("Category type mismatch for the transaction.");
        }
        else
        {
            // ensure current tx type matches new category
            if (cat.IsIncome != (tx.Type == TransactionType.Income))
                return Results.BadRequest("Category type mismatch for the transaction.");
        }
        tx.CategoryId = req.CategoryId.Value;
    }
    if (req.Amount.HasValue) tx.Amount = req.Amount.Value;
    if (req.OccurredOn.HasValue) tx.OccurredOn = req.OccurredOn.Value;
    if (req.Note is not null) tx.Note = req.Note;

    await db.SaveChangesAsync();
    return Results.NoContent();
});

// DELETE /api/transactions/{id}
api.MapDelete("/transactions/{id:guid}", async (AppDbContext db, Guid id) =>
{
    var tx = await db.Transactions.FindAsync(id);
    if (tx is null) return Results.NotFound();
    db.Transactions.Remove(tx);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

//
// ========== BUDGETS ==========
//
// GET /api/budgets/current?userId=...&year=2025&month=10
api.MapGet("/budgets/current", async (AppDbContext db, Guid userId, int? year, int? month) =>
{
    var now = DateTime.UtcNow;
    int y = year ?? now.Year;
    int m = month ?? now.Month;
    var start = new DateTime(y, m, 1);
    var end = start.AddMonths(1);

    var budgets = await db.Budgets
        .Where(b => b.UserId == userId && b.Year == y && b.Month == m)
        .Select(b => new { b.Id, b.CategoryId, b.Amount, b.Month, b.Year })
        .ToListAsync();

    var startUtc = new DateTime(y, m, 1, 0, 0, 0, DateTimeKind.Utc);
    var endUtc = startUtc.AddMonths(1);

    var spentByCat = await db.Transactions
        .Where(t => t.UserId == userId
                 && t.Type == TransactionType.Expense
                 && t.OccurredOn >= startUtc && t.OccurredOn < endUtc)
        .GroupBy(t => t.CategoryId)
        .Select(g => new { CategoryId = g.Key, Spent = g.Sum(x => x.Amount) })
        .ToListAsync();

    var cats = await db.Categories.AsNoTracking()
                 .Where(c => c.UserId == userId)
                 .Select(c => new { c.Id, c.Name })
                 .ToListAsync();

    var rows = (from b in budgets
                join c in cats on b.CategoryId equals c.Id into catj
                from c in catj.DefaultIfEmpty()
                join s in spentByCat on b.CategoryId equals s.CategoryId into spj
                from s in spj.DefaultIfEmpty()
                select new
                {
                    id = b.Id,
                    categoryId = b.CategoryId,
                    categoryName = c?.Name ?? "(deleted)",
                    amount = b.Amount,
                    spent = s?.Spent ?? 0m,
                    month = b.Month,
                    year = b.Year
                }).OrderBy(x => x.categoryName).ToList();

    return Results.Ok(rows);
});

// POST /api/budgets  (upsert for (userId, categoryId, year, month))
api.MapPost("/budgets", async (AppDbContext db, [FromBody] UpsertBudgetRequest req) =>
{
    if (req.Month < 1 || req.Month > 12) return Results.BadRequest("Month must be 1..12.");
    if (req.Amount < 0) return Results.BadRequest("Amount must be >= 0.");

    var existing = await db.Budgets
        .FirstOrDefaultAsync(b => b.UserId == req.UserId && b.CategoryId == req.CategoryId && b.Year == req.Year && b.Month == req.Month);

    if (existing is null)
    {
        var b = new Budget { UserId = req.UserId, CategoryId = req.CategoryId, Year = req.Year, Month = req.Month, Amount = req.Amount };
        db.Budgets.Add(b);
        await db.SaveChangesAsync();
        return Results.Created($"/api/budgets/{b.Id}", new { b.Id });
    }
    else
    {
        existing.Amount = req.Amount;
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
});

// DELETE /api/budgets/{id}
api.MapDelete("/budgets/{id:guid}", async (AppDbContext db, Guid id) =>
{
    var b = await db.Budgets.FindAsync(id);
    if (b is null) return Results.NotFound();
    db.Budgets.Remove(b);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// (Optional) GET /api/budgets/summary/yearly?userId=...&year=2025
api.MapGet("/budgets/summary/yearly", async (AppDbContext db, Guid userId, int year) =>
{
    var budgets = await db.Budgets
        .Where(b => b.UserId == userId && b.Year == year)
        .GroupBy(b => b.Month)
        .Select(g => new { Month = g.Key, Budget = g.Sum(x => x.Amount) })
        .ToListAsync();

    var tx = await db.Transactions
        .Where(t => t.UserId == userId && t.Type == TransactionType.Expense && t.OccurredOn.Year == year)
        .GroupBy(t => t.OccurredOn.Month)
        .Select(g => new { Month = g.Key, Spent = g.Sum(x => x.Amount) })
        .ToListAsync();

    var months = Enumerable.Range(1, 12);
    var result = months.Select(m => new {
        Month = m,
        Budget = budgets.SingleOrDefault(x => x.Month == m)?.Budget ?? 0m,
        Spent = tx.SingleOrDefault(x => x.Month == m)?.Spent ?? 0m
    }).ToList();

    return Results.Ok(result);
});
app.Run();


record AuthRequest(string Email, string Password);
// -------------------- DTOs --------------------
record CreateCategoryRequest(Guid UserId, string Name, bool IsIncome);
record UpdateCategoryRequest(string Name, bool? IsIncome, bool? IsArchived);

record CreateTransactionRequest(Guid UserId, Guid CategoryId, string Type, decimal Amount, DateTime OccurredOn, string? Note);
record UpdateTransactionRequest(Guid? CategoryId, string? Type, decimal? Amount, DateTime? OccurredOn, string? Note);

record UpsertBudgetRequest(Guid UserId, Guid CategoryId, decimal Amount, int Month, int Year);


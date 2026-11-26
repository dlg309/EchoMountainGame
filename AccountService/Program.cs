using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 8192);
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "local", _ =>
            new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.RejectionStatusCode = 429;
});
builder.WebHost.UseUrls(builder.Configuration["ECHO_ACCOUNT_URL"] ?? "https://localhost:7186");
var app = builder.Build();
var db = new AccountDatabase(builder.Configuration["ECHO_ACCOUNT_DB"] ?? Path.Combine(AppContext.BaseDirectory, "accounts.db"));
db.Initialize();
app.Use(async (context, next) =>
{
    context.Response.Headers.CacheControl = "no-store";
    await next();
});
app.UseRateLimiter();

app.MapPost("/v1/register", (Credentials input) =>
{
    var error = AccountRules.CredentialsError(input);
    if (error != null) return Results.BadRequest(new { error });
    var user = db.Register(input.Email.Trim().ToLowerInvariant(), input.Password, input.DisplayName.Trim());
    return user == null ? Results.Conflict(new { error = "Account already exists." }) : Results.Ok(user);
});
app.MapPost("/v1/login", (Login input) =>
{
    if (string.IsNullOrWhiteSpace(input.Email) || string.IsNullOrEmpty(input.Password)) return Results.Unauthorized();
    var session = db.Login(input.Email.Trim().ToLowerInvariant(), input.Password);
    return session == null ? Results.Unauthorized() : Results.Ok(session);
});
app.MapPost("/v1/logout", (HttpRequest request) =>
{
    var token = Token(request);
    if (token != null) db.Logout(token);
    return Results.NoContent();
});
app.MapGet("/v1/me", (HttpRequest request) =>
{
    var user = Owner(request);
    return user == null ? Results.Unauthorized() : Results.Ok(user);
});
app.MapPut("/v1/me", (HttpRequest request, ProfileUpdate input) =>
{
    var user = Owner(request);
    if (user == null) return Results.Unauthorized();
    var error = AccountRules.DisplayNameError(input.DisplayName);
    if (error != null) return Results.BadRequest(new { error });
    return Results.Ok(db.UpdateName(user.Id, input.DisplayName.Trim()));
});
app.MapPost("/v1/runs", (HttpRequest request, RunInput input) =>
{
    var user = Owner(request);
    if (user == null) return Results.Unauthorized();
    var error = AccountRules.RunError(input);
    if (error != null) return Results.BadRequest(new { error });
    return Results.Ok(db.SaveRun(user.Id, input));
});
app.MapGet("/v1/runs", (HttpRequest request) =>
{
    var user = Owner(request);
    return user == null ? Results.Unauthorized() : Results.Ok(db.Runs(user.Id));
});
app.MapGet("/v1/bests", (HttpRequest request) =>
{
    var user = Owner(request);
    return user == null ? Results.Unauthorized() : Results.Ok(db.Bests(user.Id));
});
app.MapGet("/v1/runs/{id:long}", (HttpRequest request, long id) =>
{
    var user = Owner(request);
    if (user == null) return Results.Unauthorized();
    var run = db.Run(user.Id, id);
    return run == null ? Results.NotFound() : Results.Ok(run);
});
app.Run();

string? Token(HttpRequest request)
{
    var header = request.Headers.Authorization.ToString();
    return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? header[7..] : null;
}
Account? Owner(HttpRequest request) => Token(request) is { } token ? db.AccountFor(token) : null;

public sealed record Credentials(string Email, string Password, string DisplayName);
public sealed record Login(string Email, string Password);
public sealed record ProfileUpdate(string DisplayName);
public sealed record Account(long Id, string Email, string DisplayName);
public sealed record Session(string Token, Account Account);
public sealed record RunInput(string AttemptId, string Route, bool Practice, double Seconds, double DistanceMeters,
    double TopSpeedKmh, int Score, string Tricks, string CompletedUtc, string Course, int CourseVersion, int RulesVersion);
public sealed record SavedRun(long Id, string AttemptId, string Route, bool Practice, double Seconds, double DistanceMeters,
    double TopSpeedKmh, int Score, string Tricks, string CompletedUtc, string Course, int CourseVersion, int RulesVersion);
public sealed record RunBests(string Route, bool Practice, SavedRun? Fastest, SavedRun? HighestScore);

public static class AccountRules
{
    public static string? DisplayNameError(string? name) => name == null || name.Trim().Length is < 2 or > 40 ||
        name.Any(char.IsControl) ? "Display name must have 2–40 printable characters." : null;
    public static string? CredentialsError(Credentials input) =>
        string.IsNullOrWhiteSpace(input.Email) || input.Email.Length > 254 || !input.Email.Contains('@') ? "Enter a valid email." :
        string.IsNullOrEmpty(input.Password) || input.Password.Length < 12 || input.Password.Length > 128 ? "Password must have 12–128 characters." :
        DisplayNameError(input.DisplayName);
    public static string? RunError(RunInput run) =>
        !Guid.TryParse(run.AttemptId, out _) || run.AttemptId.Length > 36 ? "Invalid attempt ID." :
        run.Route is not ("Pine" or "Summit") ? "Invalid route." :
        run.Practice && run.Route != "Summit" ? "Invalid practice route." :
        !double.IsFinite(run.Seconds) || run.Seconds <= 0 || run.Seconds > 3600 ||
        !double.IsFinite(run.DistanceMeters) || run.DistanceMeters < 0 || run.DistanceMeters > 100000 ||
        !double.IsFinite(run.TopSpeedKmh) || run.TopSpeedKmh < 0 || run.TopSpeedKmh > 300 ||
        run.Score < 0 || run.Score > 10000000 ? "Invalid run measurements." :
        run.Tricks == null || run.Tricks.Length > 2048 || run.Tricks.Any(char.IsControl) ? "Invalid trick summary." :
        !DateTimeOffset.TryParse(run.CompletedUtc, out _) ? "Invalid completion time." :
        run.Course != "glacier-pass" || run.CourseVersion != 2 || run.RulesVersion != 5
            ? "Incompatible course or rules version." : null;
}

public sealed class AccountDatabase
{
    private readonly string connectionString;
    private readonly PasswordHasher<string> hasher = new();
    public AccountDatabase(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        connectionString = new SqliteConnectionStringBuilder { DataSource = path, Mode = SqliteOpenMode.ReadWriteCreate, Pooling = false }.ToString();
    }
    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var pragmas = connection.CreateCommand();
        pragmas.CommandText = "PRAGMA foreign_keys=ON; PRAGMA busy_timeout=5000;";
        pragmas.ExecuteNonQuery();
        return connection;
    }
    public void Initialize()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS accounts (id INTEGER PRIMARY KEY, email TEXT NOT NULL UNIQUE, password_hash TEXT NOT NULL, display_name TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS sessions (token_hash TEXT PRIMARY KEY, account_id INTEGER NOT NULL REFERENCES accounts(id), expires_utc TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS runs (id INTEGER PRIMARY KEY, account_id INTEGER NOT NULL REFERENCES accounts(id), attempt_id TEXT NOT NULL,
              route TEXT NOT NULL, practice INTEGER NOT NULL, seconds REAL NOT NULL, distance_meters REAL NOT NULL, top_speed_kmh REAL NOT NULL,
              score INTEGER NOT NULL, tricks TEXT NOT NULL, completed_utc TEXT NOT NULL, course TEXT NOT NULL, course_version INTEGER NOT NULL,
              rules_version INTEGER NOT NULL, UNIQUE(account_id, attempt_id));
            CREATE INDEX IF NOT EXISTS runs_owner ON runs(account_id, completed_utc DESC);
            """;
        command.ExecuteNonQuery();
    }
    public Account? Register(string email, string password, string displayName)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO accounts(email,password_hash,display_name) VALUES($email,$hash,$name) RETURNING id";
        command.Parameters.AddWithValue("$email", email);
        command.Parameters.AddWithValue("$hash", hasher.HashPassword(email, password));
        command.Parameters.AddWithValue("$name", displayName);
        try { return new Account((long)command.ExecuteScalar()!, email, displayName); }
        catch (SqliteException error) when (error.SqliteErrorCode == 19) { return null; }
    }
    public Session? Login(string email, string password)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id,password_hash,display_name FROM accounts WHERE email=$email";
        command.Parameters.AddWithValue("$email", email);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) return null;
        var id = reader.GetInt64(0);
        var hash = reader.GetString(1);
        var name = reader.GetString(2);
        if (hasher.VerifyHashedPassword(email, hash, password) == PasswordVerificationResult.Failed) return null;
        reader.Close();
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        using var insert = connection.CreateCommand();
        insert.CommandText = "INSERT INTO sessions VALUES($hash,$id,$expires)";
        insert.Parameters.AddWithValue("$hash", Hash(token));
        insert.Parameters.AddWithValue("$id", id);
        insert.Parameters.AddWithValue("$expires", DateTimeOffset.UtcNow.AddHours(12).ToString("O"));
        insert.ExecuteNonQuery();
        return new Session(token, new Account(id, email, name));
    }
    public Account? AccountFor(string token)
    {
        if (token.Length != 64 || !token.All(Uri.IsHexDigit)) return null;
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT a.id,a.email,a.display_name FROM sessions s JOIN accounts a ON a.id=s.account_id WHERE s.token_hash=$hash AND s.expires_utc>$now";
        command.Parameters.AddWithValue("$hash", Hash(token));
        command.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToString("O"));
        using var reader = command.ExecuteReader();
        return reader.Read() ? new Account(reader.GetInt64(0), reader.GetString(1), reader.GetString(2)) : null;
    }
    public void Logout(string token)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM sessions WHERE token_hash=$hash";
        command.Parameters.AddWithValue("$hash", Hash(token));
        command.ExecuteNonQuery();
    }
    public Account UpdateName(long owner, string name)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE accounts SET display_name=$name WHERE id=$owner; SELECT email FROM accounts WHERE id=$owner";
        command.Parameters.AddWithValue("$name", name);
        command.Parameters.AddWithValue("$owner", owner);
        return new Account(owner, (string)command.ExecuteScalar()!, name);
    }
    public SavedRun SaveRun(long owner, RunInput input)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO runs(account_id,attempt_id,route,practice,seconds,distance_meters,top_speed_kmh,score,tricks,completed_utc,course,course_version,rules_version)
            VALUES($owner,$attempt,$route,$practice,$seconds,$distance,$speed,$score,$tricks,$utc,$course,$courseVersion,$rulesVersion)
            ON CONFLICT(account_id,attempt_id) DO NOTHING;
            SELECT id FROM runs WHERE account_id=$owner AND attempt_id=$attempt;
            """;
        command.Parameters.AddWithValue("$owner", owner);
        command.Parameters.AddWithValue("$attempt", input.AttemptId);
        command.Parameters.AddWithValue("$route", input.Route);
        command.Parameters.AddWithValue("$practice", input.Practice ? 1 : 0);
        command.Parameters.AddWithValue("$seconds", input.Seconds);
        command.Parameters.AddWithValue("$distance", input.DistanceMeters);
        command.Parameters.AddWithValue("$speed", input.TopSpeedKmh);
        command.Parameters.AddWithValue("$score", input.Score);
        command.Parameters.AddWithValue("$tricks", input.Tricks);
        command.Parameters.AddWithValue("$utc", input.CompletedUtc);
        command.Parameters.AddWithValue("$course", input.Course);
        command.Parameters.AddWithValue("$courseVersion", input.CourseVersion);
        command.Parameters.AddWithValue("$rulesVersion", input.RulesVersion);
        var id = (long)command.ExecuteScalar()!;
        return Run(owner, id)!;
    }
    public SavedRun? Run(long owner, long id)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id,attempt_id,route,practice,seconds,distance_meters,top_speed_kmh,score,tricks,completed_utc,course,course_version,rules_version FROM runs WHERE account_id=$owner AND id=$id";
        command.Parameters.AddWithValue("$owner", owner);
        command.Parameters.AddWithValue("$id", id);
        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadRun(reader) : null;
    }
    public IReadOnlyList<SavedRun> Runs(long owner)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id,attempt_id,route,practice,seconds,distance_meters,top_speed_kmh,score,tricks,completed_utc,course,course_version,rules_version FROM runs WHERE account_id=$owner ORDER BY completed_utc DESC LIMIT 100";
        command.Parameters.AddWithValue("$owner", owner);
        using var reader = command.ExecuteReader();
        var result = new List<SavedRun>();
        while (reader.Read()) result.Add(ReadRun(reader));
        return result;
    }
    public IReadOnlyList<RunBests> Bests(long owner)
    {
        var result = new List<RunBests>();
        foreach (var (route, practice) in new[] { ("Pine", false), ("Summit", false), ("Summit", true) })
            result.Add(new RunBests(route, practice, Best(owner, route, practice, "seconds ASC"),
                Best(owner, route, practice, "score DESC")));
        return result;
    }
    private SavedRun? Best(long owner, string route, bool practice, string order)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id,attempt_id,route,practice,seconds,distance_meters,top_speed_kmh,score,tricks,completed_utc,course,course_version,rules_version FROM runs WHERE account_id=$owner AND route=$route AND practice=$practice ORDER BY " + order + " LIMIT 1";
        command.Parameters.AddWithValue("$owner", owner);
        command.Parameters.AddWithValue("$route", route);
        command.Parameters.AddWithValue("$practice", practice ? 1 : 0);
        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadRun(reader) : null;
    }
    private static SavedRun ReadRun(SqliteDataReader reader) => new(reader.GetInt64(0),reader.GetString(1),reader.GetString(2),reader.GetInt64(3) != 0,
        reader.GetDouble(4),reader.GetDouble(5),reader.GetDouble(6),reader.GetInt32(7),reader.GetString(8),reader.GetString(9),reader.GetString(10),reader.GetInt32(11),reader.GetInt32(12));
    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(token)));
}
public partial class Program { }

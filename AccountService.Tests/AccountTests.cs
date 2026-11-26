using Xunit;

public sealed class AccountTests
{
    [Fact]
    public void RulesRejectBadNamesAndImpossibleScores()
    {
        Assert.NotNull(AccountRules.CredentialsError(new Credentials("a@example.com", "short", "Player")));
        Assert.NotNull(AccountRules.DisplayNameError("\n"));
        Assert.NotNull(AccountRules.RunError(Run() with { Score = -1 }));
        Assert.NotNull(AccountRules.RunError(Run() with { Seconds = double.NaN }));
        Assert.Null(AccountRules.RunError(Run()));
    }

    [Fact]
    public void DatabaseKeepsRunsPrivateAndIdempotent()
    {
        var path = Path.Combine(Path.GetTempPath(), "echo-account-test-" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            var db = new AccountDatabase(path);
            db.Initialize();
            var alice = db.Register("alice@example.com", "a-long-password-123", "Alice")!;
            var bob = db.Register("bob@example.com", "a-long-password-123", "Bob")!;
            Assert.Null(db.Register("alice@example.com", "a-long-password-123", "Alice"));
            Assert.Null(db.Login(alice.Email, "wrong-password"));
            var session = db.Login(alice.Email, "a-long-password-123")!;
            Assert.Equal(alice.Id, db.AccountFor(session.Token)!.Id);
            var input = Run();
            var saved = db.SaveRun(alice.Id, input);
            Assert.Equal(saved.Id, db.SaveRun(alice.Id, input with { Score = 999 }).Id);
            Assert.Equal(120, db.Run(alice.Id, saved.Id)!.Score);
            Assert.Single(db.Runs(alice.Id));
            Assert.Empty(db.Runs(bob.Id));
            Assert.Null(db.Run(bob.Id, saved.Id));
            var reopened = new AccountDatabase(path);
            reopened.Initialize();
            Assert.Equal(saved.Id, reopened.Run(alice.Id, saved.Id)!.Id);
            Assert.Equal(alice.Id, reopened.Login(alice.Email, "a-long-password-123")!.Account.Id);
            Assert.Equal("New Alice", db.UpdateName(alice.Id, "New Alice").DisplayName);
            db.Logout(session.Token);
            Assert.Null(db.AccountFor(session.Token));
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    private static RunInput Run() => new(Guid.NewGuid().ToString(), "Pine", false, 75, 850, 60, 120,
        "180 Indy +120", DateTimeOffset.UtcNow.ToString("O"), "glacier-pass", 2, 5);
}

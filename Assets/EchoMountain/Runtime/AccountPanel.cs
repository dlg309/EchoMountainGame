using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace EchoMountain.Riding
{
    [Serializable] public sealed class AccountProfile { public long id; public string email; public string displayName; }
    [Serializable] public sealed class AccountSession { public string token; public AccountProfile account; }
    [Serializable] public sealed class AccountRun
    {
        public long id;
        public string attemptId, route, tricks, completedUtc, course;
        public bool practice;
        public float seconds, distanceMeters, topSpeedKmh;
        public int score, courseVersion, rulesVersion;
    }
    [Serializable] internal sealed class RunList { public AccountRun[] items; }
    [Serializable] public sealed class AccountBest { public string route; public bool practice; public AccountRun fastest, highestScore; }
    [Serializable] internal sealed class BestList { public AccountBest[] items; }
    [Serializable] internal sealed class AccountError { public string error; }
    [Serializable] internal sealed class LoginRequest { public string email, password; }
    [Serializable] internal sealed class RegisterRequest { public string email, password, displayName; }
    [Serializable] internal sealed class ProfileRequest { public string displayName; }

    public interface IAccountRunStore
    {
        IEnumerator Save(AccountRun run, Action<bool, string> completed);
        IEnumerator History(Action<AccountRun[], AccountBest[], string> completed);
    }

    // Owns only HTTPS calls and an in-memory bearer token. No credential or token file.
    public sealed class AccountServiceClient : IAccountRunStore
    {
        private readonly string baseUrl;
        private string token;
        public AccountProfile Profile { get; private set; }
        public bool SignedIn => !string.IsNullOrEmpty(token);

        public AccountServiceClient(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                throw new ArgumentException("Account service must use HTTPS.");
            baseUrl = url.TrimEnd('/');
        }
        private UnityWebRequest Request(string method, string path, object body = null)
        {
            var request = new UnityWebRequest(baseUrl + path, method);
            request.downloadHandler = new DownloadHandlerBuffer();
            if (body != null)
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(body)));
                request.SetRequestHeader("Content-Type", "application/json");
            }
            if (SignedIn) request.SetRequestHeader("Authorization", "Bearer " + token);
            request.timeout = 10;
            return request;
        }
        private static string Error(UnityWebRequest request)
        {
            if (request.responseCode == 401) return "Sign in again to access private records.";
            if (request.result == UnityWebRequest.Result.ConnectionError) return "Account service unavailable. Local play still works; retry later.";
            try
            {
                string message = JsonUtility.FromJson<AccountError>(request.downloadHandler.text)?.error;
                if (!string.IsNullOrEmpty(message)) return message;
            }
            catch (ArgumentException) { }
            return "Account request failed (" + request.responseCode + ").";
        }
        public IEnumerator Register(string email, string password, string name, Action<string> done)
        {
            using var request = Request("POST", "/v1/register", new RegisterRequest { email = email, password = password, displayName = name });
            yield return request.SendWebRequest();
            done(request.result == UnityWebRequest.Result.Success ? "Account created. Sign in to continue." : Error(request));
        }
        public IEnumerator Login(string email, string password, Action<string> done)
        {
            using var request = Request("POST", "/v1/login", new LoginRequest { email = email, password = password });
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                var session = JsonUtility.FromJson<AccountSession>(request.downloadHandler.text);
                token = session.token;
                Profile = session.account;
                done("Signed in as " + Profile.displayName + ".");
            }
            else done(Error(request));
        }
        public IEnumerator Logout(Action done)
        {
            if (SignedIn)
            {
                using var request = Request("POST", "/v1/logout");
                yield return request.SendWebRequest();
            }
            token = null;
            Profile = null;
            done();
        }
        public IEnumerator UpdateName(string name, Action<string> done)
        {
            using var request = Request("PUT", "/v1/me", new ProfileRequest { displayName = name });
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                Profile = JsonUtility.FromJson<AccountProfile>(request.downloadHandler.text);
                done("Display name saved.");
            }
            else done(Error(request));
        }
        public IEnumerator Save(AccountRun run, Action<bool, string> completed)
        {
            using var request = Request("POST", "/v1/runs", run);
            yield return request.SendWebRequest();
            completed(request.result == UnityWebRequest.Result.Success,
                request.result == UnityWebRequest.Result.Success ? "Saved to private history." : Error(request));
        }
        public IEnumerator History(Action<AccountRun[], AccountBest[], string> completed)
        {
            using var request = Request("GET", "/v1/runs");
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                // JsonUtility cannot deserialize a top-level array.
                var list = JsonUtility.FromJson<RunList>("{\"items\":" + request.downloadHandler.text + "}");
                using var bestRequest = Request("GET", "/v1/bests");
                yield return bestRequest.SendWebRequest();
                if (bestRequest.result != UnityWebRequest.Result.Success)
                { completed(list.items, null, Error(bestRequest)); yield break; }
                var bests = JsonUtility.FromJson<BestList>("{\"items\":" + bestRequest.downloadHandler.text + "}");
                completed(list.items ?? Array.Empty<AccountRun>(), bests.items ?? Array.Empty<AccountBest>(), "");
            }
            else completed(null, null, Error(request));
        }
    }

    // Separate account presentation from the ride HUD and gameplay state.
    public sealed class AccountPanel : MonoBehaviour
    {
        private MountainRun run;
        private AccountServiceClient client;
        private string email = "", password = "", displayName = "", message = "";
        private AccountRun[] history = Array.Empty<AccountRun>();
        private AccountBest[] bests = Array.Empty<AccountBest>();
        private bool busy, savedAttempt;
        private string savedId;
        private int page;
        private Vector2 historyScroll;
        public bool IsOpen { get; private set; }

        private void Awake()
        {
            run = GetComponent<MountainRun>();
            client = new AccountServiceClient(Environment.GetEnvironmentVariable("ECHO_ACCOUNT_URL") ?? "https://localhost:7186");
        }
        public void Open() { IsOpen = true; page = client.SignedIn ? 1 : 0; message = ""; }
        public void Close() { IsOpen = false; password = ""; }
        private void OnGUI()
        {
            if (!IsOpen) return;
            if (Event.current.type == EventType.KeyDown)
            {
                if (!client.SignedIn && Event.current.keyCode == KeyCode.Tab)
                {
                    string focus = GUI.GetNameOfFocusedControl();
                    GUI.FocusControl(focus == "accountEmail" ? "accountPassword" :
                        focus == "accountPassword" ? "accountName" : "accountEmail");
                    Event.current.Use();
                }
                else if (!busy && !client.SignedIn && Event.current.keyCode == KeyCode.Return)
                { Begin(client.Login(email, password, Done)); Event.current.Use(); }
                else if (!busy && !client.SignedIn && Event.current.control && Event.current.keyCode == KeyCode.N)
                { Begin(client.Register(email, password, displayName, Done)); Event.current.Use(); }
                else if (!busy && client.SignedIn && Event.current.keyCode == KeyCode.H)
                { LoadHistory(); Event.current.Use(); }
                else if (!busy && client.SignedIn && Event.current.keyCode == KeyCode.S)
                { page = 2; Event.current.Use(); }
                else if (!busy && client.SignedIn && page == 2 && Event.current.control && Event.current.keyCode == KeyCode.Return)
                { Begin(client.UpdateName(displayName, Done)); Event.current.Use(); }
                else if (!busy && client.SignedIn && page == 2 && Event.current.keyCode == KeyCode.L)
                { Begin(client.Logout(() => { history = Array.Empty<AccountRun>(); savedAttempt = false; Done("Signed out."); })); Event.current.Use(); }
            }
            float scale = Mathf.Min(Screen.width / 900f, Screen.height / 650f);
            Matrix4x4 old = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float x = (Screen.width / scale - 620f) / 2f;
            float y = (Screen.height / scale - 560f) / 2f;
            GUI.Box(new Rect(x, y, 620f, 560f), "PRIVATE RUNS / ACCOUNT");
            if (GUI.Button(new Rect(x + 525f, y + 30f, 70f, 32f), "Close") || Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            { Close(); GUI.matrix = old; return; }
            if (!client.SignedIn)
            {
                GUI.Label(new Rect(x + 30f, y + 70f, 540f, 30f), "Local play works without an account. Enter: sign in / Ctrl+N: create.");
                GUI.Label(new Rect(x + 30f, y + 122f, 120f, 28f), "Email");
                GUI.SetNextControlName("accountEmail");
                email = GUI.TextField(new Rect(x + 160f, y + 120f, 390f, 33f), email, 254);
                GUI.Label(new Rect(x + 30f, y + 170f, 120f, 28f), "Password");
                GUI.SetNextControlName("accountPassword");
                password = GUI.PasswordField(new Rect(x + 160f, y + 168f, 390f, 33f), password, '*', 128);
                GUI.Label(new Rect(x + 30f, y + 218f, 120f, 28f), "Display name");
                GUI.SetNextControlName("accountName");
                displayName = GUI.TextField(new Rect(x + 160f, y + 216f, 390f, 33f), displayName, 40);
                if (GUI.Button(new Rect(x + 30f, y + 290f, 240f, 44f), "Sign in") && !busy)
                    Begin(client.Login(email, password, Done));
                if (GUI.Button(new Rect(x + 310f, y + 290f, 240f, 44f), "Create account") && !busy)
                    Begin(client.Register(email, password, displayName, Done));
            }
            else
            {
                GUI.Label(new Rect(x + 30f, y + 70f, 540f, 30f), client.Profile.displayName + "  /  " + client.Profile.email);
                if ((GUI.Button(new Rect(x + 30f, y + 115f, 160f, 36f), "Save result") ||
                    Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return && page != 2) && !busy) SaveResult();
                if (GUI.Button(new Rect(x + 220f, y + 115f, 160f, 36f), "History [H]") && !busy)
                    LoadHistory();
                if (GUI.Button(new Rect(x + 410f, y + 115f, 160f, 36f), "Settings [S]")) page = 2;
                if (page == 1)
                {
                    for (int i = 0; i < Mathf.Min(bests.Length, 3); i++)
                    {
                        var best = bests[i];
                        if (best.fastest != null)
                            GUI.Label(new Rect(x + 30f, y + 165f + i * 29f, 550f, 27f),
                                $"{best.route}{(best.practice ? " practice" : "")}: best {best.fastest.seconds:F1}s / {best.highestScore.score} pts");
                    }
                    if (history.Length == 0) GUI.Label(new Rect(x + 30f, y + 270f, 520f, 30f), "No saved runs yet.");
                    historyScroll = GUI.BeginScrollView(new Rect(x + 30f, y + 270f, 550f, 226f),
                        historyScroll, new Rect(0f, 0f, 520f, Mathf.Max(226f, history.Length * 53f)));
                    for (int i = 0; i < history.Length; i++)
                    {
                        var item = history[i];
                        GUI.Label(new Rect(0f, i * 53f, 520f, 24f),
                            $"{item.route}  {item.seconds:F1}s  {item.score} pts  {item.distanceMeters:F0}m  {item.topSpeedKmh:F0}km/h");
                        GUI.Label(new Rect(0f, i * 53f + 23f, 520f, 25f),
                            $"{item.completedUtc}  /  {(string.IsNullOrEmpty(item.tricks) ? "No tricks" : item.tricks)}");
                    }
                    GUI.EndScrollView();
                }
                else if (page == 2)
                {
                    GUI.Label(new Rect(x + 30f, y + 190f, 500f, 28f), "Display name (2–40 characters)");
                    displayName = GUI.TextField(new Rect(x + 30f, y + 225f, 350f, 36f), displayName, 40);
                    if (GUI.Button(new Rect(x + 395f, y + 225f, 175f, 36f), "Save name") && !busy)
                        Begin(client.UpdateName(displayName, Done));
                    GUI.Label(new Rect(x + 30f, y + 270f, 530f, 25f), "Ctrl+Enter: save name   /   L: sign out");
                    if (GUI.Button(new Rect(x + 30f, y + 305f, 160f, 38f), "Sign out") && !busy)
                        Begin(client.Logout(() => { history = Array.Empty<AccountRun>(); savedAttempt = false; Done("Signed out."); }));
                }
            }
            GUI.Label(new Rect(x + 30f, y + 515f, 555f, 30f), busy ? "Working..." : message);
            GUI.matrix = old;
        }
        private void SaveResult()
        {
            if (run.Phase != MountainPhase.Results || string.IsNullOrEmpty(run.CompletedAttemptId))
            { message = "Finish a Glacier Pass run before saving."; return; }
            if (savedAttempt && savedId == run.CompletedAttemptId)
            { message = "This attempt is already saved."; return; }
            var data = new AccountRun
            {
                attemptId = run.CompletedAttemptId, route = run.Line == MountainLine.Pine ? "Pine" : "Summit",
                practice = run.IsPractice, seconds = run.Elapsed, distanceMeters = run.DistanceMeters,
                topSpeedKmh = run.PeakSpeed, score = run.Tricks.Points, tricks = run.Tricks.CompletedTricks,
                completedUtc = DateTime.UtcNow.ToString("O"), course = LocalRunStore.Course,
                courseVersion = LocalRunStore.CourseVersion, rulesVersion = LocalRunStore.RulesVersion
            };
            Begin(client.Save(data, (ok, status) => { if (ok) { savedAttempt = true; savedId = data.attemptId; } Done(status); }));
        }
        private void LoadHistory()
        {
            page = 1;
            Begin(client.History((runs, fetchedBests, error) =>
            {
                history = runs ?? Array.Empty<AccountRun>();
                bests = fetchedBests ?? Array.Empty<AccountBest>();
                Done(error);
            }));
        }
        private void Begin(IEnumerator operation) { busy = true; StartCoroutine(Finish(operation)); }
        private IEnumerator Finish(IEnumerator operation)
        {
            yield return operation;
            busy = false;
            password = "";
        }
        private void Done(string status) { message = status; }
    }
}

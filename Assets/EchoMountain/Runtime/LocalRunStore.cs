using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace EchoMountain.Riding
{
    [Serializable]
    public sealed class FinishedRun
    {
        public float seconds;
        public float peakKmh;
        public int points;
        public string completedUtc;
        public string build;
    }

    [Serializable]
    public sealed class PersonalRecord
    {
        public MountainLine line;
        public bool practice;
        public int finishes;
        public FinishedRun previous;
        public FinishedRun fastest;
        public FinishedRun highestScore;
    }

    [Serializable]
    public sealed class LocalRunData
    {
        // No field initializers: incomplete JSON must fail validation.
        public int schema;
        public string course;
        public int courseVersion;
        public int rulesVersion;
        public string assist;
        public bool sound;
        public MountainLine selectedLine;
        public PersonalRecord[] records;
    }

    // Owns a small, bounded offline record book. Physics and UI do not own file I/O.
    public sealed class LocalRunStore
    {
        public const string Course = "glacier-pass";
        public const int CourseVersion = 1;
        public const int RulesVersion = 1;
        public const string Assist = "balanced-lab-v1";
        public const int Schema = 1;
        private const int MaximumBytes = 65536;
        private readonly string path;
        private bool readOnly;
        public LocalRunData Data { get; private set; }
        public string Notice { get; private set; } = "";

        public LocalRunStore(string filePath)
        {
            path = filePath;
            Data = Fresh();
            Load();
        }

        public static LocalRunStore OpenDefault()
        {
            // Editor/test sessions never write the player's records.
            string directory = Application.isEditor
                ? Path.Combine(Application.temporaryCachePath, "EchoMountain-Editor")
                : Application.persistentDataPath;
            if (Application.isBatchMode)
                directory = Path.Combine(Application.temporaryCachePath, "EchoMountain-Batch", Guid.NewGuid().ToString("N"));
            return new LocalRunStore(Path.Combine(directory, "runs-v1.json"));
        }

        private static LocalRunData Fresh() => new()
        {
            schema = Schema, course = Course, courseVersion = CourseVersion,
            rulesVersion = RulesVersion, assist = Assist, sound = true,
            records = new[]
            {
                new PersonalRecord { line = MountainLine.Pine },
                new PersonalRecord { line = MountainLine.Summit },
                new PersonalRecord { line = MountainLine.Summit, practice = true }
            }
        };

        public PersonalRecord Record(MountainLine line, bool practice) =>
            Array.Find(Data.records, record => record.line == line && record.practice == practice);

        public bool Finish(MountainLine line, bool practice, float seconds, float peakKmh, int points, string build)
        {
            var finished = new FinishedRun
            {
                seconds = seconds, peakKmh = peakKmh, points = points,
                completedUtc = DateTime.UtcNow.ToString("O"), build = build
            };
            var record = Record(line, practice);
            if (record == null || !ValidRun(finished)) return false;
            record.finishes = Math.Min(record.finishes + 1, 1000000);
            record.previous = finished;
            if (record.fastest == null || seconds < record.fastest.seconds) record.fastest = finished;
            if (record.highestScore == null || points > record.highestScore.points) record.highestScore = finished;
            return Save();
        }

        public bool SetPreferences(bool sound, MountainLine line)
        {
            if (line != MountainLine.Pine && line != MountainLine.Summit) return false;
            Data.sound = sound;
            Data.selectedLine = line;
            return Save();
        }

        private void Load()
        {
            if (TryRead(path, out var data, out bool incompatible))
            {
                Data = data;
                return;
            }
            if (incompatible)
            {
                readOnly = true;
                Notice = "Save belongs to another version. Records are temporary.";
                return;
            }
            if (TryRead(path + ".bak", out data, out bool backupIncompatible))
            {
                Data = data;
                Notice = "Recovered local records from backup.";
            }
            else if (backupIncompatible)
            {
                readOnly = true;
                Notice = "Backup belongs to another version. Records are temporary.";
            }
            else if (File.Exists(path) || File.Exists(path + ".bak"))
                Notice = "Local records could not be read. Starting a fresh record book.";
        }

        private static bool Compatible(LocalRunData data) =>
            data.schema == Schema && data.course == Course && data.courseVersion == CourseVersion
            && data.rulesVersion == RulesVersion && data.assist == Assist;

        private static bool TryRead(string file, out LocalRunData data, out bool incompatible)
        {
            data = null;
            incompatible = false;
            try
            {
                if (!File.Exists(file) || new FileInfo(file).Length > MaximumBytes) return false;
                data = JsonUtility.FromJson<LocalRunData>(File.ReadAllText(file));
                if (data == null) return false;
                incompatible = data.schema > 0 && !Compatible(data);
                // Unity serializes null inline class fields as empty objects. Normalize only
                // the exact empty sentinel, so corrupt nonzero records still fail validation.
                if (data.records != null && data.records.Length == 3)
                    foreach (var record in data.records)
                        if (record != null && record.finishes == 0)
                        {
                            if (EmptyRun(record.previous)) record.previous = null;
                            if (EmptyRun(record.fastest)) record.fastest = null;
                            if (EmptyRun(record.highestScore)) record.highestScore = null;
                        }
                return !incompatible && Valid(data);
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException)
            {
                return false;
            }
        }

        private static bool EmptyRun(FinishedRun run) => run == null ||
            (run.seconds == 0f && run.peakKmh == 0f && run.points == 0
            && string.IsNullOrEmpty(run.completedUtc) && string.IsNullOrEmpty(run.build));

        private static bool Valid(LocalRunData data)
        {
            if (!Compatible(data) || data.records == null || data.records.Length != 3
                || (data.selectedLine != MountainLine.Pine && data.selectedLine != MountainLine.Summit)) return false;
            for (int index = 0; index < data.records.Length; index++)
            {
                var record = data.records[index];
                if (record == null || record.line != (index == 0 ? MountainLine.Pine : MountainLine.Summit)
                    || record.practice != (index == 2) || record.finishes < 0 || record.finishes > 1000000) return false;
                if (record.finishes == 0)
                {
                    if (record.previous != null || record.fastest != null || record.highestScore != null) return false;
                }
                else if (!ValidRun(record.previous) || !ValidRun(record.fastest) || !ValidRun(record.highestScore)
                    || record.fastest.seconds > record.previous.seconds || record.highestScore.points < record.previous.points) return false;
            }
            return true;
        }

        private static bool ValidRun(FinishedRun run) => run != null
            && float.IsFinite(run.seconds) && run.seconds > 0f && run.seconds <= 3600f
            && float.IsFinite(run.peakKmh) && run.peakKmh >= 0f && run.peakKmh <= 300f
            && run.points >= 0 && run.points <= 10000000
            && !string.IsNullOrEmpty(run.build) && run.build.Length <= 64
            && DateTime.TryParse(run.completedUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out _);

        private bool Save()
        {
            if (readOnly || !Valid(Data)) return false;
            try
            {
                // Refuse to replace an incompatible file even if it changed after Load.
                bool primaryValid = TryRead(path, out _, out bool incompatible);
                TryRead(path + ".bak", out _, out bool backupIncompatible);
                if (incompatible || backupIncompatible)
                {
                    readOnly = true;
                    Notice = "Save belongs to another version. Records are temporary.";
                    return false;
                }
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
                byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(Data, true));
                if (bytes.Length > MaximumBytes) return false;
                using (var stream = new FileStream(path + ".tmp", FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }
                if (primaryValid)
                    File.Replace(path + ".tmp", path, path + ".bak");
                else
                {
                    // Keep one damaged primary for diagnosis; never replace a good backup with it.
                    if (File.Exists(path))
                    {
                        if (File.Exists(path + ".invalid")) File.Delete(path + ".invalid");
                        File.Move(path, path + ".invalid");
                    }
                    File.Move(path + ".tmp", path);
                }
                Notice = "Saved on this PC.";
                return true;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is NotSupportedException)
            {
                Notice = "Could not save on this PC. This session remains playable.";
                return false;
            }
        }
    }
}

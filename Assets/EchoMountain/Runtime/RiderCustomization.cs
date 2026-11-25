using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace EchoMountain.Riding
{
    [Serializable]
    public sealed class RiderLook
    {
        public int version = 1;
        public int jacket;
        public int pants;
        public int helmet;
        public int goggles;
        public int topside;
        public int underside;

        public RiderLook Copy() => (RiderLook)MemberwiseClone();
    }

    public static class RiderCustomization
    {
        public static readonly string[] ColorNames = { "Ember", "Glacier", "Pine", "Slate", "Snow" };
        public static readonly Color[] Colors =
        {
            new(0.84f, 0.28f, 0.13f), new(0.12f, 0.67f, 0.77f),
            new(0.15f, 0.46f, 0.31f), new(0.16f, 0.21f, 0.26f),
            new(0.88f, 0.92f, 0.91f)
        };
        public static readonly string[] DeckNames = { "Alpine", "Signal", "Night Run" };
        public static readonly Color[] DeckColors =
        {
            new(0.16f, 0.65f, 0.78f), new(0.90f, 0.43f, 0.17f), new(0.22f, 0.23f, 0.35f)
        };
        public static readonly Color[] DeckAccents =
        {
            new(0.94f, 0.98f, 0.94f), new(0.14f, 0.19f, 0.22f), new(0.88f, 0.75f, 0.40f)
        };

        public static bool Valid(RiderLook look) => look != null && look.version == 1
            && InRange(look.jacket, ColorNames.Length) && InRange(look.pants, ColorNames.Length)
            && InRange(look.helmet, ColorNames.Length) && InRange(look.goggles, ColorNames.Length)
            && InRange(look.topside, DeckNames.Length) && InRange(look.underside, DeckNames.Length);

        private static bool InRange(int index, int count) => index >= 0 && index < count;
    }

    // Cosmetic data is independent of course records and their versioned physics rules.
    public sealed class RiderCustomizationStore
    {
        public const string FileName = "rider-look-v1.json";
        private readonly string path;
        public RiderLook Look { get; private set; } = new();
        public string Notice { get; private set; } = "";
        private static readonly string BatchDirectory = Path.Combine(Application.temporaryCachePath,
            "EchoMountain-Batch-Look-" + Guid.NewGuid().ToString("N"));

        public RiderCustomizationStore(string filePath)
        {
            path = filePath;
            try
            {
                if (!File.Exists(path)) return;
                if (new FileInfo(path).Length > 4096) throw new InvalidDataException();
                RiderLook loaded = JsonUtility.FromJson<RiderLook>(File.ReadAllText(path));
                if (!RiderCustomization.Valid(loaded)) throw new InvalidDataException();
                Look = loaded;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException)
            {
                Notice = "Saved look could not be read. Showing default colors.";
            }
        }

        public static RiderCustomizationStore OpenDefault()
        {
            string directory = Application.isBatchMode ? BatchDirectory : Application.isEditor
                ? Path.Combine(Application.temporaryCachePath, "EchoMountain-Editor") : Application.persistentDataPath;
            return new RiderCustomizationStore(Path.Combine(directory, FileName));
        }

        public bool Set(RiderLook look)
        {
            if (!RiderCustomization.Valid(look)) return false;
            Look = look.Copy();
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
                string temporary = path + ".tmp";
                byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(Look, true));
                using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }
                if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
                else File.Move(temporary, path);
                Notice = "Look saved on this PC.";
                return true;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException || error is NotSupportedException)
            {
                Notice = "Could not save look. This session still uses it.";
                return false;
            }
        }
    }
}

using System;
using System.IO;
using UnityEngine;
using ArrowFlow.Core;

namespace ArrowFlow
{
    public static class SaveStore
    {
#if UNITY_EDITOR
        // Editor verification uses a disposable workspace directory, never player saves.
        public static string VerificationDirectory { get; set; }
#endif
        static string PathName
        {
            get
            {
#if UNITY_EDITOR
                if (!string.IsNullOrEmpty(VerificationDirectory))
                    return Path.Combine(VerificationDirectory, "progress-v1.json");
#endif
                return Path.Combine(Application.persistentDataPath, "progress-v1.json");
            }
        }

        public static string LastError { get; private set; }

        public static Progress Load()
        {
            foreach (string path in new[]
            {
                PathName,
                PathName + ".bak"
            }

            )
                try
                {
                    if (!File.Exists(path))
                        continue;
                    var p = JsonUtility.FromJson<Progress>(File.ReadAllText(path));
                    if (p == null || p.version != 1 || p.days == null || p.attempts == null || p.weeklyWins == null)
                        continue;
                    p.unlocked = Mathf.Clamp(p.unlocked, 1, 501);
                    p.attempts.RemoveAll(a => a == null || a.key == null || a.removed == null);
                    foreach (var a in p.attempts)
                    {
                        a.lives = Mathf.Clamp(a.lives, 0, 3);
                        a.hintsUsed = Mathf.Clamp(a.hintsUsed, 0, Attempt.HintLimit);
                    }
                    return p;
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Save recovery: " + e.Message);
                }

            return new Progress();
        }

        public static bool Save(Progress p)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(PathName));
                File.WriteAllText(PathName + ".tmp", JsonUtility.ToJson(p, true));
                if (File.Exists(PathName))
                    File.Copy(PathName, PathName + ".bak", true);
                if (File.Exists(PathName))
                    File.Delete(PathName);
                File.Move(PathName + ".tmp", PathName);
                LastError = null;
                return true;
            }
            catch (Exception e)
            {
                LastError = "Could not save progress. Check free storage.";
                Debug.LogError(e);
                return false;
            }
        }

        public static void Delete()
        {
            foreach (string s in new[]
            {
                "",
                ".bak",
                ".tmp"
            }

            )
                if (File.Exists(PathName + s))
                    File.Delete(PathName + s);
        }
    }
}

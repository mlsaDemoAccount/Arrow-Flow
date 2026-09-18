using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace ArrowFlow
{
    [Serializable]
    public class TournamentConfiguration
    {
        public string url;
    }

    [Serializable]
    public class TournamentEntry
    {
        public string name;
        public int rank, score;
    }

    [Serializable]
    public class TournamentEvent
    {
        public string week;
        public int nextLevel, score, rank;
        public TournamentEntry[] entries;
        public string[] medals;
    }

    [Serializable]
    public class Registration
    {
        public string token;
    }

    [Serializable]
    public class SolutionProof
    {
        public string week;
        public int level;
        public int[] moves;
    }

    public static class TournamentClient
    {
        static string endpoint;
        static string Token
        {
            get
            {
                return PlayerPrefs.GetString("tournament-token", "");
            }

            set
            {
                PlayerPrefs.SetString("tournament-token", value);
                PlayerPrefs.Save();
            }
        }

        public static bool Configured
        {
            get
            {
                if (endpoint == null)
                {
                    var asset = Resources.Load<TextAsset>("TournamentConfiguration");
                    endpoint = asset == null ? "" : JsonUtility.FromJson<TournamentConfiguration>(asset.text).url ?? "";
                    endpoint = endpoint.TrimEnd('/');
                }

                return endpoint.StartsWith("https://", StringComparison.Ordinal) || endpoint.StartsWith("http://localhost:", StringComparison.Ordinal) || endpoint.StartsWith("http://127.0.0.1:", StringComparison.Ordinal);
            }
        }

        static UnityWebRequest Request(string path, string method, string body = null)
        {
            var r = new UnityWebRequest(endpoint + path, method);
            r.downloadHandler = new DownloadHandlerBuffer();
            r.timeout = 15;
            if (body != null)
            {
                r.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
                r.SetRequestHeader("Content-Type", "application/json");
            }

            if (Token.Length > 0)
                r.SetRequestHeader("Authorization", "Bearer " + Token);
            return r;
        }

        public static IEnumerator Load(Action<TournamentEvent, string> done)
        {
            if (!Configured)
            {
                done(null, "Tournaments aren't available right now.");
                yield break;
            }

            if (Token.Length == 0)
            {
                using (var r = Request("/players", "POST"))
                {
                    yield return r.SendWebRequest();
                    if (r.result != UnityWebRequest.Result.Success)
                    {
                        done(null, "Couldn't connect. Please try again later.");
                        yield break;
                    }

                    Token = JsonUtility.FromJson<Registration>(r.downloadHandler.text).token;
                }
            }

            using (var r = Request("/event", "GET"))
            {
                yield return r.SendWebRequest();
                if (r.result != UnityWebRequest.Result.Success)
                {
                    if (r.responseCode == 401)
                        Token = "";
                    done(null, "Couldn't load the tournament. Please try again.");
                    yield break;
                }

                done(JsonUtility.FromJson<TournamentEvent>(r.downloadHandler.text), null);
            }
        }

        public static IEnumerator Submit(SolutionProof proof, Action<string> done)
        {
            using (var r = Request("/solve", "POST", JsonUtility.ToJson(proof)))
            {
                yield return r.SendWebRequest();
                done(r.result == UnityWebRequest.Result.Success ? null : r.responseCode == 400 ? "This tournament has ended or the solution was rejected." : "Couldn't send your result. It is saved for retry.");
            }
        }

        public static IEnumerator Delete(Action<bool> done)
        {
            if (!Configured || Token.Length == 0)
            {
                Token = "";
                done(true);
                yield break;
            }

            using (var r = Request("/players/me", "DELETE"))
            {
                yield return r.SendWebRequest();
                bool ok = r.result == UnityWebRequest.Result.Success || r.responseCode == 401;
                if (ok)
                    Token = "";
                done(ok);
            }
        }
    }
}

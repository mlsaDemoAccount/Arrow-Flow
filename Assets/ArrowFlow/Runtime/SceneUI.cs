using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace ArrowFlow
{
    // Serialized scene references. Names and parenting can be edited without breaking these links.
    public sealed class SceneUI : MonoBehaviour
    {
        [Serializable] public class Entry
        {
            public string id;
            public GameObject gameObject;
            public Text text;
            public Button button;
            public Graphic graphic;
        }
        public Canvas canvas;
        public RectTransform design;
        public BoardView board;
        public BoardView hero;
        public AudioSource audioSource;
        public List<Entry> elements = new List<Entry>();
        Dictionary<string, Entry> index;
        public Entry Get(string id)
        {
            if (index == null) { index = new Dictionary<string, Entry>(); foreach (var e in elements) index.Add(e.id, e); }
            Entry entry;
            if (!index.TryGetValue(id, out entry)) throw new InvalidOperationException("Missing scene UI reference: " + id + ". Open the updated Main scene or use Build scene UI.");
            return entry;
        }
        public void Text(string id, string value) { Get(id).text.text = value; }
        public void Active(string id, bool enabled) { Get(id).gameObject.SetActive(enabled); }
        public void Interactable(string id, bool enabled) { Get(id).button.interactable = enabled; }
        public void ShowScreen(string id)
        {
            foreach (var entry in elements)
                if (entry.id.StartsWith("Screen/", StringComparison.Ordinal)) entry.gameObject.SetActive(entry.id == "Screen/" + id);
        }
        public void ShowDialog(string id)
        {
            foreach (var entry in elements)
                if (entry.id.StartsWith("Dialog/", StringComparison.Ordinal)) entry.gameObject.SetActive(entry.id == "Dialog/" + id);
        }
    }
}

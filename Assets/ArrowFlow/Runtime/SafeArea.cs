using UnityEngine;
namespace ArrowFlow
{
    [ExecuteAlways, RequireComponent(typeof(RectTransform))]
    public sealed class SafeArea : MonoBehaviour
    {
        void OnEnable() { Apply(); }
        void Update() { Apply(); }
        void Apply()
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;
            Rect area = Screen.safeArea;
            Vector2 size = new Vector2(Screen.width, Screen.height);
            var r = (RectTransform)transform;
            Vector2 min = area.position / size, max = (area.position + area.size) / size;
            if (r.anchorMin != min) r.anchorMin = min;
            if (r.anchorMax != max) r.anchorMax = max;
            if (r.offsetMin != Vector2.zero) r.offsetMin = Vector2.zero;
            if (r.offsetMax != Vector2.zero) r.offsetMax = Vector2.zero;
        }
    }
}

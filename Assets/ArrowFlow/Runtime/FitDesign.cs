using UnityEngine;
namespace ArrowFlow
{
    [ExecuteAlways, RequireComponent(typeof(RectTransform))]
    public sealed class FitDesign : MonoBehaviour
    {
        void LateUpdate()
        {
            var r = (RectTransform)transform;
            var parent = r.parent as RectTransform;
            if (parent == null || r.rect.width <= 0 || r.rect.height <= 0) return;
            float s = Mathf.Min(parent.rect.width / r.rect.width, parent.rect.height / r.rect.height);
            var scale = new Vector3(s, s, 1);
            if (r.localScale != scale) r.localScale = scale;
        }
    }
}

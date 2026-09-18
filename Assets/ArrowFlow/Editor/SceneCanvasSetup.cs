using UnityEditor;
namespace ArrowFlow.Editor
{
    public static class SceneCanvasSetup
    {
        [MenuItem("Arrow Flow/Create scene Canvas")]
        public static void Create() { SceneUIBuilder.Build(); }
    }
}

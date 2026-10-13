using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ArrowFlow.Editor
{
    // These UI meshes use Canvas's default material, so there is no material
    // asset dependency to keep UI/Default in a player build.
    public sealed class UIShaderBuildGuard : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report) => EnsureIncluded();

        [MenuItem("Arrow Attack/Repair UI shader inclusion")]
        public static void EnsureIncluded()
        {
            var shader = Shader.Find("UI/Default");
            if (shader == null)
                throw new BuildFailedException("Arrow Attack requires the built-in UI/Default shader.");

            var graphics = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (graphics.Length == 0)
                throw new BuildFailedException("Cannot load GraphicsSettings to preserve the UI shader.");

            var settings = new SerializedObject(graphics[0]);
            var shaders = settings.FindProperty("m_AlwaysIncludedShaders");
            if (shaders == null || !shaders.isArray)
                throw new BuildFailedException("Cannot find Always Included Shaders in GraphicsSettings.");

            for (int i = 0; i < shaders.arraySize; i++)
                if (shaders.GetArrayElementAtIndex(i).objectReferenceValue == shader)
                {
                    Debug.Log("ARROWFLOW_UI_SHADER_OK: UI/Default is included.");
                    return;
                }

            int index = shaders.arraySize;
            shaders.InsertArrayElementAtIndex(index);
            shaders.GetArrayElementAtIndex(index).objectReferenceValue = shader;
            settings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            Debug.Log("ARROWFLOW_UI_SHADER_FIXED: Added UI/Default to Always Included Shaders.");
        }
    }
}

using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace ArrowFlow.Editor
{
    // The game's name and app icon in Player Settings. Applied automatically while the project
    // still has the old name or no icon, before every build (BuildTools.Prepare), and from
    // Arrow Attack > Apply name and icon. To change the icon, replace AppIcon.png.
    [InitializeOnLoad]
    public static class Branding
    {
        public const string ProductName = "Arrow Attack";
        public const string CompanyName = "ArrowAttackStudio";
        public const string IconPath = "Assets/ArrowFlow/Art/Icon/AppIcon.png";
        const string OldProductName = "Arrow Flow", OldCompanyName = "ArrowFlowStudio";

        static Branding()
        {
            EditorApplication.delayCall += ApplyIfNeeded;
        }

        internal static void ApplyIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            bool changed = false;
            if (PlayerSettings.productName == OldProductName || PlayerSettings.companyName == OldCompanyName)
            {
                SetNames();
                changed = true;
            }
            if (!HasDefaultIcon() && SetIcon()) changed = true;
            if (changed) AssetDatabase.SaveAssets();
        }

        [MenuItem("Arrow Attack/Apply name and icon")]
        public static void Apply()
        {
            SetNames();
            if (!SetIcon()) Debug.LogWarning("Arrow Attack app icon not found at " + IconPath + ".");
            AssetDatabase.SaveAssets();
        }

        static void SetNames()
        {
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
        }

        // The default icon: Unity scales it to every Android, iOS and Windows icon size.
        static bool SetIcon()
        {
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon == null) return false;
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            return true;
        }

        static bool HasDefaultIcon()
        {
            foreach (var icon in PlayerSettings.GetIcons(NamedBuildTarget.Unknown, IconKind.Any))
                if (icon != null) return true;
            return false;
        }
    }

    // The icon can finish importing after the scripts reload, so apply it once it is there.
    sealed class BrandingIconImport : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (Array.IndexOf(imported, Branding.IconPath) >= 0)
                EditorApplication.delayCall += Branding.ApplyIfNeeded;
        }
    }
}

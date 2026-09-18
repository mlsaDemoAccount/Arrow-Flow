#if UNITY_IOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
namespace ArrowFlow.Editor {
public static class IOSExport {
 [PostProcessBuild(100)]public static void Finish(BuildTarget target,string path){if(target!=BuildTarget.iOS)return;string file=PBXProject.GetPBXProjectPath(path);var project=new PBXProject();project.ReadFromFile(file);project.AddFrameworkToProject(project.GetUnityFrameworkTargetGuid(),"UserNotifications.framework",false);project.WriteToFile(file);}
}
#endif


using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;
using System.Linq;

public static class FFHAndroidBuilder
{
    public static void Build()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.applicationIdentifier = "fun.tophouse.ffh.mobile";
        PlayerSettings.productName = "Femboy Futa House";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;

        Directory.CreateDirectory("Builds/Android");

        var scenes=EditorBuildSettings.scenes
            .Where(s=>s.enabled && File.Exists(s.path))
            .Select(s=>s.path).ToArray();

        if(scenes.Length==0)
            scenes=AssetDatabase.FindAssets("t:Scene")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p=>p.StartsWith("Assets/"))
                .ToArray();

        if(scenes.Length==0)
            throw new System.Exception("No scenes found.");

        var opts=new BuildPlayerOptions {
            scenes=scenes,
            locationPathName="Builds/Android/Femboy_Futa_House_v1.177_Android.apk",
            target=BuildTarget.Android,
            options=BuildOptions.None
        };

        var report=BuildPipeline.BuildPlayer(opts);
        if(report.summary.result!=BuildResult.Succeeded)
            throw new System.Exception("Android build failed: "+report.summary.result);
    }
}

using UnityEditor;
using UnityEngine.InputSystem;
using System.Linq;

public static class FFHInputActionsPostprocessor
{
    [InitializeOnLoadMethod]
    static void QueuePatch() => EditorApplication.delayCall += Patch;

    static void Patch()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:InputActionAsset"))
        {
            var path=AssetDatabase.GUIDToAssetPath(guid);
            var asset=AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
            if(asset==null) continue;

            bool dirty=false;
            foreach(var map in asset.actionMaps)
            foreach(var action in map.actions)
            {
                var controls=action.bindings.Select(b=>b.path).Where(p=>p!=null).ToArray();

                if(controls.Contains("<Mouse>/position") &&
                   !controls.Contains("<Touchscreen>/primaryTouch/position"))
                {
                    action.AddBinding("<Touchscreen>/primaryTouch/position");
                    dirty=true;
                }

                if(controls.Contains("<Mouse>/leftButton") &&
                   !controls.Contains("<Touchscreen>/primaryTouch/press"))
                {
                    action.AddBinding("<Touchscreen>/primaryTouch/press");
                    dirty=true;
                }

                if(controls.Contains("<Mouse>/rightButton") &&
                   !controls.Contains("<Touchscreen>/touch1/press"))
                {
                    action.AddBinding("<Touchscreen>/touch1/press");
                    dirty=true;
                }
            }

            if(dirty)
            {
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssetIfDirty(asset);
            }
        }

        AssetDatabase.SaveAssets();
    }
}

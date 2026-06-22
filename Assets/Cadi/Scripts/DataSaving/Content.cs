using System.IO;
using UnityEditor;
using UnityEngine;

namespace Cadi.Scripts.DataSaving
{
    [CreateAssetMenu(fileName = "GeneralContent", menuName = "Content/GeneralContent", order = 0)]
    public class Content : ScriptableObject
    {
        private const string c_ResourcesAssetPath = "Content/GeneralContent"; // Resources/Content/GeneralContent.asset
        private static Content s_Content;

        public static Content Get()
        {
            if (s_Content != null)
            {
                return s_Content;
            }

            s_Content = Resources.Load<Content>(c_ResourcesAssetPath);

#if UNITY_EDITOR
            if (s_Content != null)
                return s_Content;

            Debug.Log("Content not found. Creating a new one at Assets/Resources/Content/GeneralContent.asset");
            s_Content = CreateInstance<Content>();
            EnsureAssetExists(s_Content);
#endif

            return s_Content;
        }


#if UNITY_EDITOR
        // private void OnValidate()
        // {
        //     Refresh();
        // }

        private static void EnsureAssetExists(Content content)
        {
            const string folderPath = "Assets/Resources/Content";
            const string assetPath = "Assets/Resources/Content/GeneralContent.asset";

            if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");
            }

            if (!UnityEditor.AssetDatabase.IsValidFolder(folderPath))
            {
                UnityEditor.AssetDatabase.CreateFolder("Assets/Resources", "Settings");
            }

            AssetDatabase.CreateAsset(content, assetPath);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
        }

        private static void EnsureFolderExists(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folderPath)?.Replace("\\", "/");
            string name = Path.GetFileName(folderPath);

            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolderExists(parent);
            }

            AssetDatabase.CreateFolder(parent, name);
        }

#endif
    }
}
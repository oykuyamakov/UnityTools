#if UNITY_EDITOR
using Cadi.Scripts.CacherSystem;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CadiKazani.Scripts.CacherSystem
{
    public sealed class CacherReferenceBuildCheck : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var previousSetup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                var scenes = EditorBuildSettings.scenes;

                for (int i = 0; i < scenes.Length; i++)
                {
                    if (!scenes[i].enabled)
                    {
                        continue;
                    }

                    string path = scenes[i].path;
                    Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

                    GameObject[] roots = scene.GetRootGameObjects();

                    for (int r = 0; r < roots.Length; r++)
                    {
                        var resolvers = roots[r].GetComponentsInChildren<CacherMonoBehaviour>(true);

                        for (int b = 0; b < resolvers.Length; b++)
                        {
                            var comp = resolvers[b];

                            comp.ResolveReferences();

                            if (comp.LastResolveHadErrors)
                            {
                                throw new BuildFailedException(
                                    $"AutoReference build check failed. Missing required references on: {comp.name} ({comp.GetType().Name}) in scene: {path}");
                            }
                        }
                    }
                }
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }
        }
    }
}
#endif
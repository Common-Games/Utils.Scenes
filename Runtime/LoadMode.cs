using System;

using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.SceneManagement.SceneManager;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using static UnityEditor.SceneManagement.EditorSceneManager;
#endif

namespace CGTK.Utils.Scenes
{
    using static LoadMode;
    
    public enum LoadMode
    {
        Overwrite,
        Additive,
        AdditiveWithoutLoading,
        
        /*
        AsyncSingle,                
        AsyncAdditive,              
        AsyncAdditiveWithoutLoading,
        */
    }
    
    public static class LoadModeExtensions
    {
        //Hacky ass code, but blame the C# team for not allowing operators in enums OR extension methods.
        public static void Deconstruct(this LoadMode mode, out Action<SceneRef> action, out bool _)
        {
            #if UNITY_EDITOR
            
            bool _isInEditMode = !Application.isPlaying;

            if (_isInEditMode)
            {
                switch (mode)
                {
                    case Overwrite:
                    //case AsyncSingle: 
                        (action, _) = (OpenSceneSingle, false);
                        return;
                    case Additive:
                    //case AsyncAdditive:
                        (action, _) = (OpenSceneAdditive, false);
                        return;
                    case AdditiveWithoutLoading:
                    //case AsyncAdditiveWithoutLoading:
                        (action, _) = (OpenSceneAdditiveWithoutLoading, false);
                        return;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
                }
            }

            #endif
            
            switch (mode)
            {
                case Overwrite:
                    (action, _) = (LoadSceneSingle, false);
                    return;
                case Additive:
                    (action, _) = (LoadSceneAdditive, false);
                    return;
                case AdditiveWithoutLoading:
                    (action, _) = (LoadSceneAdditiveWithoutLoading, false);
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
                
                /*
                case AsyncSingle: 
                    (action, throwAway) = OpenSceneSingle;
                    return;
                case AsyncAdditive:
                    (action, throwAway) = OpenSceneAdditive;
                    return;
                case AsyncAdditiveWithoutLoading:
                    (action, throwAway) = OpenSceneAdditiveWithoutLoading;
                    return;
                */
            }
        }

        private static void OpenSceneSingle(SceneRef scene)
        {
            OpenScene(scenePath: scene.Path, mode: OpenSceneMode.Single);
        }
        private static void OpenSceneAdditive(SceneRef scene)
        {
            OpenScene(scenePath: scene.Path, mode: OpenSceneMode.Additive);
        }
        private static void OpenSceneAdditiveWithoutLoading(SceneRef scene)
        {
            OpenScene(scenePath: scene.Path, mode: OpenSceneMode.AdditiveWithoutLoading);
        }
        
        private static void LoadSceneSingle(SceneRef scene)
        {
            LoadScene(sceneBuildIndex: scene.Index, mode: LoadSceneMode.Single);
        }
        private static void LoadSceneAdditive(SceneRef scene)
        {
            LoadScene(sceneBuildIndex: scene.Index, mode: LoadSceneMode.Additive);
        }
        private static void LoadSceneAdditiveWithoutLoading(SceneRef scene)
        {
            Debug.Log(message: $"Ignoring Scene {scene}");
        }
        
        /*
        private static void LoadSceneAsyncSingle(SceneRef scene)
        {
            LoadSceneAsync(sceneBuildIndex: scene.Index, mode: LoadSceneMode.Single);
        }
        private static void LoadSceneAsyncAdditive(SceneRef scene)
        {
            LoadScene(sceneBuildIndex: scene.Index, mode: LoadSceneMode.Additive);
        }
        private static void LoadSceneAsyncAdditiveWithoutLoading(SceneRef scene)
        {
            OpenScene(scenePath: scene.Path, mode: OpenSceneMode.AdditiveWithoutLoading);
        }
        */
    }
}

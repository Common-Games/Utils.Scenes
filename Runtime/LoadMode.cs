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
    }
    
    public static class LoadModeExtensions
    {
        public static void GetLoadAction(this LoadMode mode, out Action<SceneReference> action)
        {
            #if UNITY_EDITOR
            
            bool _isInEditMode = !Application.isPlaying;

            if (_isInEditMode)
            {
                switch (mode)
                {
                    case Overwrite: 
                        action = scene => OpenScene(scenePath: scene.Path, mode: OpenSceneMode.Single);
                        return;
                    case Additive: 
                        action = scene => OpenScene(scenePath: scene.Path, mode: OpenSceneMode.Additive);
                        return;
                    case AdditiveWithoutLoading: 
                        action = scene => OpenScene(scenePath: scene.Path, mode: OpenSceneMode.AdditiveWithoutLoading);
                        return;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
                }
            }

            #endif
            
            switch (mode)
            {
                case Overwrite:
                    action = scene => LoadScene(sceneName: scene.Path, mode: LoadSceneMode.Single);
                    return;
                case Additive:
                    action = scene => LoadScene(sceneName: scene.Path, mode: LoadSceneMode.Additive);
                    return;
                case AdditiveWithoutLoading:
                    action = scene => Debug.Log(message: $"Ignoring Scene {scene}");
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }
        
        //Hacky ass code, but blame the C# team for not allowing operators in enums OR extension methods.
        public static void Deconstruct(this LoadMode mode, out Action<SceneRef> action, out bool _) //func for async?
        {
            #if UNITY_EDITOR
            
            bool _isInEditMode = !Application.isPlaying;

            if (_isInEditMode)
            {
                switch (mode)
                {
                    case Overwrite:
                        (action, _) = (scene => OpenScene(scenePath: scene.Path, mode: OpenSceneMode.Single), false);
                        return;
                    case Additive:
                        (action, _) = (scene => OpenScene(scenePath: scene.Path, mode: OpenSceneMode.Additive), false);
                        return;
                    case AdditiveWithoutLoading:
                        (action, _) = (scene => OpenScene(scenePath: scene.Path, mode: OpenSceneMode.AdditiveWithoutLoading), false);
                        return;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
                }
            }

            #endif
            
            switch (mode)
            {
                case Overwrite:
                    (action, _) = (scene => LoadScene(sceneBuildIndex: scene.Index, mode: LoadSceneMode.Single), false);
                    return;
                case Additive:
                    (action, _) = (scene => LoadScene(sceneBuildIndex: scene.Index, mode: LoadSceneMode.Additive), false);
                    return;
                case AdditiveWithoutLoading:
                    (action, _) = (scene => Debug.Log(message: $"Ignoring Scene {scene}"), false);
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }
    }
}

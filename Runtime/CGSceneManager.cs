using UnityEngine.SceneManagement;

namespace CGTK.Utils.Scenes
{
    public static class CGSceneManager
    {
        public static Scene[] OpenScenes
        {
            get
            {
                //TODO: Cache. If I can intercept loading and unloading of scenes it should just work.
            
                int _sceneCount = SceneManager.sceneCount;
                Scene[] _scenes = new Scene[_sceneCount];
 
                for (int _index = 0; _index < _sceneCount; _index += 1)
                {
                    _scenes[_index] = SceneManager.GetSceneAt(index: _index);
                }

                return _scenes;    
            }
        }

        /*
        public static Scene[] GetOpenScenes()//, out Scene[] scenes)
        {
            //TODO: Cache.
            
            int _sceneCount = SceneManager.sceneCount;
            Scene[] _loadedScenes = new Scene[_sceneCount];
 
            for (int _index = 0; _index < _sceneCount; _index += 1)
            {
                _loadedScenes[_index] = SceneManager.GetSceneAt(index: _index);
            }

            return _loadedScenes;
        }
        */
    }
}

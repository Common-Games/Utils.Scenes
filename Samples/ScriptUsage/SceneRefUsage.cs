using UnityEngine;
using UnityEngine.SceneManagement;

namespace CGTK.Utils.Scenes.Samples.ScriptUsage.Samples.ScriptUsage
{
    public class SceneRefUsage : MonoBehaviour
    {
        [SerializeField] private SceneRef sceneToLoad0;
        
        [SerializeField] private SceneRef sceneToLoad1;
    
        private void Start()
        {
            SceneManager.LoadScene(sceneBuildIndex: sceneToLoad0);
        }
    }
}

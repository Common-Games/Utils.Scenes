using UnityEngine;

namespace CGTK.Utils.Scenes.Samples.ScriptUsage.Samples.ScriptUsage
{
    public sealed class SceneReferenceUsage : MonoBehaviour
    {
        [SerializeField] private SceneReference sceneA;
        
        [SerializeField] private SceneReference sceneB;

        private void Start()
        {
            //scene.Load();
        }
    }
}

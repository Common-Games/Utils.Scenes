using UnityEngine;

namespace CGTK.Utils.Scenes.Samples.ScriptUsage.Samples.ScriptUsage
{
    public sealed class SceneBundleUsage : MonoBehaviour
    {
        [SerializeField] private SceneBundle scenes;
        
        private void Start()
        {
            scenes.Load();
        }
    }
}
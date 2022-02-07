using System;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.SceneManagement;
#endif

namespace CGTK.Utils.Scenes
{
    [Serializable]
    public sealed partial class SceneRef
    {
        #region Fields
        
        [field: SerializeField] public string Path  { get; private set; } = string.Empty;
        [field: SerializeField] public int    Index { get; private set; } = -1;
        #if UNITY_EDITOR
        [field: SerializeField] public SceneAsset Asset { get; private set; } = null;
        #endif
        
        #endregion

        #region Structors
        
        public SceneRef(string scenePath)
        {
            this.Path = scenePath;
        }
        public SceneRef(int sceneBuildIndex)
        {
            this.Index = sceneBuildIndex;
        }
        #if UNITY_EDITOR
        public SceneRef(SceneAsset sceneAsset)
        {
            this.Asset = sceneAsset;
        }
        #endif
        
        #endregion

        #region Operators

        public static implicit operator string (SceneRef reference) => reference.Path;
        public static implicit operator int    (SceneRef reference) => reference.Index;
        
        #endregion

        #region Methods

        public void Load(LoadSceneMode mode = LoadSceneMode.Single)
        {
            #if UNITY_EDITOR 
            SceneManager.LoadScene(sceneBuildIndex: Index, mode: mode);
            #endif
        }
        
        public void LoadAsync(LoadSceneMode mode = LoadSceneMode.Single)
        {
            SceneManager.LoadSceneAsync(sceneBuildIndex: Index, mode: mode);
        }

        #endregion

        #region Custom PropertyDrawer

        #if UNITY_EDITOR
        [CustomPropertyDrawer(type: typeof(SceneRef))]
        public sealed partial class SceneRefDrawer : PropertyDrawer
        {
            private const string _PATH  = "<" + nameof(Path)  + ">k__BackingField";
            private const string _INDEX = "<" + nameof(Index) + ">k__BackingField";
            private const string _ASSET = "<" + nameof(Asset) + ">k__BackingField";

            private SerializedProperty _scenePathProperty;
            private SerializedProperty _buildIndexProperty;
            private SerializedProperty _sceneAssetProperty;
            
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                _scenePathProperty  = property.FindPropertyRelative(relativePropertyPath: _PATH);
                _buildIndexProperty = property.FindPropertyRelative(relativePropertyPath: _INDEX);
                _sceneAssetProperty = property.FindPropertyRelative(relativePropertyPath: _ASSET);
                
                SceneAsset _currentSceneAsset = _sceneAssetProperty.objectReferenceValue as SceneAsset;
                _currentSceneAsset ??= AssetDatabase.LoadAssetAtPath<SceneAsset>(assetPath: _scenePathProperty.stringValue);
                
                EditorGUI.BeginChangeCheck();
                
                Rect _newPosition = new(x: position.x, y: position.y,
                    width: position.width,
                    height: EditorGUIUtility.singleLineHeight);
                
                SceneAsset _newSceneAsset = EditorGUI.ObjectField(
                        position: _newPosition, 
                        label: label,
                        obj: _currentSceneAsset,
                        objType: typeof(SceneAsset), 
                        allowSceneObjects: false) as SceneAsset;

                if (EditorGUI.EndChangeCheck())
                {
                    OnChange(newSceneAsset: _newSceneAsset);
                }
            }

            private void OnChange(SceneAsset newSceneAsset)
            {
                if (newSceneAsset is null)
                {
                    _scenePathProperty.stringValue = null;
                    _buildIndexProperty.intValue   = -1;
                    _sceneAssetProperty.objectReferenceValue = null;
                    return;
                }

                string _newPath = AssetDatabase.GetAssetPath(assetObject: newSceneAsset);
                
                Scene _newScene = SceneManager.GetSceneByPath(scenePath: _newPath);

                _scenePathProperty.stringValue = _newPath;
                _buildIndexProperty.intValue   = _newScene.IsValid() ? _newScene.buildIndex : -1;
                _sceneAssetProperty.objectReferenceValue = newSceneAsset;
                
                Debug.Log($"build index = {_newScene.buildIndex}");
                
                Debug.Log($"string  value = {_scenePathProperty.stringValue}");
                Debug.Log($"integer value = {_buildIndexProperty.intValue}");
            }

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                return base.GetPropertyHeight(property: property, label: label) + GUI.skin.box.CalcHeight(content: label, width: Screen.width);
            }
        }
        #endif
        
        #endregion
        
    }
}
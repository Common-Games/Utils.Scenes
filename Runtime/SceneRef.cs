using System;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.SceneManagement;
#endif

namespace CGTK.Utils.Scenes
{
    [Serializable]
    public sealed class SceneRef
    {
        [field: SerializeField] public string ScenePath  { get; private set; } = string.Empty;
        [field: SerializeField] public int BuildIndex    { get; private set; } = -1;
        #if UNITY_EDITOR
        [field: SerializeField] public SceneAsset Asset  { get; private set; } = null;
        #endif
        
        public SceneRef(string scenePath)
        {
            this.ScenePath = scenePath;
        }
        public SceneRef(int buildIndex)
        {
            this.BuildIndex = buildIndex;
        }
        #if UNITY_EDITOR
        public SceneRef(SceneAsset sceneAsset)
        {
            this.Asset = sceneAsset;
        }
        #endif
        
        public static implicit operator string (SceneRef reference) => reference.ScenePath;
        public static implicit operator int    (SceneRef reference) => reference.BuildIndex;

        #region Custom PropertyDrawer

        #if UNITY_EDITOR
        [CustomPropertyDrawer(type: typeof(SceneRef))]
        public class SceneRefDrawer : PropertyDrawer
        {
            private const string _SCENE_PATH  = "<" + nameof(ScenePath)  + ">k__BackingField";
            private const string _BUILD_INDEX = "<" + nameof(BuildIndex) + ">k__BackingField";
            private const string _SCENE_ASSET = "<" + nameof(Asset)      + ">k__BackingField";

            private SerializedProperty _scenePathProperty;
            private SerializedProperty _buildIndexProperty;
            private SerializedProperty _sceneAssetProperty;
            
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                _scenePathProperty  = property.FindPropertyRelative(relativePropertyPath: _SCENE_PATH);
                _buildIndexProperty = property.FindPropertyRelative(relativePropertyPath: _BUILD_INDEX);
                _sceneAssetProperty = property.FindPropertyRelative(relativePropertyPath: _SCENE_ASSET);
                
                SceneAsset _currentSceneAsset = _sceneAssetProperty.objectReferenceValue as SceneAsset;
                _currentSceneAsset ??= AssetDatabase.LoadAssetAtPath<SceneAsset>(assetPath: _scenePathProperty.stringValue);

                //SceneAsset _newSceneAsset;
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
                    OnChange(_newSceneAsset);
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
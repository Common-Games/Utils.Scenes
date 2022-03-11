using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.VersionControl;
#endif

using Object = UnityEngine.Object;

namespace CGTK.Utils.Scenes
{
    using static LoadMode;
    
    // Author: JohannesMP (2018-08-12)
    // Modified by: Walter H (2022-02-07)
    //
    // A wrapper that provides the means to safely serialize Scene Asset References.
    //
    // Internally we serialize an Object to the SceneAsset which only exists at editor time.
    // Any time the object is serialized, we store the path provided by this Asset (assuming it was valid).
    //
    // This means that, come build time, the string path of the scene asset is always already stored, which if 
    // the scene was added to the build settings means it can be loaded.
    //
    // It is up to the user to ensure the scene exists in the build settings so it is loadable at runtime.
    // To help with this, a custom PropertyDrawer displays the scene build settings state.
    //
    //  Known issues:
    // - When reverting back to a prefab which has the asset stored as null, Unity will show the property 
    // as modified despite having just reverted. This only happens on the fist time, and reverting again fix it. 
    // Under the hood the state is still always valid and serialized correctly regardless.
    
    /// <summary>
    /// A wrapper that provides the means to safely serialize Scene Asset References.
    /// </summary>
    [Serializable]
    public sealed partial class SceneReference : ISerializationCallbackReceiver
    {
        #region Fields

        #if UNITY_EDITOR
        // What we use in editor to select the scene
        [SerializeField] private Object sceneAsset;
        private bool IsValidSceneAsset
        {
            get
            {
                if (!sceneAsset) return false;

                return sceneAsset is SceneAsset;
            }
        }
        #endif

        // This should only ever be set during serialization/deserialization!
        [SerializeField]
        private string scenePath = string.Empty;

        // Use this when you want to actually have the scene path
        public string Path
        {
            get
            {
                #if UNITY_EDITOR
                // In editor we always use the asset's path
                return GetScenePathFromAsset();
                #else
                // At runtime we rely on the stored path value which we assume was serialized correctly at build time.
                // See OnBeforeSerialize and OnAfterDeserialize
                return scenePath;
                #endif
            }
            set
            {
                scenePath = value;
                #if UNITY_EDITOR
                sceneAsset = GetSceneAssetFromPath();
                #endif
            }
        }
        
        #endregion

        #region Structors
        
        public SceneReference(string scenePath)
        {
            this.Path = scenePath;
            this.sceneAsset = GetSceneAssetFromPath();
        }
        public SceneReference(int sceneBuildIndex)
        {
            this.Path = SceneUtility.GetScenePathByBuildIndex(sceneBuildIndex);
            this.sceneAsset = GetSceneAssetFromPath();
        }
        #if UNITY_EDITOR
        public SceneReference(SceneAsset sceneAsset)
        {
            this.sceneAsset = sceneAsset;
            this.Path = GetScenePathFromAsset();
        }
        
        #endif
        
        #endregion

        #region Operators

        public static implicit operator string(SceneReference reference) => reference.Path;
        public static implicit operator int   (SceneReference reference) => SceneUtility.GetBuildIndexByScenePath(reference.Path); //TODO: Cache

        public bool Equals(Scene scene)
        {	
            string _name = GetSceneName();
            return _name.Equals(scene.name);
        }

        public bool Equals(SceneReference scene) 
        {
            string _name = GetSceneName();
            return _name.Equals(scene.GetSceneName());
        }

        public bool Equals(string name) 
        {
            string _name = GetSceneName();
            return _name.Equals(name);
        }

        public string GetSceneName()
        {
            #if UNITY_EDITOR
            return (sceneAsset != null) ? sceneAsset.name : "NULL";
            #else
		    return Path.GetFileNameWithoutExtension(scenePath);
            #endif
        }

        #endregion

        #region Methods

        private Action<SceneReference> _loadAction; //TODO: Cache LoadAction.
        public void Load(LoadMode mode = Overwrite)
        {
            mode.GetLoadAction(action: out _loadAction);
            
            _loadAction.Invoke(obj: this);
        }
        
        public void LoadAsync(LoadSceneMode mode = LoadSceneMode.Single)
        {
            SceneManager.LoadSceneAsync(sceneName: Path, mode: mode);
        }
        
        // Called to prepare this data for serialization. Stubbed out when not in editor.
        public void OnBeforeSerialize()
        {
            #if UNITY_EDITOR
            if(IsValidSceneAsset)
            {
                scenePath = GetScenePathFromAsset();
            }
            else if(string.IsNullOrEmpty(scenePath) == false)
            {
                sceneAsset = GetSceneAssetFromPath();
                if (sceneAsset == null) scenePath = string.Empty;

                //why?
                //EditorSceneManager.MarkAllScenesDirty();
            }
            #endif
        }

        // Called to set up data for deserialization. Stubbed out when not in editor.
        public void OnAfterDeserialize()
        {
            #if UNITY_EDITOR
            EditorApplication.delayCall += HandleAfterDeserialize;
            #endif
        }
        
        private void HandleAfterDeserialize()
        {
            if (IsValidSceneAsset) return;

            // Asset is invalid but have path to try and recover from
            if (string.IsNullOrEmpty(scenePath)) return;

            sceneAsset = GetSceneAssetFromPath();
            if (sceneAsset == null) scenePath = string.Empty;

            if (!Application.isPlaying) EditorSceneManager.MarkAllScenesDirty();
        }

        #if UNITY_EDITOR
        private SceneAsset GetSceneAssetFromPath()
        {
            return string.IsNullOrEmpty(Path) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(assetPath: Path);
        }

        private string GetScenePathFromAsset()
        {
            return (sceneAsset == null) ? string.Empty : AssetDatabase.GetAssetPath(sceneAsset);
        }

        private void HandleBeforeSerialize()
        {
            // Asset is invalid but have Path to try and recover from
            if (IsValidSceneAsset == false && string.IsNullOrEmpty(scenePath) == false)
            {
                sceneAsset = GetSceneAssetFromPath();
                if (sceneAsset == null) scenePath = string.Empty;

                EditorSceneManager.MarkAllScenesDirty();
            }
            // Asset takes precendence and overwrites Path
            else
            {
                scenePath = GetScenePathFromAsset();
            }
        }
        #endif
        
        #endregion

        #region Custom Editor

        #if UNITY_EDITOR
        [CustomPropertyDrawer(type: typeof(SceneReference))]
        public class SceneReferencePropertyDrawer : PropertyDrawer
        {
            // The exact name of the asset Object variable in the SceneReference object
            private const string _SCENE_ASSET_PROPERTY_STRING = nameof(sceneAsset);
            // The exact name of the scene Path variable in the SceneReference object
            private const string _SCENE_PATH_PROPERTY_STRING  = nameof(scenePath);

            private SerializedProperty _sceneAssetProperty;
            private SerializedProperty _scenePathProperty;
            
            /*
            private static SerializedProperty GetSceneAssetProperty(SerializedProperty property)
            {
                return property.FindPropertyRelative(relativePropertyPath: _SCENE_ASSET_PROPERTY_STRING);
            }

            private static SerializedProperty GetScenePathProperty(SerializedProperty property)
            {
                return property.FindPropertyRelative(relativePropertyPath: _SCENE_PATH_PROPERTY_STRING);
            }
            */

            private static readonly RectOffset BoxPadding = EditorStyles.helpBox.padding;
            
            private const float _PAD_SIZE = 2f;

            private static readonly float LineHeight = EditorGUIUtility.singleLineHeight;
            private static readonly float PaddedLine = LineHeight + _PAD_SIZE;

            /// <summary>
            /// Drawing the 'SceneReference' property
            /// </summary>
            public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
            {
                _sceneAssetProperty = property.FindPropertyRelative(relativePropertyPath: _SCENE_ASSET_PROPERTY_STRING);
                _scenePathProperty  = property.FindPropertyRelative(relativePropertyPath: _SCENE_PATH_PROPERTY_STRING);

                // Move this up
                EditorGUI.BeginProperty(rect, GUIContent.none, property);
                {
                    // Draw the Box Background
                    GUI.Box(EditorGUI.IndentedRect(rect), GUIContent.none, EditorStyles.helpBox);
                    rect = BoxPadding.Remove(rect);
                    rect.height = LineHeight;

                    // Draw the main Object field
                    label.tooltip = "The actual Scene Asset reference.\n" +
                                    "On serialize this is also stored as the asset's path.";

                    int sceneControlID = GUIUtility.GetControlID(FocusType.Passive);
                    EditorGUI.BeginChangeCheck();
                    {
                        _sceneAssetProperty.objectReferenceValue = EditorGUI.ObjectField(rect, label, _sceneAssetProperty.objectReferenceValue, typeof(SceneAsset), allowSceneObjects: false);
                    }
                    BuildUtils.BuildScene buildScene = BuildUtils.GetBuildScene(_sceneAssetProperty.objectReferenceValue);
                    if (EditorGUI.EndChangeCheck())
                    {
                        // If no valid scene asset was selected, reset the stored path accordingly
                        if (buildScene.scene == null) _scenePathProperty.stringValue = String.Empty;
                    }

                    rect.y += PaddedLine;

                    if (!buildScene.assetGuid.Empty())
                    {
                        // Draw the Build Settings Info of the selected Scene
                        DrawSceneInfoGUI(rect, buildScene, sceneControlID: sceneControlID + 1);
                    }
                }
                EditorGUI.EndProperty();
            }

            /// <summary>
            /// Ensure that what we draw in OnGUI always has the room it needs
            /// </summary>
            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                _sceneAssetProperty = property.FindPropertyRelative(relativePropertyPath: _SCENE_ASSET_PROPERTY_STRING);

                int lines = _sceneAssetProperty.objectReferenceValue != null ? 2 : 1;
                
                return BoxPadding.vertical + LineHeight * lines + _PAD_SIZE * (lines - 1);
            }

            /// <summary>
            /// Draws info box of the provided scene
            /// </summary>
            private static void DrawSceneInfoGUI(Rect rect, BuildUtils.BuildScene buildScene, int sceneControlID)
            {
                bool readOnly = BuildUtils.IsReadOnly();
                string readOnlyWarning = readOnly ? "\n\nWARNING: Build Settings is not checked out and so cannot be modified." : "";

                // Label Prefix
                GUIContent iconContent = new GUIContent();
                GUIContent labelContent = new GUIContent();

                // Missing from build scenes
                if (buildScene.buildIndex == -1)
                {
                    iconContent = EditorGUIUtility.IconContent(name: "d_winbtn_mac_close");
                    labelContent.text = "NOT in build";
                    labelContent.tooltip = "This scene is NOT in build settings.\nIt will be NOT included in builds.";
                }
                // In build scenes and enabled
                else if (buildScene.scene.enabled)
                {
                    iconContent = EditorGUIUtility.IconContent(name: "d_winbtn_mac_max");
                    //labelContent.text = "BuildIndex: " + buildScene.buildIndex;
                    labelContent.text = "In build and enabled.";
                    labelContent.tooltip = "This scene is in build settings and ENABLED.\nIt will be included in builds." + readOnlyWarning;
                }
                // In build scenes and disabled
                else
                {
                    iconContent = EditorGUIUtility.IconContent(name: "d_winbtn_mac_min");
                    labelContent.text = "In build and disabled";
                    //labelContent.text = "BuildIndex: disabled"; //+ buildScene.buildIndex;
                    labelContent.tooltip = "This scene is in build settings and DISABLED.\nIt will be NOT included in builds.";
                }

                // Left status label
                using (new EditorGUI.DisabledScope(disabled: readOnly))
                {
                    Rect _labelRect = DrawUtils.GetLabelRect(position: rect);
                    Rect iconRect = _labelRect;
                    iconRect.width = iconContent.image.width + _PAD_SIZE;
                    _labelRect.width -= iconRect.width;
                    _labelRect.x += iconRect.width;
                    EditorGUI.PrefixLabel(totalPosition: iconRect, id: sceneControlID, label: iconContent);
                    EditorGUI.PrefixLabel(totalPosition: _labelRect, id: sceneControlID, label: labelContent);
                }

                // Right context buttons
                Rect buttonRect = DrawUtils.GetFieldRect(position: rect);
                buttonRect.width /= 4;

                string tooltipMsg;
                using (new EditorGUI.DisabledScope(disabled: readOnly))
                {
                    // NOT in build settings
                    if (buildScene.buildIndex == -1)
                    {
                        buttonRect.width *= 2;
                        int addIndex = EditorBuildSettings.scenes.Length;
                        tooltipMsg = "Add this scene to build settings. It will be appended to the end of the build scenes as buildIndex: " + addIndex + "." + readOnlyWarning;
                        if (DrawUtils.ButtonHelper(position: buttonRect, msgShort: "Add...", msgLong: "Add (buildIndex " + addIndex + ")", style: EditorStyles.miniButtonLeft, tooltip: tooltipMsg))
                        {
                            EditorApplication.delayCall += () => { BuildUtils.AddBuildScene(buildScene: buildScene); };
                        }
                        buttonRect.width /= 2;
                        buttonRect.x += buttonRect.width;
                    }
                    // In build settings
                    else
                    {
                        bool _isEnabled = buildScene.scene.enabled;
                        string stateString = _isEnabled ? "Disable" : "Enable";
                        tooltipMsg = stateString + " this scene in build settings.\n" +
                                     (_isEnabled ? "No longer be included in builds" : "Include in builds") + "." + readOnlyWarning;

                        if (DrawUtils.ButtonHelper(position: buttonRect, msgShort: stateString, msgLong: stateString, style: EditorStyles.miniButtonLeft, tooltip: tooltipMsg))
                        {
                            EditorApplication.delayCall += () => { BuildUtils.SetBuildSceneState(buildScene: buildScene, enabled: !_isEnabled); };
                        }
                        buttonRect.x += buttonRect.width;

                        tooltipMsg = "Completely remove this scene from build settings.\n" +
                                     "You will need to add it again for it to be included in builds!" + readOnlyWarning;
                        
                        if (DrawUtils.ButtonHelper(position: buttonRect, msgShort: "Remove...", msgLong: "Remove from Build", style: EditorStyles.miniButtonMid, tooltip: tooltipMsg))
                        {
                            EditorApplication.delayCall += () => { BuildUtils.RemoveBuildScene(buildScene: buildScene); };
                        }

                    }
                }

                buttonRect.x += buttonRect.width;

                tooltipMsg = "Loads the scene." + readOnlyWarning;
                if (DrawUtils.ButtonHelper(position: buttonRect, msgShort: "Load", msgLong: "Load", style: EditorStyles.miniButtonRight, tooltip: tooltipMsg))
                {
                    #if UNITY_EDITOR
                    if (Application.isPlaying)
                    {
                        SceneManager.LoadScene(buildScene.assetPath);
                    }
                    else
                    {
                        EditorSceneManager.OpenScene(buildScene.assetPath);
                    }
                    #else
                    SceneManager.LoadScene(buildScene.assetPath);
                    #endif
                }
                
                buttonRect.x += buttonRect.width;

                tooltipMsg = "Open the 'Build Settings' Window for managing scenes." + readOnlyWarning;
                if (DrawUtils.ButtonHelper(position: buttonRect, msgShort: "Settings", msgLong: "Build Settings", style: EditorStyles.miniButtonRight, tooltip: tooltipMsg))
                {
                    BuildUtils.OpenBuildSettings();
                }

            }

            private static class DrawUtils
            {
                /// <summary>
                /// Draw a GUI button, choosing between a short and a long button text based on if it fits
                /// </summary>
                public static bool ButtonHelper(Rect position, string msgShort, string msgLong, GUIStyle style, string tooltip = null)
                {
                    GUIContent content = new GUIContent(msgLong) { tooltip = tooltip };

                    float longWidth = style.CalcSize(content).x;
                    if (longWidth > position.width) content.text = msgShort;

                    return GUI.Button(position, content, style);
                }

                /// <summary>
                /// Given a position rect, get its field portion
                /// </summary>
                public static Rect GetFieldRect(Rect position)
                {
                    position.width -= EditorGUIUtility.labelWidth;
                    position.x += EditorGUIUtility.labelWidth;
                    return position;
                }
                /// <summary>
                /// Given a position rect, get its label portion
                /// </summary>
                public static Rect GetLabelRect(Rect position)
                {
                    position.width = EditorGUIUtility.labelWidth - _PAD_SIZE;
                    return position;
                }
            }

            /// <summary>
            /// Various BuildSettings interactions
            /// </summary>
            private static class BuildUtils
            {
                // time in seconds that we have to wait before we query again when IsReadOnly() is called.
                public const float MIN_CHECK_WAIT = 3;

                private static float _lastTimeChecked;
                private static bool  _cachedReadonlyVal = true;

                /// <summary>
                /// A small container for tracking scene data BuildSettings
                /// </summary>
                public struct BuildScene
                {
                    public int buildIndex;
                    public GUID assetGuid;
                    public string assetPath;
                    public EditorBuildSettingsScene scene;
                }

                /// <summary>
                /// Check if the build settings asset is readonly.
                /// Caches value and only queries state a max of every 'minCheckWait' seconds.
                /// </summary>
                public static bool IsReadOnly()
                {
                    float _curTime = Time.realtimeSinceStartup;
                    float _timeSinceLastCheck = _curTime - _lastTimeChecked;

                    if (!(_timeSinceLastCheck > MIN_CHECK_WAIT)) return _cachedReadonlyVal;

                    _lastTimeChecked = _curTime;
                    _cachedReadonlyVal = QueryBuildSettingsStatus();

                    return _cachedReadonlyVal;
                }

                /// <summary>
                /// A blocking call to the Version Control system to see if the build settings asset is readonly.
                /// Use BuildSettingsIsReadOnly for version that caches the value for better responsivenes.
                /// </summary>
                private static bool QueryBuildSettingsStatus()
                {
                    // If no version control provider, assume not readonly
                    if (!Provider.enabled) return false;

                    // If we cannot checkout, then assume we are not readonly
                    if (!Provider.hasCheckoutSupport) return false;

                    //// If offline (and are using a version control provider that requires checkout) we cannot edit.
                    //if (UnityEditor.VersionControl.Provider.onlineState == UnityEditor.VersionControl.OnlineState.Offline)
                    //    return true;

                    // Try to get status for file
                    Task _status = Provider.Status("ProjectSettings/EditorBuildSettings.asset", false);
                    _status.Wait();

                    // If no status listed we can edit
                    //#if UNITY_2022_
                    //if (_status.assetList is not {Count: 1}) return true;
                    if (_status.assetList == null || _status.assetList.Count != 1) return true;

                    // If is checked out, we can edit
                    return !_status.assetList[index: 0].IsState(Asset.States.CheckedOutLocal);
                }

                /// <summary>
                /// For a given Scene Asset object reference, extract its build settings data, including buildIndex.
                /// </summary>
                public static BuildScene GetBuildScene(Object sceneObject)
                {
                    BuildScene _entry = new BuildScene
                    {
                        buildIndex = -1,
                        assetGuid = new GUID(string.Empty)
                    };

                    if (sceneObject as SceneAsset == null) return _entry;

                    _entry.assetPath = AssetDatabase.GetAssetPath(sceneObject);
                    _entry.assetGuid = new GUID(AssetDatabase.AssetPathToGUID(_entry.assetPath));

                    EditorBuildSettingsScene[] _scenes = EditorBuildSettings.scenes;
                    for (int _index = 0; _index < _scenes.Length; ++_index)
                    {
                        if (!_entry.assetGuid.Equals(_scenes[_index].guid)) continue;

                        _entry.scene = _scenes[_index];
                        _entry.buildIndex = _index;
                        return _entry;
                    }

                    return _entry;
                }

                /// <summary>
                /// Enable/Disable a given scene in the buildSettings
                /// </summary>
                public static void SetBuildSceneState(BuildScene buildScene, bool enabled)
                {
                    bool _modified = false;
                    EditorBuildSettingsScene[] _scenesToModify = EditorBuildSettings.scenes;
                    foreach (EditorBuildSettingsScene _curScene in _scenesToModify)
                    {
                        if (_curScene.guid.Equals(buildScene.assetGuid))
                        {
                            _curScene.enabled = enabled;
                            _modified = true;
                            break;
                        }
                    }

                    if (_modified) EditorBuildSettings.scenes = _scenesToModify;
                }

                /// <summary>
                /// Display Dialog to add a scene to build settings
                /// </summary>
                public static void AddBuildScene(BuildScene buildScene, bool force = false, bool enabled = true)
                {
                    if (force == false)
                    {
                        int _selection = EditorUtility.DisplayDialogComplex(
                            title: "Add Scene To Build",
                            message: "You are about to add scene at " + buildScene.assetPath + " To the Build Settings.",
                            ok: "Add as Enabled",       // option 0
                            cancel: "Add as Disabled",      // option 1
                            alt: "Cancel (do nothing)"); // option 2

                        switch (_selection)
                        {
                            case 0: // enabled
                                enabled = true;
                                break;
                            case 1: // disabled
                                enabled = false;
                                break;
                            default:
                                //case 2: // cancel
                                return;
                        }
                    }

                    EditorBuildSettingsScene _newScene = new EditorBuildSettingsScene(buildScene.assetGuid, enabled);
                    List<EditorBuildSettingsScene> _tempScenes = EditorBuildSettings.scenes.ToList();
                    _tempScenes.Add(_newScene);
                    EditorBuildSettings.scenes = _tempScenes.ToArray();
                }

                /// <summary>
                /// Display Dialog to remove a scene from build settings (or just disable it)
                /// </summary>
                public static void RemoveBuildScene(BuildScene buildScene, bool force = false)
                {
                    bool _onlyDisable = false;
                    if (force == false)
                    {
                        int _selection = -1;

                        const string _TITLE   =  "Remove Scene From Build";
                        
                              string _details =  "You are about to remove the following scene from build settings:\n" +
                                                $"    {buildScene.assetPath}\n" +
                                                $"    buildIndex: {buildScene.buildIndex}\n\n" +
                                                "This will modify build settings, but the scene asset will remain untouched.";
                              
                        const string _confirm = "Remove From Build";
                        const string _alt = "Just Disable";
                        const string _cancel = "Cancel (do nothing)";

                        if (buildScene.scene.enabled)
                        {
                            _details += "\n\nIf you want, you can also just disable it instead.";
                            _selection = EditorUtility.DisplayDialogComplex(_TITLE, _details, _confirm, _alt, _cancel);
                        }
                        else
                        {
                            _selection = EditorUtility.DisplayDialog(_TITLE, _details, _confirm, _cancel) ? 0 : 2;
                        }

                        switch (_selection)
                        {
                            case 0: // remove
                                break;
                            case 1: // disable
                                _onlyDisable = true;
                                break;
                            default:
                                //case 2: // cancel
                                return;
                        }
                    }

                    // User chose to not remove, only disable the scene
                    if (_onlyDisable)
                    {
                        SetBuildSceneState(buildScene, enabled: false);
                    }
                    // User chose to fully remove the scene from build settings
                    else
                    {
                        List<EditorBuildSettingsScene> _tempScenes = EditorBuildSettings.scenes.ToList();
                        _tempScenes.RemoveAll(scene => scene.guid.Equals(buildScene.assetGuid));
                        EditorBuildSettings.scenes = _tempScenes.ToArray();
                    }
                }

                /// <summary>
                /// Open the default Unity Build Settings window
                /// </summary>
                public static void OpenBuildSettings()
                {
                    EditorWindow.GetWindow(typeof(BuildPlayerWindow));
                }
            }
        }
        #endif   

        #endregion
    }
}
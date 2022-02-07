using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEngine.SceneManagement;

using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif
   
using JetBrains.Annotations;

using static CGTK.Utils.Scenes.LoadMode;

namespace CGTK.Utils.Scenes
{
	//#if ODIN_INSPECTOR
	//[InlineEditor]
	//#endif
	[CreateAssetMenu(order = 200)]
	public sealed partial class SceneBundle : ScriptableObject
	{
		#region Custom Types

		[Serializable]
		public partial struct SceneInfo
		{
			public SceneRef scene;
			public bool loadScene;

			public SceneInfo(SceneRef scene = null, bool loadScene = true)
			{
				this.scene     = scene;
				this.loadScene = loadScene;
			}

			public static implicit operator SceneRef(SceneInfo sceneInfo) => sceneInfo.scene;
			
			#region Custom Property Drawer
			
			#if UNITY_EDITOR
			[CustomPropertyDrawer(type: typeof(SceneInfo))]
			private sealed class SceneInfoDrawer : PropertyDrawer
			{
				private const string _LOAD  = nameof(loadScene);
				private const string _SCENE = nameof(scene);

				public override VisualElement CreatePropertyGUI(SerializedProperty property)
				{
					VisualElement _container = new();
					
					PropertyField _loadField  = new(property: property.FindPropertyRelative(relativePropertyPath: _LOAD),  label: "");
					PropertyField _sceneField = new(property: property.FindPropertyRelative(relativePropertyPath: _SCENE), label: "");
					
					_container.Add(child: _loadField);
					_container.Add(child: _sceneField);

					return _container;
				}
			}
			#endif
			
			#endregion
		}

		#endregion

		#region Fields
		
		[field: SerializeField] 
		public SceneInfo[] Scenes { get; [UsedImplicitly] private set; } = Array.Empty<SceneInfo>();

		#endregion

		#region Methods

		public void Load(LoadMode mode = Overwrite)
		{
			bool _isFirstScene = true;
			foreach (SceneInfo _sceneInfo in Scenes)
			{
				LoadMode _loadMode;
				
				if (_isFirstScene && _sceneInfo.loadScene) //will ignore the first scene completely if it's not set as loadScene 
				{
					_loadMode = mode;
					_isFirstScene = false;
				}
				else
				{
					_loadMode = (_sceneInfo.loadScene) ? Additive : AdditiveWithoutLoading;
				}

				_sceneInfo.scene.Load(mode: _loadMode);
			}
		}

		#endregion

		#region Custom Editor

		[CustomEditor(inspectedType: typeof(SceneBundle))]
		private sealed class SceneBundleEditor : Editor 
		{
			public override VisualElement CreateInspectorGUI()
			{
				VisualElement _container = new();

				InspectorElement.FillDefaultInspector(container: _container, serializedObject: serializedObject, editor: this);

				return _container;
			}

			[OnOpenAsset(callbackOrder: 1)]
			private static bool OnOpenAsset(int id, int line) 
			{
				Object _obj = EditorUtility.InstanceIDToObject(instanceID: id);

				if (_obj is not SceneBundle _bundle) return false;
				
				_bundle.Load();
				return true;
			}
		}

		#endregion
	}
}
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

/*
#if ODIN_INSPECTOR
using ScriptableObject = Sirenix.OdinInspector.SerializedScriptableObject;
#else
using ScriptableObject = UnityEngine.ScriptableObject;
#endif
*/

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
			public SceneReference scene;
			public bool load;

			public SceneInfo(SceneReference scene = null, bool load = true)
			{
				this.scene     = scene;
				this.load = load;
			}

			public static implicit operator SceneReference(SceneInfo sceneInfo) => sceneInfo.scene;
			
			/*
			#region Custom Property Drawer
			
			#if UNITY_EDITOR
			[CustomPropertyDrawer(type: typeof(SceneInfo))]
			private sealed class SceneInfoDrawer : PropertyDrawer
			{
				private const string _LOAD  = nameof(load);
				private const string _SCENE = nameof(scene);

				public override VisualElement CreatePropertyGUI(SerializedProperty property)
				{
					VisualElement _container = new VisualElement();
					
					PropertyField _loadField  = new(property: property.FindPropertyRelative(relativePropertyPath: _LOAD),  label: "");
					PropertyField _sceneField = new(property: property.FindPropertyRelative(relativePropertyPath: _SCENE), label: "");
					
					_container.Add(child: _loadField);
					_container.Add(child: _sceneField);

					return _container;
				}
			}
			#endif

			#endregion
			*/
		}

		#endregion

		#region Fields
		
		[field: SerializeField] 
		public SceneInfo[] Scenes { get; [UsedImplicitly] private set; } = Array.Empty<SceneInfo>();

		#endregion

		#region Methods

		//TODO: Load Async
		public void Load(LoadMode mode = Overwrite)
		{
			bool _isFirstScene = true;
			foreach (SceneInfo _sceneInfo in Scenes)
			{
				LoadMode _loadMode;
				
				if (_isFirstScene && _sceneInfo.load) //will ignore the first scene completely if it's not set as loadScene 
				{
					_loadMode = mode;
					_isFirstScene = false;
				}
				else
				{
					_loadMode = (_sceneInfo.load) ? Additive : AdditiveWithoutLoading;
				}

				_sceneInfo.scene.Load(mode: _loadMode);
			}
		}

		#endregion
		
		#region Custom Editor

		#if UNITY_EDITOR
		[OnOpenAsset(callbackOrder: 1)]
		private static bool OnOpenAsset(int id, int line) 
		{
			Object _obj = EditorUtility.InstanceIDToObject(instanceID: id);

			//if (_obj is not SceneBundle _bundle) return false;
			
			if (_obj is SceneBundle _bundle)
			{
				_bundle.Load();
				return true;
			}

			return false;
		}
		
		/*
		[CustomEditor(inspectedType: typeof(SceneBundle))]
		private sealed class SceneBundleEditor : Editor 
		{
			public override VisualElement CreateInspectorGUI()
			{
				VisualElement _container = new VisualElement();

				InspectorElement.FillDefaultInspector(container: _container, serializedObject: serializedObject, editor: this);

				return _container;
			}
		}
		*/
		#endif

		#endregion
	}
}
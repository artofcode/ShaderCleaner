using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace artofcode.Cleaner
{

	public class ShaderCleaner: EditorWindow
	{

		// Structs

		private struct TextureProp
		{
			public string					propertyName;
			public string					displayName;
			public Texture					texture;
			public string					assetBundleName;
			//public TextureDimension			dimension;
		}

		struct ShaderDefaultMap
		{
			public string					assetBundleName;
			public Shader					shader;
			public List<TextureProp>		properties;

			public bool HasMismatchAssetBundle()
			{
				foreach (var prop in properties)
				{
					if (prop.assetBundleName != assetBundleName)
					{
						return true;
					}
				}

				return false;
			}
		}

		//

		private Vector2						m_scrollPosition;
		private bool						m_onlyShowMismatchedAssetBundles;
		private List<ShaderDefaultMap>		m_shadersWithDefaultMap = new List<ShaderDefaultMap>();

		//

		[MenuItem("Art of Code/Shader Cleaner")]
		private static void Init()
		{
			var shaderCleaner = GetWindow<ShaderCleaner>("Shader Cleaner");
			shaderCleaner.UpdateUI();
		}

		protected virtual void OnGUI()
		{
			EditorGUILayout.Space();
			EditorGUILayout.HelpBox("Shaders with default maps (and the asset bundle that they're in)", MessageType.Info);
			EditorGUI.BeginChangeCheck();

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Only show entries with mismatched asset bundles");
			m_onlyShowMismatchedAssetBundles = EditorGUILayout.Toggle(m_onlyShowMismatchedAssetBundles);
			EditorGUILayout.EndHorizontal();
			if (EditorGUI.EndChangeCheck())
			{
				Repaint();
			}
			
			EditorGUILayout.Space();

			m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);
			{
				foreach (var shaderDefaultMap in m_shadersWithDefaultMap)
				{
					if (!m_onlyShowMismatchedAssetBundles || shaderDefaultMap.HasMismatchAssetBundle())
					{
						ShowShaderWithProperties(shaderDefaultMap);
					}
				}
			}
			EditorGUILayout.EndScrollView();

			if (GUILayout.Button("Refresh", GUILayout.Width(80f)))
			{
				UpdateUI();
			}
		}

		private void ShowShaderWithProperties(ShaderDefaultMap shaderDefaultMap)
		{
			GUIStyle oldReferenceStyle = "CN StatusWarn";
			//EditorGUILayout.LabelField(shaderDefaultMap.shader.name + " (" + shaderDefaultMap.shaderAssetBundleName + ")");
			GUILayout.BeginHorizontal();
			EditorGUILayout.ObjectField("Shader", shaderDefaultMap.shader, typeof(Shader), false);
			EditorGUILayout.LabelField(shaderDefaultMap.assetBundleName);
			GUILayout.EndHorizontal();
			EditorGUI.indentLevel++;
			{
				foreach (var prop in shaderDefaultMap.properties)
				{
					//EditorGUILayout.LabelField(prop.propertyName, prop.texture.name + " (" + prop.textureAssetBundleName + ")");
					GUILayout.BeginHorizontal();
					EditorGUILayout.ObjectField("Texture", prop.texture, typeof(Texture), false, GUILayout.MaxHeight(16f));
					if (prop.assetBundleName != shaderDefaultMap.assetBundleName)
					{
						EditorGUILayout.LabelField(prop.assetBundleName, oldReferenceStyle);
					}
					else
					{
						EditorGUILayout.LabelField(prop.assetBundleName);
					}

					GUILayout.EndHorizontal();
				}
			}
			EditorGUI.indentLevel--;
			EditorGUILayout.Space();
		}

		private void UpdateUI()
		{
			UpdateShaderMap();
			Repaint();
		}

		private void UpdateShaderMap()
		{
			int shadersWithDefaultMapCount = 0;
			m_shadersWithDefaultMap.Clear();

			StringBuilder report = new StringBuilder();

			string[] assetGUIDs = AssetDatabase.FindAssets("t:shader");
			Debug.Log("Found shaders: " + assetGUIDs.Length);
			foreach (var assetGUID in assetGUIDs)
			{
				var assetPath = AssetDatabase.GUIDToAssetPath(assetGUID);
				var importer = AssetImporter.GetAtPath(assetPath);
				ShaderImporter shaderImporter = importer as ShaderImporter;

				List<TextureProp> properties = new List<TextureProp>();
				GetProperties(ref properties, shaderImporter);
				StringBuilder sb = new StringBuilder();
				sb.AppendLine("Shader: " + assetPath + " (bundle: " + importer.assetBundleName + ")");
				bool hasDefaultMap = false;

				ShaderDefaultMap sdm = new ShaderDefaultMap();
				sdm.shader = shaderImporter.GetShader();
				sdm.assetBundleName = importer.assetBundleName;
				sdm.properties = new List<TextureProp>();

				// Walk through shader's textures and see if it's in the same asset bundle
				for (int i = 0; i < properties.Count; ++i)
				{
					var prop = properties[i];
					if (prop.texture != null)
					{
						sdm.properties.Add(prop);
						hasDefaultMap = true;
						sb.AppendLine("\t" + prop.propertyName + ", " + prop.texture.name + " (bundle: " + prop.assetBundleName + ")");
					}
				}

				if (hasDefaultMap)
				{
					m_shadersWithDefaultMap.Add(sdm);
					report.AppendLine(sb.ToString());
					++shadersWithDefaultMapCount;
				}
			}

			//Debug.Log(report.ToString());
			//Debug.LogWarning("Shaders with Default Map: " + shadersWithDefaultMapCount);
		}

		private static void GetProperties(ref List<TextureProp> m_Properties, ShaderImporter importer)
		{
			var shader = importer.GetShader();
			var propertyCount = ShaderUtil.GetPropertyCount(shader);

			for (var i = 0; i < propertyCount; i++)
			{
				if (ShaderUtil.GetPropertyType(shader, i) != ShaderUtil.ShaderPropertyType.TexEnv)
					continue;

				var propertyName = ShaderUtil.GetPropertyName(shader, i);
				var displayName = ShaderUtil.GetPropertyDescription(shader, i);  // might be empty
				var texture = importer.GetDefaultTexture(propertyName);

				var assetBundleName = "";
				if (texture != null)
				{
					var textureAssetPath = AssetDatabase.GetAssetPath(texture);
					assetBundleName = AssetDatabase.GetImplicitAssetBundleName(textureAssetPath);
				}
			
				var temp = new TextureProp
				{
					propertyName = propertyName,
					displayName = displayName,
					texture = texture,
					assetBundleName = assetBundleName,
					//dimension = ShaderUtil.GetTexDim(shader, i)
				};
				m_Properties.Add(temp);
			}
		}

	}

}

#if MF_MAT_PROCESSOR
using System;
using System.IO;
using Moonflow.MFAssetTools.MFMatProcessor.EnternalConfigs;
using UnityEditor;
using UnityEngine;

namespace Moonflow.MFAssetTools.MFAssetImporter
{
    public class MFModelImportMatRuleConfigs : ScriptableObject
    {
        private static string _configPathFolder = "EditorConfigs"; 
        private static string _configPath = $"{_configPathFolder}/ModelImportMaterialRuleConfigs.asset";
        private static string _configPathNoExt = $"{_configPathFolder}/ModelImportMaterialRuleConfigs";
        
        [Serializable]
        public struct MatDeliverConfigPair
        {
            public string name;
            public MFMatSetterConfig config;
        }
        [SerializeField]public MatDeliverConfigPair[] matSetterConfigPairs;
        
        [MenuItem("Moonflow/Tools/ModelImportMatRuleConfigs")]
        public static void CreateConfigs()
        {
            MFModelImportMatRuleConfigs config = CreateInstance(typeof(MFModelImportMatRuleConfigs)) as MFModelImportMatRuleConfigs;
            if (!AssetDatabase.IsValidFolder($"Assets/Resources\\{_configPathFolder}"))
            {
                AssetDatabase.CreateFolder("Assets/Resources",_configPathFolder);
                AssetDatabase.Refresh();
            }
            AssetDatabase.CreateAsset(config, $"Assets/Resources/{_configPath}");
        }

        public static MFModelImportMatRuleConfigs LoadConfigs()
        {
            try
            {
                return Resources.Load<MFModelImportMatRuleConfigs>(_configPathNoExt);
            }
            catch (Exception e)
            {
                Debug.LogError($"路径Resources/{_configPathNoExt}配置不存在");    
            }
            return null;
        }

        public string[] GetTypeList()
        {
            if (matSetterConfigPairs == null || matSetterConfigPairs.Length == 0)
            {
                Debug.LogError("配置为空");
                return null;
            }
            string[] shownNames = new string[matSetterConfigPairs.Length];
            for (int i = 0; i < shownNames.Length; i++)
            {
                shownNames[i] = matSetterConfigPairs[i].name;
            }

            return shownNames;
        }

        public void DoDeliver(Material mat, int i)
        {
            foreach (var matSetter in matSetterConfigPairs[i].config.setters)
            {
                matSetter.SetData(mat);
            }
        }
    }
}
#endif
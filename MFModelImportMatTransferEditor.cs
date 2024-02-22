#if MF_MAT_PROCESSOR
using System;
using UnityEditor;
using UnityEngine;

namespace Moonflow.MFAssetTools.MFAssetImporter
{
    public class MFModelImportMatTransferEditor : EditorWindow
    {
        public Material[] mats;
        public int[] transferTypeIndexes;
        
        private string[] _transferTypes;
        private bool _isInitialized = false;
        private Vector2 scrollPos;
        private MFModelImportMatRuleConfigs _ruleConfigs;

        // public delegate void MatTransferCallback();

        // public static MatTransferCallback FinishTransfer;
        public static MFModelImportMatTransferEditor ShowWindow()
        {
            var _ins = GetWindow<MFModelImportMatTransferEditor>("Material Transfer");
            _ins.minSize = new Vector2(400, 700);
            _ins.maxSize = new Vector2(400, 700);
            return _ins;
        }

        private void OnDisable()
        {
            // FinishTransfer();
        }

        public void InitializeData(Material[] materials)
        {
            mats = materials;
            transferTypeIndexes = new int[mats.Length];
            for (var index = 0; index < transferTypeIndexes.Length; index++)
            {
                transferTypeIndexes[index]= 0;
            }
            _ruleConfigs = MFModelImportMatRuleConfigs.LoadConfigs();
            _transferTypes = _ruleConfigs.GetTypeList();

            _isInitialized = true;
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Do Transfer"))
                {
                    DoTransfer();
                    this.Close();
                }
                if (GUILayout.Button("Skip"))
                {
                    this.Close();
                }
            }
            using (var scope = new EditorGUILayout.ScrollViewScope(scrollPos, false, false))
            {
                scrollPos = scope.scrollPosition;
                using (new EditorGUILayout.VerticalScope())
                {
                    for (int i = 0; i < mats.Length; i++)
                    {
                        using (new EditorGUILayout.HorizontalScope("box"))
                        {
                            EditorGUILayout.ObjectField(mats[i], typeof(Material));
                            transferTypeIndexes[i] = EditorGUILayout.Popup(transferTypeIndexes[i], _transferTypes);
                        }
                    }
                }
            }
        }

        private void DoTransfer()
        {
            for (int i = 0; i < mats.Length; i++)
            {
                var mat = mats[i];
                var setters = _ruleConfigs.matSetterConfigPairs[transferTypeIndexes[i]].config.setters;
                for (int j = 0; j < setters.Length; j++)
                {
                    setters[j].SetData(mat);
                }
            }
        }
    }
}
#endif
# ReadMe
This tool is used to extract assets from .fbx files or other type of model files which can be recognized by Unity.
Most of time it can be run without any reference, so there are two parts of functions.

### Standalone Function
The tool override the editor of ModelImporter。After you import a model file to your unity project, choose to see the inspector view of this asset, you will see extra functions made by this tool below the __"Asset PostProcessors"__ view.
There are two parts on shown:
1. Common
    * __Select part to Extract/Copy__: Nothing / Everything / Material / Animation / Avatar / Mesh
    * __Extract / Copy__
         Both are to move sub assets out of original model file on Project view, but with some difference
      * Extract: Sub assets will seemed be removed, and the reference link will linked to the new assets created out of model asset. The meta file of model file will be changed.__Caution that skinned mesh can not extract meshes out of model file__
      * Copy: It just create a copy of sub assets and saved out of model file. The meta file of model file will not be changed, reference link will not be relinked to new assets either.

2. Cel Character

### Extra Function
##### Need "Moonflow Material Processor" in the project
The extra function is used to change material settings when extract/copy materials out of model file.
It aimed to help artists see the render result of models in current project faster.
#### Guide for artist - Use the correct config to deliver material properties.
  * When extract/copy materials from model file, there will opened another editor window called _Material Transfer_. It will show all the materials as a list.
  * Every material binded with a _Transfer Rule_, you can see on every right of the material. The display name is edited by technical artist, which may summarize the real funcion of the rule. Like "Lit to CelBase" means material will be transferred from shader "Lit" to "CelBase", and the original properties will be transferred to fit the new shader.
  * Artists can choose the correct rule to transfer material properties. As we know, one complex model commonly need serveral different type of shaders to show the complex display sense.
  * Click "Do Transfer" to apply the transfer, or just click "Skip" to skip this step.
#### Guide for technical artist - Make preset to help artist choose the correct rule.
  * On Menu bar, Click _Moonflow-Tools-ModelImportMatRuleConfigs_ to Create material transfer rules config asset. 
  * The default saving path is _Resources/EditorConfigs/ModelImportMaterialRuleConfigs.asset_, you can change the path settings on _MFModelImportMatRuleConfigs.cs_
  * In the Config, every rules has a name setting and a MFMatSetterConfig typed asset reference. The name in this asset will show artists to understand what will this rule do.
  * How to Create MFMatSetterConfig and how to add rules please see the readme in _Moonflow Material Processor_

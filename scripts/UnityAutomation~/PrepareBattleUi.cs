using UnityEditor;
public static class PrepareBattleUi { public static object Run(){TMPro.TMP_PackageResourceImporter.ImportResources(true,false,false);AssetDatabase.Refresh();return "TMP resources import requested";} }

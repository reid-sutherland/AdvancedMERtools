using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Handles importing AMERT components from the compiled objects folder. By icedchai
/// </summary>
public static class AmertImporter
{
    [MenuItem("SchematicManager/Decompile All", priority = 1)]
    public static void DecompileAll()
    {
        DecompileCustomCollider();
        DecompileFuncExecutor();
        DecompileGroovyNoise();
        DecompileHealthSchematics();
        DecompileInteractableObjects();
        DecompileInteractablePickups();
    }

    [MenuItem("SchematicManager/Decompile Custom Colliders", priority = 1)]
    public static void DecompileCustomCollider()
    {
        foreach (Schematic schematic in GameObject.FindObjectsOfType<Schematic>())
        {
            List<CCDTO> datas = Decompile<CCDTO>(schematic, "Colliders");
            List<FCCDTO> fdatas = Decompile<FCCDTO>(schematic, "FColliders");

            foreach (var data in datas)
            {
                Transform target = FindObjectWithPath(schematic.transform, data.ObjectId);

                CustomCollider targetComponent = target.AddComponent<CustomCollider>();

                targetComponent.ScriptGroup = data.ScriptGroup;
                targetComponent.data = data;
                targetComponent.data.AnimationModules.ForEach(a => a.Animator = FindObjectWithPath(schematic.transform, a.AnimatorAdress).gameObject);
                targetComponent.UseScriptValue = false;
            }

            foreach (var data in fdatas)
            {
                Transform target = FindObjectWithPath(schematic.transform, data.ObjectId);

                CustomCollider targetComponent = target.AddComponent<CustomCollider>();

                targetComponent.ScriptGroup = data.ScriptGroup;
                targetComponent.ScriptValueData = data;
                targetComponent.data.AnimationModules.ForEach(a => a.Animator = FindObjectWithPath(schematic.transform, a.AnimatorAdress).gameObject);
                targetComponent.UseScriptValue = true;
            }
        }
    }

    [MenuItem("SchematicManager/Decompile Function Executor", priority = 1)]
    public static void DecompileFuncExecutor()
    {
        foreach (Schematic schematic in GameObject.FindObjectsOfType<Schematic>())
        {
            List<FEDTO> datas = Decompile<FEDTO>(schematic, "Functions");

            foreach (var data in datas)
            {
                schematic.AddComponent<FunctionExecutor>().data = data;
            }
        }
    }

    [MenuItem("SchematicManager/Decompile Groovy Noise", priority = 1)]
    public static void DecompileGroovyNoise()
    {
        foreach (Schematic schematic in GameObject.FindObjectsOfType<Schematic>())
        {
            List<GNDTO> datas = Decompile<GNDTO>(schematic, "GroovyNoises");

            foreach (var data in datas)
            {
                Transform target = FindObjectWithPath(schematic.transform, data.ObjectId);

                GroovyNoise targetComponent = target.AddComponent<GroovyNoise>();

                targetComponent.ScriptGroup = data.ScriptGroup;
                targetComponent.data = data;
                targetComponent.UseScriptValue = false;
            }
        }
    }

    [MenuItem("SchematicManager/Decompile Health Schematics", priority = 1)]
    public static void DecompileHealthSchematics()
    {
        foreach (Schematic schematic in GameObject.FindObjectsOfType<Schematic>())
        {
            List<HODTO> datas = Decompile<HODTO>(schematic, "HealthObjects");
            List<FHODTO> fdatas = Decompile<FHODTO>(schematic, "FHealthObjects");

            foreach (var data in datas)
            {
                Transform target = FindObjectWithPath(schematic.transform, data.ObjectId);

                HealthObject targetComponent = target.AddComponent<HealthObject>();

                targetComponent.ScriptGroup = data.ScriptGroup;
                targetComponent.data = data;
                targetComponent.data.AnimationModules.ForEach(a => a.Animator = FindObjectWithPath(schematic.transform, a.AnimatorAdress).gameObject);
                targetComponent.UseScriptValue = false;
            }

            foreach (var data in fdatas)
            {
                Transform target = FindObjectWithPath(schematic.transform, data.ObjectId);

                HealthObject targetComponent = target.AddComponent<HealthObject>();

                targetComponent.ScriptGroup = data.ScriptGroup;
                targetComponent.ScriptValueData = data;
                targetComponent.data.AnimationModules.ForEach(a => a.Animator = FindObjectWithPath(schematic.transform, a.AnimatorAdress).gameObject);
                targetComponent.UseScriptValue = true;
            }
        }
    }

    [MenuItem("SchematicManager/Decompile Interactable Objects", priority = 1)]
    public static void DecompileInteractableObjects()
    {
        foreach (Schematic schematic in GameObject.FindObjectsOfType<Schematic>())
        {
            List<IODTO> datas = Decompile<IODTO>(schematic, "Objects");
            List<FIODTO> fdatas = Decompile<FIODTO>(schematic, "FObjects");

            foreach (var data in datas)
            {
                Transform target = FindObjectWithPath(schematic.transform, data.ObjectId);

                InteractableObject targetComponent = target.AddComponent<InteractableObject>();

                targetComponent.ScriptGroup = data.ScriptGroup;
                targetComponent.data = data;
                targetComponent.data.AnimationModules.ForEach(a => a.Animator = FindObjectWithPath(schematic.transform, a.AnimatorAdress).gameObject);
                targetComponent.UseScriptValue = false;
            }

            foreach (var data in fdatas)
            {
                Transform target = FindObjectWithPath(schematic.transform, data.ObjectId);

                InteractableObject targetComponent = target.AddComponent<InteractableObject>();

                targetComponent.ScriptGroup = data.ScriptGroup;
                targetComponent.ScriptValueData = data;
                targetComponent.data.AnimationModules.ForEach(a => a.Animator = FindObjectWithPath(schematic.transform, a.AnimatorAdress).gameObject);
                targetComponent.UseScriptValue = true;
            }
        }
    }

    [MenuItem("SchematicManager/Decompile Interactable Pickups", priority = 1)]
    public static void DecompileInteractablePickups()
    {
        foreach (Schematic schematic in GameObject.FindObjectsOfType<Schematic>())
        {
            List<IPDTO> datas = Decompile<IPDTO>(schematic, "Pickups");
            List<FIPDTO> fdatas = Decompile<FIPDTO>(schematic, "FPickups");

            foreach (var data in datas)
            {
                Transform target = FindObjectWithPath(schematic.transform, data.ObjectId);

                InteractablePickup interactablePickup = target.AddComponent<InteractablePickup>();


                interactablePickup.ScriptGroup = data.ScriptGroup;
                interactablePickup.data = data;
                interactablePickup.data.AnimationModules.ForEach(a => a.Animator = FindObjectWithPath(schematic.transform, a.AnimatorAdress).gameObject);
                interactablePickup.UseScriptValue = false;
            }

            foreach (var data in fdatas)
            {
                Transform target = FindObjectWithPath(schematic.transform, data.ObjectId);

                InteractablePickup interactableObject = target.AddComponent<InteractablePickup>();

                interactableObject.ScriptGroup = data.ScriptGroup;
                interactableObject.ScriptValueData = data;
                interactableObject.data.AnimationModules.ForEach(a => a.Animator = FindObjectWithPath(schematic.transform, a.AnimatorAdress).gameObject);
                interactableObject.UseScriptValue = true;
            }
        }
    }

    private static readonly Config Config = SchematicManager.Config;
    public static Transform FindObjectWithPath(Transform target, string pathO)
    {
        pathO = pathO.Trim();
        if (pathO != "")
        {
            string[] path = pathO.Split(' ');
            for (int i = path.Length - 1; i > -1; i--)
            {
                if (target.childCount == 0 || target.childCount <= int.Parse(path[i].ToString()))
                {
                    Debug.Log("AMERT Importer: Could not find appropriate child!");
                    break;
                }
                target = target.GetChild(int.Parse(path[i]));
            }
        }
        return target;
    }

    /// <summary>
    /// Get a list of compiled custom components of the provided Type using the name as an extension to the schematic to search.
    /// </summary>
    /// <typeparam name="TDO">The Type of the custom component.</typeparam>
    /// <param name="schematic">The Schematic to apply to.</param>
    /// <param name="name">The extension onto the name of the schematic in the json containing the custom components.</param>
    /// <returns>A List of decompiled custom components.</returns>
    public static List<TDO> Decompile<TDO>(Schematic schematic, string name)
    {
        string parentDirectoryPath = Directory.Exists(Config.ExportPath)
            ? Config.ExportPath
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "MapEditorReborn_CompiledSchematics");
        string schematicDirectoryPath = Path.Combine(parentDirectoryPath, schematic.gameObject.name);

        string amertData = Path.Combine(schematicDirectoryPath, $"{schematic.gameObject.name}-{name}.json");

        if (!Directory.Exists(parentDirectoryPath))
        {
            Debug.LogError("Could Not find root object's compiled directory!");
            return new List<TDO>();
        }
        if (!File.Exists(amertData))
        {
            return new List<TDO>();
        }
        return JsonConvert.DeserializeObject<List<TDO>>(File.ReadAllText(amertData), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, TypeNameHandling = TypeNameHandling.Auto });
    }
}

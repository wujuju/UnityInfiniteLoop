using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.Callbacks;
using System.Linq;
using System.Reflection;
using ILRuntime.Mono.Cecil;
using ILRuntime.Mono.Cecil.Cil;

public class HookEditor
{
    private static List<string> assemblyPathss = new List<string>()
    {
        Application.dataPath + "/../Library/ScriptAssemblies/Assembly-CSharp.dll",
    };

    [MenuItem("Hook/主动注入代码")]
    static void HookScript()
    {
        string editor_root = Path.GetDirectoryName(EditorApplication.applicationPath);
        editor_root = editor_root.Replace("Program Files", "Progra~1");
        string pdb_path = editor_root + @"\Data\MonoBleedingEdge\lib\mono\4.5\pdb2mdb.exe";
        string mono_path = editor_root + @"\Data\MonoBleedingEdge\bin\mono.exe";

        if (!File.Exists(pdb_path))
        {
            Debug.LogError("找不到文件:" + pdb_path);
            return;
        }
        if (!File.Exists(mono_path))
        {
            Debug.LogError("找不到文件:" + mono_path);
            return;
        }


        AssemblyPostProcessorRun();
    }

    [MenuItem("Hook/还原代码")]
    static void RevertCompile()
    {
        //foreach (String assemblyPath in assemblyPathss)
        //{
        //    File.Delete(assemblyPath);
        //    File.Delete(assemblyPath.Replace(".dll", ".pdb"));

        //    File.Copy(assemblyPath + ".bak", assemblyPath, true);
        //    File.Copy(assemblyPath.Replace(".dll", ".pdb") + ".bak", assemblyPath.Replace(".dll", ".pdb"), true);
        //    File.Delete(assemblyPath + ".bak");
        //    File.Delete(assemblyPath + ".pdb.bak");
        //}
    }

    //[PostProcessScene]//打包的时候会自动调用下面方法注入代码
    static void AssemblyPostProcessorRun()
    {
        bool wasProcessed = false;
        try
        {
            Debug.Log("AssemblyPostProcessor running");
            EditorApplication.LockReloadAssemblies();
            DefaultAssemblyResolver assemblyResolver = new DefaultAssemblyResolver();

            foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    assemblyResolver.AddSearchDirectory(Path.GetDirectoryName(assembly.Location));
                }
                catch (Exception e)
                {
                }
            }

            assemblyResolver.AddSearchDirectory(Path.GetDirectoryName(EditorApplication.applicationPath) + "/Data/Managed");

            ReaderParameters readerParameters = new ReaderParameters();
            readerParameters.AssemblyResolver = assemblyResolver;

            WriterParameters writerParameters = new WriterParameters();

            foreach (String assemblyPath in assemblyPathss)
            {
                readerParameters.ReadSymbols = true;
                readerParameters.SymbolReaderProvider = new ILRuntime.Mono.Cecil.Pdb.PdbReaderProvider();
                writerParameters.WriteSymbols = true;
                writerParameters.SymbolWriterProvider = new ILRuntime.Mono.Cecil.Pdb.PdbWriterProvider();

                AssemblyDefinition assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyPath, readerParameters);

                Debug.Log("Processing " + Path.GetFileName(assemblyPath));
                if (HookEditor.ProcessAssembly(assemblyDefinition))
                {
                    wasProcessed = true;
                    Debug.Log("Writing to " + assemblyPath);
                    assemblyDefinition.Write(assemblyPath + ".temp", writerParameters);
                    Debug.Log("Done writing");
                }
                else
                {
                    Debug.Log(Path.GetFileName(assemblyPath) + " didn't need to be processed");
                }
                assemblyDefinition.Dispose();
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
        EditorApplication.UnlockReloadAssemblies();
        if (wasProcessed)
        {
            foreach (String assemblyPath in assemblyPathss)
            {
                File.Copy(assemblyPath, assemblyPath + ".bak", true);
                File.Copy(assemblyPath.Replace(".dll", ".pdb"), assemblyPath.Replace(".dll", ".pdb") + ".bak", true);
                File.Delete(assemblyPath);
                File.Delete(assemblyPath.Replace(".dll", ".pdb"));
                File.Copy(assemblyPath + ".temp", assemblyPath, true);
                File.Copy(assemblyPath + ".pdb", assemblyPath.Replace(".dll", ".pdb"), true);
                File.Delete(assemblyPath + ".temp");
                File.Delete(assemblyPath + ".pdb");
            }
        }
    }

    private static bool ProcessAssembly(AssemblyDefinition assemblyDefinition)
    {
        bool wasProcessed = false;
        foreach (ModuleDefinition moduleDefinition in assemblyDefinition.Modules)
        {
            foreach (TypeDefinition typeDefinition in moduleDefinition.Types)
            {
                //过滤抽象类
                if (typeDefinition.IsAbstract) 
                    continue;
                //过滤抽象方法
                if (typeDefinition.IsInterface) 
                    continue;
                //自己的方法没有命名空间
                if (typeDefinition.Namespace != "") 
                    continue;
                //过滤特殊字符的
                byte ascii = System.Text.Encoding.ASCII.GetBytes(typeDefinition.Name.ToLower().Substring(0, 1))[0];
                if (ascii < 65 || ascii > 122 || (ascii > 90 && ascii < 97))
                    continue;
                Debug.LogError("Hook:" + typeDefinition.Name);
                foreach (MethodDefinition methodDefinition in typeDefinition.Methods)
                {
                    //过滤构造函数
                    if (methodDefinition.Name == ".ctor") continue;
                    if (methodDefinition.Name == ".cctor") continue;
                    //过滤抽象方法、虚函数、get set 方法
                    if (methodDefinition.IsAbstract) continue;
                    if (methodDefinition.IsVirtual) continue;
                    if (methodDefinition.IsGetter) continue;
                    if (methodDefinition.IsSetter) continue;
                    if (methodDefinition.Body == null) continue;
                    //如果注入代码失败，可以打开下面的输出看看卡在了那个方法上。
                    MethodReference logMethodReference = moduleDefinition.ImportReference(typeof(HookUtils).GetMethod("Begin", new Type[] { typeof(string) }));
                    ILProcessor ilProcessor = methodDefinition.Body.GetILProcessor();
                    Instruction first = methodDefinition.Body.Instructions[0];
                    ilProcessor.InsertBefore(first, Instruction.Create(OpCodes.Ldstr, typeDefinition.FullName + "." + methodDefinition.Name));
                    ilProcessor.InsertBefore(first, Instruction.Create(OpCodes.Call, logMethodReference));

                    wasProcessed = true;
                }
            }
        }
        return wasProcessed;
    }
    
}
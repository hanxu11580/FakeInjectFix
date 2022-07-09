using LitJson;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

// 参考
// Mono.Cecil https://github.com/jbevain/cecil
// https://www.jianshu.com/p/a5276aadccdd


public static class InjectTools
{
    const string GenPatchMethodPrefix = "GenMethod_";
    const string FixJson = "FixJson";

    [MenuItem("Tools/RequestScriptReload")]
    public static void RequestScriptReload()
    {
        CompilationPipeline.RequestScriptCompilation();
    }

    private const string AssemblyPath = "./Library/ScriptAssemblies/Assembly-CSharp.dll";

    [MenuItem("Tools/Inject")]
    public static void Inject()
    {
        if (EditorApplication.isCompiling || Application.isPlaying)
        {
            UnityEngine.Debug.LogError("compiling or playing");
            return;
        }

        // mono.cecil 0.10必须这么写，不然写入时会报IOException: Sharing violation on path
        // https://github.com/jbevain/cecil/issues/291
        var readerParameters = new ReaderParameters
        {
            InMemory = true,
            ReadWrite = true,
        };

        using (AssemblyDefinition assemblyDefinition =
                AssemblyDefinition.ReadAssembly(AssemblyPath, readerParameters))
        {
            var mainModule = assemblyDefinition.MainModule;
            var fixFullName = typeof(FixAttribute).FullName;
            var testPatchFullName = typeof(TestPatchAttribute).FullName;
            var patchFullName = typeof(PatchAttribute).FullName;

            foreach (TypeDefinition typeDefinition in mainModule.Types)
            {
                // 这个类是否需要注入
                var needInject = typeDefinition.CustomAttributes.Any(attr =>
                    attr.AttributeType.FullName.Equals(fixFullName, StringComparison.Ordinal));
                if (!needInject) continue;

                foreach (MethodDefinition method in typeDefinition.Methods)
                {
                    //if (method.CustomAttributes.Any(attr =>
                    //        attr.AttributeType.FullName.Equals(testPatchFullName, StringComparison.Ordinal)))
                    //{
                    //    if (method.IsConstructor || method.IsGetter || method.IsSetter || !method.IsPublic)
                    //        continue;

                    //    // test
                    //    if (method.Name == "Log")
                    //    {
                    //        // 这个注入方式失败了
                    //        //InjectLog_1(mainModule, method);

                    //        InjectLog_2(mainModule, method);
                    //        PrintMethodIns(method);
                    //        break;
                    //    }
                    //}

                    if (method.CustomAttributes.Any(attr =>attr.AttributeType.FullName.Equals(patchFullName, StringComparison.Ordinal))) {
                        if (method.IsConstructor || method.IsGetter || method.IsSetter || !method.IsPublic)
                            continue;

                        // test
                        if (method.Name == "Sum") {
                            InjectMethod(mainModule, method);
                            PrintMethodIns(method);
                            break;
                        }
                    }

                }
            }
            assemblyDefinition.Write(AssemblyPath, new WriterParameters {WriteSymbols = false});
        }
    }

    [MenuItem("Tools/Fix")]
    public static void Fix() {
        if (EditorApplication.isCompiling || Application.isPlaying) {
            UnityEngine.Debug.LogError("compiling or playing");
            return;
        }
        var readerParameters = new ReaderParameters {
            InMemory = true,
            ReadWrite = true,
        };
        using (AssemblyDefinition assemblyDefinition =
                AssemblyDefinition.ReadAssembly(AssemblyPath, readerParameters)) {
            var mainModule = assemblyDefinition.MainModule;
            var fixFullName = typeof(FixAttribute).FullName;
            var patchFullName = typeof(PatchAttribute).FullName;

            foreach (TypeDefinition typeDefinition in mainModule.Types) {
                var needInject = typeDefinition.CustomAttributes.Any(attr =>
                    attr.AttributeType.FullName.Equals(fixFullName, StringComparison.Ordinal));
                if (!needInject) continue;

                foreach (MethodDefinition method in typeDefinition.Methods) {
                    if (method.CustomAttributes.Any(attr => attr.AttributeType.FullName.Equals(patchFullName, StringComparison.Ordinal))) {
                        if (method.IsConstructor || method.IsGetter || method.IsSetter || !method.IsPublic)
                            continue;
                        // test
                        if (method.Name == "Sum") {
                            Fix(mainModule, method);
                            break;
                        }
                    }

                }
            }
            assemblyDefinition.Write(AssemblyPath, new WriterParameters { WriteSymbols = false });
        }
    }


    static void Test(ModuleDefinition module, MethodDefinition method)
    {
        var insertPoint = method.Body.Instructions[0];
        var ilProcessor = method.Body.GetILProcessor();

        // 打印所有IL指令
        //LogMethodIns(method);

        // 打印所有操作数
        //foreach (Instruction instruction in method.Body.Instructions)
        //{
        //    Debug.Log($"{method.Name}-{instruction.OpCode}-{instruction.Operand}");
        //}

        //操作数还可以为IL指令类
        //var ins5 = method.Body.Instructions[5];
        //Debug.Log(ins5.Operand);
        //Debug.Log(ins5.GetType().FullName);
    }

    #region 注入Class A的Log函数

    // 通过Log100Add200函数可以发现有3个int类型变量
    // 本来是想将Log100Add200函数的内容直接注入到Log函数
    // 但是因为局部变量访问不到，无法注入
    //.locals init(int32 V_0,
    // int32 V_1,
    // int32 V_2) 
    static void InjectLog_1(ModuleDefinition module, MethodDefinition method)
    {
        Debug.Log("开始修改");

        var ilp = method.Body.GetILProcessor();
        // nop
        var ins0 = method.Body.Instructions[0];

        // 替换参数
        var ins1 = method.Body.Instructions[1];
        ins1.Operand = "This is B";

        // 替换方法
        var ins2 = method.Body.Instructions[2];
        var logErrorMethodRef =
            module.ImportReference(typeof(Debug).GetMethod("LogError", new Type[] {typeof(object)}));
        // 方法一 可以直接替换
        //ins2.Operand = logErrorMethodRef;
        // 方法二 也可以使用ILProcessor.Replace方法
        var logErrorIns = ilp.Create(OpCodes.Call, logErrorMethodRef);
        ilp.Replace(ins2, logErrorIns);

        // 在log前面加下面操作
        //var a = 100;
        //var b = 200;
        //var res = a + b;
        //Debug.LogError(res);


        int startInsIndex = 0;
        var ins_ldc_i4_s = ilp.Create(OpCodes.Ldc_I4, 100);
        IncrAddIns(ilp, ins_ldc_i4_s, ref startInsIndex);
        var ins_stloc_0 = ilp.Create(OpCodes.Stloc_0);
        IncrAddIns(ilp, ins_stloc_0, ref startInsIndex);
        var ins_ldc_i4 = ilp.Create(OpCodes.Ldc_I4, 200);
        IncrAddIns(ilp, ins_ldc_i4, ref startInsIndex);
        var ins_stloc_1 = ilp.Create(OpCodes.Stloc_1);
        IncrAddIns(ilp, ins_stloc_1, ref startInsIndex);
        var ins_ldloc_0 = ilp.Create(OpCodes.Ldloc_0);
        var ins_ldloc_1 = ilp.Create(OpCodes.Ldloc_1);
        IncrAddIns(ilp, ins_ldloc_0, ref startInsIndex);
        IncrAddIns(ilp, ins_ldloc_1, ref startInsIndex);
        var ins_add = ilp.Create(OpCodes.Add);
        IncrAddIns(ilp, ins_add, ref startInsIndex);
        var ins_stloc_2 = ilp.Create(OpCodes.Stloc_2);
        var ins_ldloc_2 = ilp.Create(OpCodes.Ldloc_2);
        IncrAddIns(ilp, ins_stloc_2, ref startInsIndex);
        IncrAddIns(ilp, ins_ldloc_2, ref startInsIndex);
        var intType = module.ImportReference(typeof(System.Int32));
        var ins_box = ilp.Create(OpCodes.Box, intType);
        IncrAddIns(ilp, ins_box, ref startInsIndex);
        var logMethodRef = module.ImportReference(typeof(Debug).GetMethod("Log", new Type[] {typeof(object)}));
        var ins_call_log = ilp.Create(OpCodes.Call, logMethodRef);
        IncrAddIns(ilp, ins_call_log, ref startInsIndex);
        var ins_nop = ilp.Create(OpCodes.Nop);
        IncrAddIns(ilp, ins_nop, ref startInsIndex);
    }

    // 方法1失败了
    // 下面考虑 在执行Log前 调用Log100Add200函数
    static void InjectLog_2(ModuleDefinition module, MethodDefinition method)
    {
        var ilp = method.Body.GetILProcessor();
        //int startInsIndex = 3;

        #region 调用A的静态方法LogSome

        //var method_LogSome = module.ImportReference(typeof(A).GetMethod("LogSome"));
        //var ins_call = ilp.Create(OpCodes.Call, method_LogSome);
        //IncrAddIns(ilp, ins_call, ref startInsIndex);
        //AddNopIns(ilp, startInsIndex);

        #endregion

        #region 调用成员方法Log100Add200

        // 在注入成员方法和静态方法的区别就是多了一个ldarg_0指令，这里0就是A的instance
        //var ins_ldarg_0 = ilp.Create(OpCodes.Ldarg_0);
        //IncrAddIns(ilp, ins_ldarg_0, ref startInsIndex);
        //var method_Log100Add200 = module.ImportReference(typeof(A).GetMethod("Log100Add200"));
        //var ins_call = ilp.Create(OpCodes.Call, method_Log100Add200);
        //IncrAddIns(ilp, ins_call, ref startInsIndex);
        //AddNopIns(ilp, startInsIndex);

        #endregion

        #region 调用静态方法LogNumber

        // 这个不行
        var startIns = ilp.Body.Instructions[3];
        var ins_ld_i4 = ilp.Create(OpCodes.Ldc_I4, 300);
        //var method_LogNumber = module.ImportReference(typeof(A).GetMethod("LogNumber"));
        var method_LogNumber = module.Types.Single(t => t.Name == "A").Methods.Single(m => m.Name == "LogNumber" && m.Parameters.Count == 1);
        var ins_call = ilp.Create(OpCodes.Call, method_LogNumber);
        var ins_nop = ilp.Create(OpCodes.Nop);
        ilp.InsertBefore(startIns, ins_nop);
        ilp.InsertBefore(startIns, ins_ld_i4);
        ilp.InsertBefore(startIns, ins_call);
        ilp.InsertBefore(startIns, ins_nop);

        #endregion

        #region ok
        //ILProcessor ilProcessor = method.Body.GetILProcessor();
        //var f = ilProcessor.Body.Instructions.First();
        //ilProcessor.InsertBefore(f, ilProcessor.Create(OpCodes.Ldstr, "hello"));
        //ilProcessor.InsertBefore(f, ilProcessor.Create(OpCodes.Call,
        //    module.ImportReference(typeof(Debug).GetMethod("Log", new Type[] { typeof(object) }))));
        #endregion
    }

    #endregion

    static void LookLog100Add200(ModuleDefinition module, MethodDefinition method)
    {
        // 可以发现这个函数有3个int形变量
        var variables = method.Body.Variables;
        foreach (var v in variables)
        {
            Debug.Log(v.VariableType.Name);
        }
    }

    #region help

    /// <summary>
    /// 打印所有IL指令
    /// </summary>
    /// <param name="method"></param>
    public static void PrintMethodIns(MethodDefinition method)
    {
        foreach (Instruction instruction in method.Body.Instructions)
        {
            Debug.Log($"{method.Name}-{instruction}");
        }
    }

    /// <summary>
    /// 递增添加il指令
    /// </summary>
    public static void IncrAddIns(ILProcessor ilp, Instruction target, ref int index)
    {
        ilp.InsertAfter(index, target);
        index++;
    }

    public static void AddNopIns(ILProcessor ilp, int indexAfter)
    {
        var ins_nop = ilp.Create(OpCodes.Nop);
        ilp.InsertAfter(indexAfter, ins_nop);
    }

    #endregion

    #region 插桩

    static void InjectMethod(ModuleDefinition module, MethodDefinition method)
    {
        //TODO 使用InsertBefore插入

        // 生成方法
        var genMethod = GenPatchMethod(module, method);


        // 插桩
        var ilp = method.Body.GetILProcessor();
        var startNopIns = method.Body.Instructions[0];
        var brFalseJumpIns = startNopIns.Next; // 条件不满足时跳转的指令

        var ins_ldstr = ilp.Create(OpCodes.Ldstr, "0"); // 插入patchKey
        ilp.InsertBefore(startNopIns, ins_ldstr); //插到index = 0位置

        var incrIndex = 0;
        var method_HasPatch = module.Types.Single(t => t.Name == "PatchLoad").Methods.Single(m => m.Name == "HasPatch");
        var ins_call = ilp.Create(OpCodes.Call, method_HasPatch);
        IncrAddIns(ilp, ins_call, ref incrIndex);
        var ins_brfalse = ilp.Create(OpCodes.Brfalse, brFalseJumpIns);
        IncrAddIns(ilp, ins_brfalse, ref incrIndex);

        // 用于测试直接返回0
        //var ins_ldc_i4_0 = ilp.Create(OpCodes.Ldc_I4_0);
        //IncrAddIns(ilp, ins_ldc_i4_0, ref incrIndex);

        // 拥有Patch时返回执生成方法，并返回
        if(method.Parameters.Count > 0) {
            //如果有参数 把参数从局部放入栈中
            //for (int i = 0; i < method.Parameters.Count; i++) {
            //    IncrAddIns(ilp, Instruction.Create(OpCodes.Ldarg_S, (byte)i), ref incrIndex);
            //}

            // 知道Sum有2个参数 直接方进入就行了
            IncrAddIns(ilp, Instruction.Create(OpCodes.Ldarg_1), ref incrIndex);
            IncrAddIns(ilp, Instruction.Create(OpCodes.Ldarg_2), ref incrIndex);

        }

        IncrAddIns(ilp, Instruction.Create(OpCodes.Call, genMethod), ref incrIndex);

        var ins_ret = ilp.Create(OpCodes.Ret);
        IncrAddIns(ilp, ins_ret, ref incrIndex);
    }

    /// <summary>
    /// 生成补丁方法
    /// </summary>
    /// <returns></returns>
    static MethodDefinition GenPatchMethod(ModuleDefinition module, MethodDefinition method) {
        // PatchLoad
        var patchLoadType = module.Types.Single(t => t.Name == "PatchLoad");
        var vmField = patchLoadType.Fields.Single(f => f.Name == "virtualMachine");
        // Call
        var callType = module.Types.Single(t => t.Name == "Call");
        var callBegin = callType.Methods.Single(m => m.Name == "Begin");
        var callPushInt32 = callType.Methods.Single(m => m.Name == "PushInt32");
        var callGetInt32 = callType.Methods.Single(m => m.Name == "GetInt32");
        // VirtualMachine
        var vmExecute = module.Types.Single(t => t.Name == "VirtualMachine")
            .Methods.Single(m => m.Name == "Execute" && m.Parameters.Count == 4);
        // 基础方法
        // 返回值
        // 参数

        var returnType = method.ReturnType;
        Mono.Cecil.MethodAttributes methodAttributes = Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static;
        MethodDefinition patchMethod = new MethodDefinition(GenPatchMethodPrefix, methodAttributes, returnType);

        // 添加参数
        foreach (var parameter in method.Parameters) {
            patchMethod.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
        }

        // var call
        VariableDefinition callVar = new VariableDefinition(callType);
        patchMethod.Body.Variables.Add(callVar);

        var ilp  = patchMethod.Body.GetILProcessor();

        ilp.Append(Instruction.Create(OpCodes.Nop));
        ilp.Append(Instruction.Create(OpCodes.Call, callBegin));
        ilp.Append(Instruction.Create(OpCodes.Stloc_0));
        // 将位于特定索引处的局部变量的地址加载到计算堆栈上
        ilp.Append(Instruction.Create(OpCodes.Ldloca_S, callVar));
        // 将索引为 0 的参数加载到计算堆栈上。
        ilp.Append(Instruction.Create(OpCodes.Ldarg_0));
        ilp.Append(Instruction.Create(OpCodes.Call, callPushInt32));
        ilp.Append(Instruction.Create(OpCodes.Nop));
        ilp.Append(Instruction.Create(OpCodes.Ldloca_S, callVar));
        ilp.Append(Instruction.Create(OpCodes.Ldarg_1));
        ilp.Append(Instruction.Create(OpCodes.Call, callPushInt32));
        ilp.Append(Instruction.Create(OpCodes.Nop));
        // 将静态字段的值推送到堆栈
        ilp.Append(Instruction.Create(OpCodes.Ldsfld, vmField));
        ilp.Append(Instruction.Create(OpCodes.Ldc_I4_0));
        ilp.Append(Instruction.Create(OpCodes.Ldloca_S, callVar));
        ilp.Append(Instruction.Create(OpCodes.Ldc_I4_2));
        ilp.Append(Instruction.Create(OpCodes.Ldc_I4_0));
        ilp.Append(Instruction.Create(OpCodes.Call, vmExecute));
        ilp.Append(Instruction.Create(OpCodes.Nop));

        ilp.Append(Instruction.Create(OpCodes.Ldloca_S, callVar));
        ilp.Append(Instruction.Create(OpCodes.Ldc_I4_0));
        ilp.Append(Instruction.Create(OpCodes.Call, callGetInt32));
        // 将计算堆栈1 存储在局部变量列表中
        //ilp.Append(Instruction.Create(OpCodes.Stloc_1));
        // 将局部变量1 加载到计算堆栈中
        //ilp.Append(Instruction.Create(OpCodes.Ldloc_1));
        ilp.Append(Instruction.Create(OpCodes.Ret));


        // 测试方法
        //ilp.Append(Instruction.Create(OpCodes.Ldstr, "我是生成的方法"));
        //var log_method = module.ImportReference(typeof(Debug).GetMethod("Log", new Type[] { typeof(object) }));
        //ilp.Append(Instruction.Create(OpCodes.Call, log_method));

        // 测试用  直接返回0
        //ilp.Append(Instruction.Create(OpCodes.Ldc_I4_0));
        //ilp.Append(Instruction.Create(OpCodes.Ret));

        patchLoadType.Methods.Add(patchMethod);

        return patchMethod;
    }

    #endregion

    #region 修复
    static void Fix(ModuleDefinition module, MethodDefinition method) {
        //{
        //  .custom instance void PatchAttribute::.ctor() = (01 00 00 00 ) 
        //  // 代码大小       9 (0x9)
        //  .maxstack  2
        //  .locals init(int32 V_0)
        //  IL_0000: nop
        // IL_0001:  ldarg.1
        //  IL_0002: ldarg.2
        //  IL_0003: add
        // IL_0004:  stloc.0
        //  IL_0005: br.s IL_0007
        //  IL_0007: ldloc.0
        //  IL_0008: ret
        //}
        var ils = method.Body.Instructions;
        var parseIls = new List<VMInstruction>();
        for (int i = 0; i < ils.Count; i++) {
            var il = ils[i];
            var ilStr = ils[i].OpCode.Code.ToString();
            switch (il.OpCode.Code) {
                case Mono.Cecil.Cil.Code.Nop:
                    parseIls.Add(new VMInstruction {
                        Code = Code.StackSpace,
                        Operand = (method.Body.Variables.Count << 16) | method.Body.MaxStackSize
                    }); // local | maxstack
                    break;
                case Mono.Cecil.Cil.Code.Ldarg_0:
                case Mono.Cecil.Cil.Code.Ldarg_1:
                case Mono.Cecil.Cil.Code.Ldarg_2:
                case Mono.Cecil.Cil.Code.Ldarg_3:
                    parseIls.Add(new VMInstruction() {
                        Code = Code.Ldarg,
                        Operand = int.Parse(ilStr.Substring(ilStr.Length - 1)) - 1
                    });
                    break;
                case Mono.Cecil.Cil.Code.Add:
                    parseIls.Add(new VMInstruction {
                        Code = (Code)Enum.Parse(typeof(Code), ilStr),
                        Operand = 0
                    });
                    break;
                case Mono.Cecil.Cil.Code.Stloc_0:
                case Mono.Cecil.Cil.Code.Stloc_1:
                case Mono.Cecil.Cil.Code.Stloc_2:
                case Mono.Cecil.Cil.Code.Stloc_3:
                    parseIls.Add(new VMInstruction {
                        Code = Code.Stloc,
                        Operand = int.Parse(ilStr.Substring(ilStr.Length - 1)),
                    });
                    break;
                case Mono.Cecil.Cil.Code.Ret:
                    parseIls.Add(new VMInstruction {
                        Code = Code.Ret,
                        Operand = method.ReturnType.ToString() == "System.Void" ? 0 : 1,
                    });
                    break;
            }
        }

        foreach (var vmil in parseIls) {
            Debug.LogError(vmil);
        }

        var json = JsonMapper.ToJson(parseIls);
        // Debug.LogError(json);
        EditorPrefs.SetString(FixJson, json);
    }

    #endregion
}
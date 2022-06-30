using LitJson;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class PatchLoad
{
    /// <summary>
    /// 根据方法名字存对应的补丁
    /// </summary>
    static Dictionary<string, object> _patchDic;

    static VirtualMachine virtualMachine;

    static PatchLoad()
    {
        _patchDic = new Dictionary<string, object>();
        virtualMachine = CreateVirtualMachine();
    }

    public static void LoadPatch(string patchPath)
    {

    }

    public static bool HasPatch(string patchKey)
    {
        return _patchDic.ContainsKey(patchKey);
    }

    public  static int TestStatic(int a, int b) {
        Debug.LogError("PatchLoad Excute Static Method");
        return a + b;
    }

    unsafe static public VirtualMachine CreateVirtualMachine() {
        //VMInstruction[][] methods = new VMInstruction[][]
        //{
        //        new VMInstruction[] //int add(int a, int b)
        //        {
        //            new VMInstruction {Code = Code.StackSpace, Operand = 2 },
        //            new VMInstruction {Code = Code.Ldarg, Operand = 0 },
        //            new VMInstruction {Code = Code.Ldarg, Operand = 1 },
        //            new VMInstruction {Code = Code.Add },
        //            new VMInstruction {Code = Code.Ret , Operand = 1},
        //        },
        //};

        var json = EditorPrefs.GetString("FixJson");
        var vmils = JsonMapper.ToObject<List<VMInstruction>>(json);
        VMInstruction[][] methods = new VMInstruction[1][];
        methods[0] = vmils.ToArray();

        List<IntPtr> nativePointers = new List<IntPtr>();

        // unmanagedCodes[i][j] 第几个方法的第几个指令
        IntPtr nativePointer = System.Runtime.InteropServices.Marshal.AllocHGlobal(
            sizeof(VMInstruction*) * methods.Length);
        // sizeof(VMInstruction)=8 指令大小也是8个字节
        // sizeof(VMInstruction*)=8 指令大小就是8个字节
        VMInstruction** unmanagedCodes = (VMInstruction**)nativePointer.ToPointer();
        nativePointers.Add(nativePointer);

        for (int i = 0; i < methods.Length; i++) {
            // 这个方法里，有几条指令，就分配多少个VMInstruction大小的内存
            nativePointer = System.Runtime.InteropServices.Marshal.AllocHGlobal(
                sizeof(VMInstruction) * methods[i].Length);
            // ToPointer返回指向VMInstruction起始位置的指针
            unmanagedCodes[i] = (VMInstruction*)nativePointer.ToPointer();
            for (int j = 0; j < methods[i].Length; j++) {
                unmanagedCodes[i][j] = methods[i][j];
            }
            nativePointers.Add(nativePointer);

            _patchDic.Add(i.ToString(), null);
        }

        return new VirtualMachine(unmanagedCodes, () => {
            for (int i = 0; i < nativePointers.Count; i++) {
                System.Runtime.InteropServices.Marshal.FreeHGlobal(nativePointers[i]);
            }
        });
    }

    public static int SafeCall(int a, int b) {
        Call call = Call.Begin();
        call.PushInt32(a);
        call.PushInt32(b);
        virtualMachine.Execute(0, ref call, 2);
        //Call.End(ref call);
        return call.GetInt32();
        //Debug.LogError($"计算结果:{addRes}");
        //virtualMachine = null;
    }
}

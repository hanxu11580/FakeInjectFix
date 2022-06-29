using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using LitJson;
using UnityEditor;

public class TTT : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        A a = new A();
        UnityEngine.Debug.LogError(a.Sum(1, 2));
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
        }

        return new VirtualMachine(unmanagedCodes, () =>
        {
            for (int i = 0; i < nativePointers.Count; i++) {
                System.Runtime.InteropServices.Marshal.FreeHGlobal(nativePointers[i]);
            }
        });
    }

    public static void SafeCall() {
        var virtualMachine = CreateVirtualMachine();
        var sw = Stopwatch.StartNew();
        Call call = Call.Begin();
        call.PushInt32(1);
        call.PushInt32(2);
        virtualMachine.Execute(0, ref call, 2);
        //Call.End(ref call);
        var addRes = call.GetInt32();
        UnityEngine.Debug.LogError($"计算结果:{addRes}");
        virtualMachine = null;
    }
}

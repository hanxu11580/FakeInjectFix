using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TTT : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        A a = new A();
        Debug.LogError(a.Sum(1, 2));

    }

    unsafe static public VirtualMachine CreateVirtualMachine(int loopCount) {
        VMInstruction[][] methods = new VMInstruction[][]
        {
                new VMInstruction[] //int add(int a, int b)
                {
                    new VMInstruction {Code = Code.StackSpace, Operand = 2 },
                    new VMInstruction {Code = Code.Ldarg, Operand = 0 },
                    new VMInstruction {Code = Code.Ldarg, Operand = 1 },
                    new VMInstruction {Code = Code.Add },
                    new VMInstruction {Code = Code.Ret , Operand = 1},
                },
                new VMInstruction[] // void test()
                {
                    new VMInstruction {Code = Code.StackSpace, Operand = (1 << 16) | 2}, // local | maxstack
                    //TODO: local init
                    new VMInstruction {Code = Code.Ldc_I4, Operand = 0 }, //1
                    new VMInstruction {Code = Code.Stloc, Operand = 0},   //2
                    new VMInstruction {Code = Code.Br, Operand =  9}, // 3

                    new VMInstruction {Code = Code.Ldc_I4, Operand = 1 }, //4
                    new VMInstruction {Code = Code.Ldc_I4, Operand = 2 }, //5
                    new VMInstruction {Code = Code.Call, Operand = (2 << 16) | 0}, //6
                    new VMInstruction {Code = Code.Pop }, //7

                    new VMInstruction {Code = Code.Ldloc, Operand = 0 }, //8
                    new VMInstruction {Code = Code.Ldc_I4, Operand = 1 }, //9
                    new VMInstruction {Code = Code.Add }, //10
                    new VMInstruction {Code = Code.Stloc, Operand = 0 }, //11

                    new VMInstruction {Code = Code.Ldloc, Operand = 0 }, // 12
                    new VMInstruction {Code = Code.Ldc_I4, Operand =  loopCount}, // 13
                    new VMInstruction {Code = Code.Blt, Operand = -10 }, //14

                    new VMInstruction {Code = Code.Ret, Operand = 0 }
                }
        };

        List<IntPtr> nativePointers = new List<IntPtr>();

        IntPtr nativePointer = System.Runtime.InteropServices.Marshal.AllocHGlobal(
            sizeof(VMInstruction*) * methods.Length);
        VMInstruction** unmanagedCodes = (VMInstruction**)nativePointer.ToPointer();
        nativePointers.Add(nativePointer);

        for (int i = 0; i < methods.Length; i++) {
            nativePointer = System.Runtime.InteropServices.Marshal.AllocHGlobal(
                sizeof(VMInstruction) * methods[i].Length);
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


}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine; 

unsafe public class VirtualMachine {
    // 栈的大小
    public const int MAX_EVALUATION_STACK_SIZE = 1024 * 10;
    VMInstruction** unmanagedCodes;
    Action onDispose;

    public VirtualMachine(VMInstruction** unmanaged_codes, Action on_dispose) {
        unmanagedCodes = unmanaged_codes;
        onDispose = on_dispose;
    }

    ~VirtualMachine() {
        onDispose();
        unmanagedCodes = null;
    }

    public void Execute(int methodIndex, ref Call call, int argsCount, int refCount = 0) {
        Execute(unmanagedCodes[methodIndex], call.argumentBase + refCount, call.managedStack,
            call.evaluationStackBase, argsCount, methodIndex, refCount, call.topWriteBack);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="pc">方法的起始位置</param>
    /// <param name="argumentBase"></param>
    /// <param name="managedStack"></param>
    /// <param name="evaluationStackBase"></param>
    /// <param name="argsCount"></param>
    /// <param name="methodIndex"></param>
    /// <param name="refCount"></param>
    /// <param name="topWriteBack"></param>
    /// <returns></returns>
    public Value* Execute(VMInstruction* pc, Value* argumentBase, object[] managedStack,
        Value* evaluationStackBase, int argsCount, int methodIndex,
        int refCount = 0, Value** topWriteBack = null) {
        // InjectFix Code.StackSpace类似Nop
        if (pc->Code != Code.StackSpace)
        {
            throw new Exception("起始指令不为Code.StackSpace");
        }

        VMInstruction* pcb = pc;

        int localsCount = (pc->Operand >> 16); // 除以2^16
        int maxStack = (pc->Operand & 0xFFFF);
        int argumentPos = (int)(argumentBase - evaluationStackBase);
        if (argumentPos + argsCount + localsCount + maxStack > MAX_EVALUATION_STACK_SIZE) {
            throw new Exception("");
        }

        // argumentBase第一个参数 + argsCount参数数量
        // Value的大小是12，所以如果argsCount=2相当于 +24
        Value* localBase = argumentBase + argsCount;

        // evaluationStackPointer参数后的值
        // 例如加法1、2，就是1和2后面
        Value* evaluationStackPointer = localBase + localsCount;
        // Code.StackSpace的下一个IL指令
        pc++;

        while (true) {
            var code = pc->Code;
            switch (code) {
                // 将参数加载到栈上
                case Code.Ldarg: {
                        // 第一个Ldarg就是起始位置
                        // 第二个刚好在下一个10进制 +12
                        //new VMInstruction { Code = Code.Ldarg, Operand = 0 },
                        //new VMInstruction { Code = Code.Ldarg, Operand = 1 },
                        Value* argPtr = argumentBase + pc->Operand;
                        *evaluationStackPointer = *argPtr;
                        evaluationStackPointer++;
                        break;
                    }
                case Code.Add: {
                        // 取出栈上的2个参数
                        Value* arg1Ptr = evaluationStackPointer - 1;
                        Value* arg2Ptr = evaluationStackPointer - 2;
                        // 计算结果直接放在arg2位置上
                        evaluationStackPointer = arg2Ptr;
                        switch (arg1Ptr->Type) {
                            case ValueType.Integer: {
                                    evaluationStackPointer->Value1 = arg1Ptr->Value1 + arg2Ptr->Value1;
                                    break;
                                }
                        }
                        evaluationStackPointer++;
                        break;
                    }
                case Code.Ret: {
                        if (topWriteBack != null) {
                            *topWriteBack = argumentBase - refCount;
                        }
                        if (pc->Operand != 0) {
                            // 有返回值
                            // evaluationStackPointer - 1是因为上面++了
                            // 把计算出来的值赋给起始位置
                            *argumentBase = *(evaluationStackPointer - 1);
                            if (argumentBase->Type == ValueType.Object
                                || argumentBase->Type == ValueType.ValueType) {
                                int resultPos = argumentBase->Value1;
                                if (resultPos != argumentPos) {
                                    managedStack[argumentPos] = managedStack[resultPos];
                                    //managedStack[resultPos] = null;
                                }
                                argumentBase->Value1 = argumentPos;
                            }
                            for (int i = 0; i < evaluationStackPointer - evaluationStackBase - 1; i++) {
                                managedStack[i + argumentPos + 1] = null;
                            }

                            return argumentBase + 1;
                        }
                        else {
                            for (int i = 0; i < evaluationStackPointer - evaluationStackBase; i++) {
                                managedStack[i + argumentPos] = null;
                            }
                            return argumentBase;
                        }
                        break;
                    }
            }
        }
    }
}

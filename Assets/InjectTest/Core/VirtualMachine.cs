using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public enum ValueType {
    Integer,
    Long,
    Float,
    Double,
    StackReference,//Value = pointer, 
    StaticFieldReference,
    FieldReference,//Value1 = objIdx, Value2 = fieldIdx
    ChainFieldReference,
    Object,        //Value1 = objIdx
    ValueType,     //Value1 = objIdx
    ArrayReference,//Value1 = objIdx, Value2 = elemIdx
}

[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
public struct Value {
    public ValueType Type;
    public int Value1;
    public int Value2;
}

unsafe class ThreadStackInfo {
    public object[] ManagedStack;
    IntPtr evaluationStackHandler;

    public ThreadStackInfo() {
        evaluationStackHandler = Marshal.AllocHGlobal(sizeof(Value) * VirtualMachine.MAX_EVALUATION_STACK_SIZE);
        ManagedStack = new object[VirtualMachine.MAX_EVALUATION_STACK_SIZE];
    }

    static ThreadStackInfo _stack;
    public static ThreadStackInfo Stack
    {
        get {
            if(_stack == null) {
                _stack = new ThreadStackInfo();
            }
            return _stack;
        }
    }
}


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

    //public Value* Execute(Instruction* pc, Value* argumentBase, object[] managedStack,
    //        Value* evaluationStackBase, int argsCount, int methodIndex,
    //        int refCount = 0, Value** topWriteBack = null) {

    //}
}

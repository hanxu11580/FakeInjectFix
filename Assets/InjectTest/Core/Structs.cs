using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

[StructLayout(LayoutKind.Sequential)]
unsafe struct UnmanagedStack {
    public Value* Base;
    public Value* Top;
}

unsafe class ThreadStackInfo {
    public UnmanagedStack* UnmanagedStack;
    public object[] ManagedStack;

    IntPtr evaluationStackHandler;
    IntPtr unmanagedStackHandler;

    public ThreadStackInfo() {
        //Console.WriteLine($"sizeof(Value)={12}"); 1个枚举、2个int
        evaluationStackHandler = Marshal.AllocHGlobal(sizeof(Value) * VirtualMachine.MAX_EVALUATION_STACK_SIZE);
        //Console.WriteLine($"sizeof(UnmanagedStack)={16}"); // 俩指针
        unmanagedStackHandler = Marshal.AllocHGlobal(sizeof(UnmanagedStack));

        UnmanagedStack = (UnmanagedStack*)unmanagedStackHandler.ToPointer();
        // 刚开始都指向栈顶
        UnmanagedStack->Base = UnmanagedStack->Top = (Value*)evaluationStackHandler.ToPointer();
        ManagedStack = new object[VirtualMachine.MAX_EVALUATION_STACK_SIZE];
    }
    static LocalDataStoreSlot localSlot = Thread.AllocateDataSlot();

    internal static ThreadStackInfo Stack
    {
        get {
            var stack = Thread.GetData(localSlot) as ThreadStackInfo;
            if (stack == null) {
                stack = new ThreadStackInfo();
                Thread.SetData(localSlot, stack);
            }
            return stack;
        }
    }
}


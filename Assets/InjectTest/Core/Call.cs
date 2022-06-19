using System.Collections;
using System.Collections.Generic;
using UnityEngine;

unsafe public struct Call {
    internal Value* argumentBase;

    internal Value* evaluationStackBase;

    internal object[] managedStack;

    internal Value* currentTop;//用于push状态

    internal Value** topWriteBack;

    public static Call Begin() {
        var stack = ThreadStackInfo.Stack;
        return new Call() {
            managedStack = stack.ManagedStack,
            currentTop = stack.UnmanagedStack->Top,
            argumentBase = stack.UnmanagedStack->Top,
            evaluationStackBase = stack.UnmanagedStack->Base,
            topWriteBack = &(stack.UnmanagedStack->Top)
        };
    }

    public void PushInt32(int i) {
        currentTop->Value1 = i;
        currentTop->Type = ValueType.Integer;
        currentTop++;
    }

    public int GetInt32(int offset = 0) {
        return (argumentBase + offset)->Value1;
    }
}

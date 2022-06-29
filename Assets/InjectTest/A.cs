using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FixAttribute : Attribute { }

public class TestPatchAttribute : Attribute { }

/// <summary>
/// 利用这个特性给方法插桩
/// </summary>
public class PatchAttribute : Attribute { }

[Fix]
public class A
{
    //.method public hidebysig instance bool Compare(int32 a,
    //                                                int32 b) cil managed
    //        {
    //  // 代码大小       10 (0xa)
    //  .maxstack  2
    //  .locals init ([0] bool V_0)
    //  IL_0000:  nop
    //  IL_0001:  ldarg.1
    //  IL_0002:  ldarg.2             将索引参数加载到堆栈上
    //  IL_0003:  clt                 如果arg1 < arg2 返回1 否则返回 0
    //  IL_0005:  stloc.0             将堆栈索引0弹出 存在局部变量列表中
    //  IL_0006:  br.s IL_0008        无条件地将控制转移到目标指令
    //  IL_0008:  ldloc.0             将索引0局部变量加载到堆栈上
    //  IL_0009:  ret                 返回
    //    } // end of method A::Compare

    /// <summary>
    /// 错误代码
    /// </summary>
    public bool Compare(int a, int b)
    {
        //if (PatchLoad.HasPatch("Compare"))
        //{
        //    return false;
        //}
        return a < b;
    }

    [Patch]
    public int Sum(int a, int b)
    {
        return a + b;
    }

    [TestPatch]
    public void Log()
    {
        //var a = 100;
        //var b = 200;
        //var res = a + b;
        //Debug.Log(res);
        Debug.Log("This is A");
    }

    [TestPatch]
    public void Log100Add200()
    {
          //IL_0000:  nop
          //IL_0001:  ldc.i4.s   100
          //IL_0003:  stloc.0
          //IL_0004:  ldc.i4     0xc8
          //IL_0009:  stloc.1
          //IL_000a:  ldloc.0
          //IL_000b:  ldloc.1
          //IL_000c:  add
          //IL_000d:  stloc.2
          //IL_000e:  ldloc.2
          //IL_000f:  box        [netstandard]System.Int32
          //IL_0014:  call       void [UnityEngine.CoreModule]UnityEngine.Debug::LogError(object)
          //IL_0019:  nop
          //IL_001a:  ret
        var a = 100;
        var b = 200;
        var res = a + b;
        Debug.Log(res);
    }

    public static void LogSome()
    {
        Debug.Log("This Is Static Method.");
    }

    public static void LogNumber(int number)
    {
        Debug.Log(number);
    }
}

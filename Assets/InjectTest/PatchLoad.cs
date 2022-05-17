using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PatchLoad
{
    /// <summary>
    /// 根据方法名字存对应的补丁
    /// </summary>
    static Dictionary<string, object> _patchDic;

    static PatchLoad()
    {
        _patchDic = new Dictionary<string, object>();
    }

    public static void LoadPatch(string patchPath)
    {

    }

    public static bool HasPatch(string patchKey)
    {
        return true;
        return _patchDic.ContainsKey(patchKey);
    }

    public static object GetPatch(string patchKey)
    {
        _patchDic.TryGetValue(patchKey, out object patch);
        return patch;
    }
}

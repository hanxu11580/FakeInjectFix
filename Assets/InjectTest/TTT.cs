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

}

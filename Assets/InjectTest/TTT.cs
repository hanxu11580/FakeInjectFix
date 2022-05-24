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

        //Debug.LogError("!!!");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

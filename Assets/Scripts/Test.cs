using System.Collections;
using System.Collections.Generic;
using SimpleToolkits;
using UnityEngine;

public class Test : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        ConfigMgr.Init();
        var config = ConfigMgr.Get<ExampleConfig>("1001");
        Debug.Log(config.name);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

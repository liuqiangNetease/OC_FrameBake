#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using OC;
using OC.Profiler;

public class OCTest : MonoBehaviour
{

    SingleScene scene;

    public bool OC = true;
    // Use this for initialization
    void Start()
    {
        var name = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        scene = new SingleScene("", name, Index.InValidIndex);
        scene.Load();

    }

    // Update is called once per frame
    void Update()
    {
        

        if (OC)
        {
            OCProfiler.Start();
            for (int i=0; i< 1; i++)
                scene.DoCulling(Camera.main.transform.position);
            var time = OCProfiler.Stop();
            //Debug.Log(time);

        }
        else
        {
            scene.UndoCulling();
        }
    }
}
#endif

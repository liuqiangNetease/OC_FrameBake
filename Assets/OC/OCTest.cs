#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using OC;
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
        scene.UndoDisabledObjects();

        if (OC)
            scene.DoCulling(Camera.main.transform.position);
    }
}
#endif

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using OC;
public class OCTest : MonoBehaviour {

    SingleScene scene;

    public bool OC = true;
	// Use this for initialization
	void Start () {
        var name = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;      
        scene = new SingleScene("", name, Index.InValidIndex);
        scene.Load();
		
	}
	
	// Update is called once per frame
	void Update () {
      
        scene.UndoDisabledObjects();

        if(OC)
            scene.DoCulling(Camera.main.transform.position);

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Camera.main.transform.position += new Vector3(x * 0.1f, 0, z* 0.1f);
	}
}

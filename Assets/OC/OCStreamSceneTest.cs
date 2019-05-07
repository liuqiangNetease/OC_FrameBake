using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OCStreamSceneTest : MonoBehaviour {

    OC.MultiScene scene;

    public int tileSize = 10;
    public int tileDim = 8;

    public bool OC = true;
	// Use this for initialization
	void Start () {
        //var name = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        scene = new OC.MultiScene("Assets/Maps/maps/0001/Scenes", "002 {0}x{1}", tileDim, tileSize);
        //scene.TestLoadAll();
		
	}
	
	// Update is called once per frame
	void Update () {
      
        scene.UndoDisabledObjects();

        if(OC)
            scene.DoCulling(Camera.main.transform.position);

        //float x = Input.GetAxis("Horizontal");
        //float z = Input.GetAxis("Vertical");

        //Camera.main.transform.position += new Vector3(x * 0.1f, 0, z* 0.1f);
	}
}

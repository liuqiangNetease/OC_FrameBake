using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OC
{
    [Serializable]
    public class GameObjectID : MonoBehaviour
    {        
        [SerializeField]
        int _uid;

        public int GUID
        {
            get { return _uid; }
            set
            {
                _uid = value;
            }
        }

        public void Reset()
        {
            GUID = resetID;
        }

        public void Flag()
        {
            GUID = flagID;
        }

        public static int resetID = -2;
        public static int flagID = -1;
        
    }
}


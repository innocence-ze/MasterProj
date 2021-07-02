using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Singleton
    {
        get
        {
            if (singleton == null)
            {
                singleton = FindObjectOfType<GameManager>();
            }
            if (singleton == null)
            {
                Debug.LogError("Cannot find Game Manager");
            }
            return singleton;
        }
    }
    private static GameManager singleton = null;


}

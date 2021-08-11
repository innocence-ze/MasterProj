using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class GameManager : MonoBehaviour
{
    public Button generateButton, clearButton, quitButton;
    public TMP_InputField seedInput;
    int seedNum;

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

    private void Start()
    {
        generateButton.onClick.AddListener(Generate);
        clearButton.onClick.AddListener(Clear);
        quitButton.onClick.AddListener(Quit);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return))
            Generate();
    }


    void Generate()
    {
        int.TryParse(seedInput.text, out seedNum);
        TerrainManager.Singleton.Generate(seedNum);
    }

    void Clear()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    void Quit()
    {

    }
}

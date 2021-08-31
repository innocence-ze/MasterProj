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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return))
            Generate();
        if (Input.GetKeyDown(KeyCode.Escape))
            Quit();
    }


    public void Generate()
    {
        int.TryParse(seedInput.text, out seedNum);
        TerrainManager.Singleton.Generate(seedNum);
    }

    public void Clear()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void ShowBubble(GameObject bubble)
    {
        bubble.SetActive(true);
        StartCoroutine(HideBubble(bubble));
    }
    WaitForSeconds bubbleTimer = new WaitForSeconds(8);
    IEnumerator HideBubble(GameObject bubble)
    {
        yield return bubbleTimer;
        bubble.SetActive(false);
    }

}

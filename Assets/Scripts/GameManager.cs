using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{

    public bool isGameOver;
    public bool isLevelOver;
    // Start is called before the first frame update
    void Start()
    {
        isGameOver = false;
        isLevelOver = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public IEnumerator HandleGameOver()
    {
        Debug.Log("Game Over");
        isGameOver = true;

        yield return new WaitForSeconds(3);
        SceneManager.LoadScene("MainScene", LoadSceneMode.Additive);
    }

    public IEnumerator HandleLevelOver()
    {
        isLevelOver = true;
        yield return new WaitForSeconds(3);
        SceneManager.LoadScene("MainScene", LoadSceneMode.Single);
    }
}

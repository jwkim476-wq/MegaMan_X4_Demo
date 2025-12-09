using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    bool isStarting = false; // 중복 입력 방지용

    void Update()
    {
        // 이미 시작 중이면 입력 무시
        if (isStarting) return;

        // Z 키 또는 엔터(Return) 키를 누르면 시작
        if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Return))
        {
            GameStart();
        }

        // ESC 누르면 종료
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            GameExit();
        }
    }

    public void GameStart()
    {
        isStarting = true;
        
        SceneManager.LoadScene("Stage1-1");
    }

    public void GameExit()
    {
        Debug.Log("게임 종료!");
        Application.Quit();
    }
}
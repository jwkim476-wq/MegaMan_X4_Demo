using UnityEngine;
using UnityEngine.SceneManagement; // 씬 이동 기능

public class GameManager : MonoBehaviour
{
    public bool isGameOver = false;

    void Update()
    {
        // ESC 누르면 메인 메뉴로 나가기
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SceneManager.LoadScene("Main");
        }
    }

    // 플레이어가 죽었을 때 호출될 함수
    public void OnPlayerDead()
    {
        if (isGameOver) return;

        isGameOver = true;
        Debug.Log("게임 오버! 2초 뒤 재시작...");

        // 2초 뒤에 Restart 함수 실행
        Invoke("RestartStage", 2f);
    }

    void RestartStage()
    {
        // 현재 씬을 다시 로드 (재시작)
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
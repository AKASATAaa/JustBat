using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PX4SceneManager : MonoBehaviour
{
    [SerializeField] private string nextSceneName; // 다음으로 넘어갈 씬 이름
    [SerializeField] private int maxBallCount = 3; // 최대 생성 가능한 공의 수

    private int spawnedBallCount = 0; // 지금까지 생성된 공의 수
    private int hitBallCount = 0; // 타격된 공의 수
    private bool isGameEnding = false; // 게임 종료 진행 중 여부

    // 싱글톤 패턴 구현
    public static PX4SceneManager Instance { get; private set; }

    private void Awake()
    {
        ResetGame();
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 공 생성 요청 시 호출됨
    public bool CanSpawnBall()
    {
        // 최대 공 개수에 도달하지 않았다면 생성 허용
        return spawnedBallCount < maxBallCount;
    }

    // 공이 생성되었을 때 호출
    public void BallSpawned()
    {
        spawnedBallCount++;
        Debug.Log($"공 생성됨: {spawnedBallCount}/{maxBallCount}");
    }

    // 공이 타격되었을 때 호출
    public void BallHit()
    {
        hitBallCount++;
        Debug.Log($"공 타격됨: {hitBallCount}/{maxBallCount}");

        // 3번째 공이 타격되었고, 아직 게임 종료가 진행 중이 아니라면
        if (hitBallCount >= maxBallCount && !isGameEnding)
        {
            StartCoroutine(EndGameAfterDelay());
        }
    }

    // 게임 종료 및 씬 전환 처리
    private IEnumerator EndGameAfterDelay()
    {
        isGameEnding = true;
        Debug.Log($"게임 종료 - {nextSceneName} 씬으로 5초 후 이동합니다.");

        // 5초 대기
        yield return new WaitForSeconds(5f);

        // 다음 씬으로 이동
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogError("다음 씬 이름이 설정되지 않았습니다!");
        }
    }

    // 게임 재시작 시 초기화 (필요한 경우 사용)
    public void ResetGame()
    {
        spawnedBallCount = 0;
        hitBallCount = 0;
        isGameEnding = false;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TeeBall 생성 영역에 붙일 스크립트
public class TeeBallSpawner : MonoBehaviour
{
    [SerializeField] private GameObject teeBallPrefab; // TeeBall 프리팹
    [SerializeField] private Transform spawnPoint; // 생성 위치 (없으면 이 오브젝트 위치 사용)
    [SerializeField] private float checkInterval = 1.0f; // 확인 주기(초)
    [SerializeField] private bool spawnOnStart = true; // 시작 시 생성 여부

    private void Start()
    {
        // TeeBallArea 태그 확인
        if (!gameObject.CompareTag("TeeBallArea"))
        {
            Debug.LogWarning("TeeBallSpawner가 붙은 오브젝트에 'TeeBallArea' 태그가 필요합니다.");
            gameObject.tag = "TeeBallArea";
        }

        // Collider 확인
        if (GetComponent<Collider>() == null || !GetComponent<Collider>().isTrigger)
        {
            Debug.LogError("TeeBallSpawner에는 트리거로 설정된 Collider가 필요합니다!");
        }

        if (spawnOnStart)
        {
            SpawnTeeBall();
        }

        // 주기적으로 확인
        StartCoroutine(CheckTeeBallRoutine());
    }

    IEnumerator CheckTeeBallRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkInterval);

            // TeeBall 태그를 가진 오브젝트가 있는지 확인
            if (GameObject.FindGameObjectWithTag("TeeBall") == null)
            {
                SpawnTeeBall();
            }
        }
    }

    void SpawnTeeBall()
    {
        // 생성 위치 결정
        Vector3 position = (spawnPoint != null) ? spawnPoint.position : transform.position;
        Quaternion rotation = (spawnPoint != null) ? spawnPoint.rotation : Quaternion.identity;

        // TeeBall 생성
        GameObject newTeeBall = Instantiate(teeBallPrefab, position, rotation);
        newTeeBall.tag = "TeeBall"; // 태그 설정 (프리팹에 이미 설정되어 있지 않은 경우)

        // TeeBallBehavior 스크립트가 없으면 추가
        if (newTeeBall.GetComponent<TeeBallController>() == null)
        {
            newTeeBall.AddComponent<TeeBallController>();
        }

        Debug.Log("새 TeeBall 생성됨");
    }
}
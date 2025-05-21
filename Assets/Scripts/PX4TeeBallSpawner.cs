using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 간단한 TeeBall 생성 스크립트
public class PX4TeeBallSpawner : MonoBehaviour
{
    [SerializeField] private GameObject teeBallPrefab; // TeeBall 프리팹
    [SerializeField] private Transform spawnPoint; // 생성 위치 (없으면 이 오브젝트 위치 사용)
    [SerializeField] private float respawnDelay = 5.0f; // 재생성 대기 시간(초)
    [SerializeField] private bool spawnOnStart = true; // 시작 시 생성 여부

    private Collider triggerCollider; // 트리거 콜라이더
    private bool isRespawnScheduled = false; // 재생성 예약 여부

    private void Start()
    {
        // 트리거 콜라이더 확인
        triggerCollider = GetComponent<Collider>();
        if (triggerCollider == null)
        {
            Debug.LogError("TeeBallSpawner에 Collider가 없습니다!");
            return;
        }

        if (!triggerCollider.isTrigger)
        {
            Debug.LogWarning("Collider가 트리거로 설정되어 있지 않습니다. 트리거로 변경합니다.");
            triggerCollider.isTrigger = true;
        }

        // TeeBallArea 태그 확인
        if (!gameObject.CompareTag("TeeBallArea"))
        {
            Debug.LogWarning("TeeBallSpawner가 붙은 오브젝트에 'TeeBallArea' 태그가 필요합니다.");
            gameObject.tag = "TeeBallArea";
        }

        // 시작 시 공 생성
        if (spawnOnStart)
        {
            SpawnTeeBall();
        }

        // 정기적으로 공 존재 여부 확인
        InvokeRepeating("CheckForTeeBall", 1.0f, 1.0f);
    }

    // 트리거 영역 내에 티볼이 있는지 확인
    private void CheckForTeeBall()
    {
        if (isRespawnScheduled)
            return;

        bool foundTeeBall = false;

        // 트리거 영역 내에 TeeBall 태그를 가진 오브젝트가 있는지 확인
        Collider[] colliders = Physics.OverlapBox(
            triggerCollider.bounds.center,
            triggerCollider.bounds.extents,
            transform.rotation
        );

        foreach (Collider col in colliders)
        {
            if (col.CompareTag("TeeBall"))
            {
                foundTeeBall = true;
                break;
            }
        }

        // 트리거 영역 내에 티볼이 없으면 5초 후 생성 예약
        if (!foundTeeBall && !isRespawnScheduled)
        {
            isRespawnScheduled = true;
            Invoke("SpawnTeeBall", respawnDelay);
            Debug.Log($"티볼이 없음: {respawnDelay}초 후에 새 티볼 생성 예정");
        }
    }

    // TeeBall 생성
    private void SpawnTeeBall()
    {
        // 게임 매니저에게 공 생성 가능 여부 확인
        if (PX4SceneManager.Instance != null && !PX4SceneManager.Instance.CanSpawnBall())
        {
            Debug.Log("최대 공 개수에 도달하여 더 이상 생성하지 않습니다.");
            isRespawnScheduled = false;
            return;
        }

        // 생성 위치 결정
        Vector3 position = (spawnPoint != null) ? spawnPoint.position : transform.position;
        Quaternion rotation = (spawnPoint != null) ? spawnPoint.rotation : Quaternion.identity;

        // TeeBall 생성
        GameObject newTeeBall = Instantiate(teeBallPrefab, position, rotation);
        newTeeBall.tag = "TeeBall";

        // TeeBallController 스크립트가 없으면 추가
        if (newTeeBall.GetComponent<TeeBallController>() == null)
        {
            newTeeBall.AddComponent<TeeBallController>();
        }

        // 게임 매니저에 공 생성 알림
        if (PX4SceneManager.Instance != null)
        {
            PX4SceneManager.Instance.BallSpawned();
        }

        // 재생성 상태 초기화
        isRespawnScheduled = false;
        Debug.Log("새 TeeBall 생성됨");
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeeBallController : MonoBehaviour
{
    private Rigidbody rb;
    [SerializeField] private float destroyDelay = 5.0f; // 위치 벗어난 후 파괴까지 시간
    private bool isMarkedForDestroy = false;


    void Start()
    {
        // 리지드바디 참조 가져오기
        rb = GetComponent<Rigidbody>();

        // 리지드바디가 없으면 추가
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        // 태그 설정 확인
        if (!CompareTag("TeeBall"))
        {
            Debug.LogWarning("이 오브젝트의 태그가 'TeeBall'로 설정되어 있지 않습니다. 현재 태그: " + tag);
            gameObject.tag = "TeeBall";
        }

        // 초기 설정
        //rb.isKinematic = true;
        rb.useGravity = false;

        Debug.Log("티볼 초기화 완료: isKinematic=" + rb.isKinematic + ", useGravity=" + rb.useGravity);
    }

    // 배트 스크립트와 별도로 충돌 감지
    void OnCollisionEnter(Collision collision)
    {
        // 배트와 충돌했는지 확인
        if (collision.gameObject.CompareTag("Bat"))
        {
            Debug.Log("티볼: 배트와 충돌 감지됨!");

            // 직접 물리 속성 변경
            rb.isKinematic = false;
            rb.useGravity = true;

            Debug.Log("티볼: 물리 속성 변경 완료 - isKinematic=" + rb.isKinematic + ", useGravity=" + rb.useGravity);
        }
    }

    // 트리거 충돌도 체크
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Bat"))
        {
            Debug.Log("티볼: 배트와 트리거 충돌 감지됨!");

            // 직접 물리 속성 변경
            rb.isKinematic = false;
            rb.useGravity = true;

            Debug.Log("티볼: 물리 속성 변경 완료 - isKinematic=" + rb.isKinematic + ", useGravity=" + rb.useGravity);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // 지정된 영역(TeeBallArea)을 벗어났을 때
        if (other.CompareTag("TeeBallArea") && !isMarkedForDestroy)
        {
            isMarkedForDestroy = true;
            StartCoroutine(DestroyAfterDelay());
        }
    }

    // 디버깅용 업데이트
    void Update()
    {
        if (!rb.isKinematic && rb.velocity.magnitude > 0.1f)
        {
            // 티볼이 움직이고 있음을 확인
            Debug.Log("티볼 움직임 확인: 속도=" + rb.velocity.magnitude);
        }
    }

    IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(destroyDelay);

        // 여전히 파괴 대상이면 파괴
        if (isMarkedForDestroy)
        {
            Destroy(gameObject);
        }
    }
}
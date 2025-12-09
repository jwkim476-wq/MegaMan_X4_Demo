using UnityEngine;

public class OneShotEffect : MonoBehaviour
{
    void OnEnable() // Start 대신 OnEnable 사용 (켜질 때마다 실행)
    {
        Animator anim = GetComponent<Animator>();
        float destroyTime = 0.5f; // 기본 수명

        if (anim != null)
        {
            // 애니메이터가 초기화될 시간을 벌기 위해 프레임 끝까지 대기 후 실행할 수도 있지만,
            // 보통 Update 한 번 돌면 정보가 들어옵니다.
            // 가장 안전하게 클립 정보를 가져옵니다.
            if (anim.runtimeAnimatorController != null && anim.runtimeAnimatorController.animationClips.Length > 0)
            {
                destroyTime = anim.runtimeAnimatorController.animationClips[0].length;
            }
            else
            {
                // 클립 정보를 못 찾으면 현재 상태 정보 시도
                AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
                if (info.length > 0) destroyTime = info.length;
            }
        }

        // 애니메이션 길이만큼 뒤에 삭제 (오브젝트 풀이라면 여기서 Return to Pool 로직 사용)
        Destroy(gameObject, destroyTime);
    }
}
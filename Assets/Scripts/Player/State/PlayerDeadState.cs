using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDeadState : PlayerState
{
    public PlayerDeadState(Player player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        base.Enter();

        // 움직임 정지
        _player.SetVelocity(Vector2.zero);

        // 중력은 그대로 두고 싶으면 이 줄은 빼도 됨
        _player.Rb.gravityScale = 0f;

        // 사망 애니메이션 트리거
        _player.Animator.SetBool("IsDead", true);

        // 입력 무시: Player.Update에서 StateMachine.LogicUpdate만 돌고,
        // ReadInput은 계속 읽어도 DeadState에서 아무것도 안 하면 됨
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        // 여기서 사망 애니메이션 끝난 후 처리(씬 리로드 등)를
        // Animation Event로 호출하거나, 타이머로 처리할 수 있음
    }

    public override void Exit()
    {
        base.Exit();
        // 부활 같은 거 구현할 거면 여기서 다시 gravityScale 되돌리기
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHitState : PlayerState
{
    private float _hitDuration = 0.25f;
    private float _startTime;

    public PlayerHitState(Player player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        base.Enter();

        _startTime = Time.time;

        // 애니메이터 피격 플래그
        _player.Animator.SetBool("IsHurt", true);

        // 넉백 (맞은 방향 반대쪽으로 튕김)
        float dir = -Mathf.Sign(_player.transform.localScale.x);
        Vector2 knockback = new Vector2(dir * 5f, 8f);
        _player.Rb.velocity = knockback;
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        // 피격 시간 끝나면 원래 상태로 복귀
        if (Time.time >= _startTime + _hitDuration)
        {
            _player.Animator.SetBool("IsHurt", false);

            if (_player.IsGrounded)
            {
                if (Mathf.Abs(_player.XInput) > 0.01f)
                    _stateMachine.ChangeState(_player.RunState);
                else
                    _stateMachine.ChangeState(_player.IdleState);
            }
            else
            {
                _stateMachine.ChangeState(_player.InAirState);
            }
        }
    }
}
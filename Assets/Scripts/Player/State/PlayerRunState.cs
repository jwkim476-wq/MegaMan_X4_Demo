using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRunState : PlayerState
{
    public PlayerRunState(Player player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        // 이동
        float targetSpeed = _player.XInput * _player.MoveSpeed;
        _player.SetHorizontalVelocity(targetSpeed);

        // 방향에 따라 스프라이트 반전
        if (Mathf.Abs(_player.XInput) > 0.01f)
        {
            Vector3 scale = _player.transform.localScale;
            scale.x = Mathf.Sign(_player.XInput) * Mathf.Abs(scale.x);
            _player.transform.localScale = scale;
        }

        // 입력 없으면 Idle
        if (Mathf.Abs(_player.XInput) < 0.01f)
        {
            _stateMachine.ChangeState(_player.IdleState);
            return;
        }

        // 점프
        if (_player.IsJumpPressed && _player.IsGrounded)
        {
            _player.SetVelocity(new Vector2(_player.Rb.velocity.x, _player.JumpForce));
            _stateMachine.ChangeState(_player.InAirState);
            return;
        }

        // 대쉬
        if (_player.IsDashPressed && _player.IsGrounded && _player.IsDashAvailable)
        {
            _stateMachine.ChangeState(_player.DashState);
            return;
        }

        // 땅에서 떨어지면 공중 상태로
        if (!_player.IsGrounded)
        {
            _stateMachine.ChangeState(_player.InAirState);
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerIdleState : PlayerState
{
    public PlayerIdleState(Player player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        base.Enter();
        _player.SetHorizontalVelocity(0f);
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        // 왼/오 입력 있으면 Run
        if (Mathf.Abs(_player.XInput) > 0.01f)
        {
            _stateMachine.ChangeState(_player.RunState);
            return;
        }

        // 점프
        if (_player.IsJumpPressed && _player.IsGrounded)
        {
            _player.SetVelocity(new Vector2(0f, _player.JumpForce));
            _stateMachine.ChangeState(_player.InAirState);
            return;
        }

        // 대쉬
        if (_player.IsDashPressed && _player.IsGrounded && _player.IsDashAvailable)
        {
            _stateMachine.ChangeState(_player.DashState);
            return;
        }

        // 떨어지기 시작하면 InAir
        if (!_player.IsGrounded)
        {
            _stateMachine.ChangeState(_player.InAirState);
        }
    }
}
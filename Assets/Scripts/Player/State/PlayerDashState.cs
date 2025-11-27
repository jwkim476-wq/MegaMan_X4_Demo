using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDashState : PlayerState
{
    private float _startTime;
    private bool _isHoldingDashButton;

    public PlayerDashState(Player player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        base.Enter();

        _startTime = Time.time;
        _player.ConsumeDash();

        // 바라보는 방향 기준
        float dir = Mathf.Sign(_player.transform.localScale.x);
        _player.SetVelocity(new Vector2(dir * _player.DashSpeed, 0f));
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        float dir = Mathf.Sign(_player.transform.localScale.x);
        _isHoldingDashButton = Input.GetKey(KeyCode.LeftShift);

        // 1) 대쉬 중 점프 → 대쉬 점프
        if (_player.IsJumpPressed)
        {
            Vector2 jumpVelocity = new Vector2(
                dir * _player.DashSpeed * 0.9f,
                _player.JumpForce * 1.1f
            );

            _player.SetVelocity(jumpVelocity);
            _stateMachine.ChangeState(_player.InAirState);
            return;
        }

        // 2) 대쉬 버튼을 떼면 즉시 종료
        if (!_isHoldingDashButton)
        {
            ExitDashToGroundState();
            return;
        }

        // 3) 땅 떠나면 공중상태로
        if (!_player.IsGrounded)
        {
            _stateMachine.ChangeState(_player.InAirState);
            return;
        }

        // 4) 유지
        _player.SetVelocity(new Vector2(dir * _player.DashSpeed, 0f));
    }

    private void ExitDashToGroundState()
    {
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
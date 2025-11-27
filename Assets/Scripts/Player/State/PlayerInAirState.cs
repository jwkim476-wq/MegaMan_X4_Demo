using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInAirState : PlayerState
{
    public PlayerInAirState(Player player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        // 공중에서 좌우 조작
        float targetSpeed = _player.XInput * _player.AirMoveSpeed;
        _player.SetHorizontalVelocity(targetSpeed);

        // 방향 반전
        if (Mathf.Abs(_player.XInput) > 0.01f)
        {
            Vector3 scale = _player.transform.localScale;
            scale.x = Mathf.Sign(_player.XInput) * Mathf.Abs(scale.x);
            _player.transform.localScale = scale;
        }

        // 천장에 닿으면 위로 더 못가게
        _player.StopVerticalIfCeilingHit();

        // 벽 슬라이드 조건: 벽에 닿아있고, 땅 아님, 아래로 떨어지는 중
        if (_player.IsTouchingWall && !_player.IsGrounded && _player.Rb.velocity.y < 0f)
        {
            _stateMachine.ChangeState(_player.WallSlideState);
            return;
        }

        // 착지
        if (_player.IsGrounded)
        {
            if (Mathf.Abs(_player.XInput) > 0.01f)
                _stateMachine.ChangeState(_player.RunState);
            else
                _stateMachine.ChangeState(_player.IdleState);
        }
    }
}
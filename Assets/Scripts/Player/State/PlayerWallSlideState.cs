using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWallSlideState : PlayerState
{
    private float _maxSlideSpeed = -2.5f;

    public PlayerWallSlideState(Player player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        base.Enter();
        // 벽 붙었을 때 x 속도 살짝 줄이기
        _player.SetHorizontalVelocity(0f);
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        // 아래로 너무 빨리 떨어지지 않게 속도 제한
        Vector2 vel = _player.Rb.velocity;
        if (vel.y < _maxSlideSpeed)
        {
            vel.y = _maxSlideSpeed;
            _player.SetVelocity(vel);
        }

        // 벽 점프
        if (_player.IsJumpPressed)
        {
            // 벽 반대 방향으로 점프
            float jumpDir = -_player.WallDirectionX;
            Vector2 jumpVelocity = new Vector2(jumpDir * _player.MoveSpeed, _player.JumpForce);
            _player.SetVelocity(jumpVelocity);

            // 방향 반전
            Vector3 scale = _player.transform.localScale;
            scale.x = Mathf.Sign(jumpDir) * Mathf.Abs(scale.x);
            _player.transform.localScale = scale;

            _stateMachine.ChangeState(_player.InAirState);
            return;
        }

        // 더 이상 벽에 안 붙어 있거나 바닥에 닿으면 전환
        if (!_player.IsTouchingWall)
        {
            _stateMachine.ChangeState(_player.InAirState);
            return;
        }

        if (_player.IsGrounded)
        {
            if (Mathf.Abs(_player.XInput) > 0.01f)
                _stateMachine.ChangeState(_player.RunState);
            else
                _stateMachine.ChangeState(_player.IdleState);
        }
    }
}
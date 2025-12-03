using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform _target;   // 따라갈 대상 (Player)

    [Header("Offset")]
    [SerializeField] private Vector2 _offset = new Vector2(0f, 1f);

    [Header("Follow")]
    [SerializeField] private float _smoothTime = 0.15f; // 숫자 클수록 더 늦게 따라옴


    [SerializeField] private bool _lockY = false;
    [SerializeField] private float _fixedY = 0f;

    private Vector3 _currentVelocity;

    private void LateUpdate()
    {
        if (_target == null) return;

        float targetX = _target.position.x + _offset.x;
        float targetY = _target.position.y + _offset.y;

        if (_lockY)
        {
            targetY = _fixedY;
        }

        // 카메라의 Z는 그대로 유지
        Vector3 targetPos = new Vector3(targetX, targetY, transform.position.z);

        // 부드럽게 따라오기
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPos,
            ref _currentVelocity,
            _smoothTime
        );
    }

    
    private void Reset()
    {
        if (_target == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
                _target = player.transform;
        }
    }
}
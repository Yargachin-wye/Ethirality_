using System;
using System.Collections;
using System.Collections.Generic;
using CharacterComponents;
using Definitions;
using UniRx;
using UniRxEvents.GamePlay;
using UnityEngine;

public class DeadZone : MonoBehaviour
{
    [SerializeField] private float dmgDelay = 1;
    private float _dmgTimer;
    private Character _player;
    private bool _isPlayerOutside;

    private void Awake()
    {
        _dmgTimer = dmgDelay;
    }

    private void FixedUpdate()
    {
        if (!_isPlayerOutside) return;
        _dmgTimer -= Time.fixedDeltaTime;
        if (_dmgTimer <= 0)
        {
            _dmgTimer = dmgDelay;
            _player.Stats.Damage(1);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponent<Character>();
        if (player != null && player == _player)
        {
            _isPlayerOutside = false;
            MessageBroker.Default.Publish(new PlayerInDeadZoneEvent { IsDeadZone = false });
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var player = other.GetComponent<Character>();
        if (player != null && player.Fraction == Fraction.Player)
        {
            _isPlayerOutside = true;
            _player = player;
            MessageBroker.Default.Publish(new PlayerInDeadZoneEvent { IsDeadZone = true });
        }
    }
}
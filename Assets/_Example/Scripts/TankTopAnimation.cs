using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class TankTopAnimation : ActorComponent<ActorTank>
{
	private static int TRIGGER_FIRE = Animator.StringToHash("Fire");
	private Animator m_animator;

	void Awake()
	{
		m_animator = GetComponent<Animator>();
	}

	void OnEnable()
	{
		actor.OnWeaponFired += OnWeaponFired;
	}

	void OnDisable()
	{
		actor.OnWeaponFired += OnWeaponFired;
	}

	private void OnWeaponFired(ActorTank obj)
	{
		m_animator.SetTrigger(TRIGGER_FIRE);
	}
}

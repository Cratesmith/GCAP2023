using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class ActorEnemyTank : ActorTank 
{
	[SerializeField] float m_tooCloseDistance = 2f;
	[SerializeField] private float m_tooFarDistance = 4f;
	[SerializeField] private float m_weaponFireFrequency = 3f;
	public static event System.Action<ActorEnemyTank> OnEnemyTankDestroyed;

	void OnEnable()
	{
		StartCoroutine(WeaponFireCoroutine());
	}

	private IEnumerator WeaponFireCoroutine()
	{
		yield return new WaitForSeconds((Random.value+1)/2f  * m_weaponFireFrequency);
		while (true)
		{
			yield return new WaitForSeconds(m_weaponFireFrequency);
			FireWeapon();
		}
	}

	void Update()
	{
		var target = ActorPlayerTank.current;
		if (target == null)
		{
			moveInput = Vector3.zero;
			aimInput = Vector3.zero;
			return;
		}

		var diff = target.transform.position - transform.position;
		diff.y = 0;
		var dist = diff.magnitude;
		var dir = (dist > Mathf.Epsilon) ? (diff / dist):(transform.forward);
		
		aimInput = dir;

		if (dist <= m_tooCloseDistance)
		{
			moveInput = -dir;
		}
		else if (dist >= m_tooFarDistance)
		{
			moveInput = dir;
		}
		else
		{
			moveInput = Vector3.zero;
		}
	}

	protected override void OnDestroyed()
	{
		base.OnDestroyed();
		if (OnEnemyTankDestroyed != null)
		{
			OnEnemyTankDestroyed(this);
		}
	}
}

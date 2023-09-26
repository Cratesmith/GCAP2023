using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

[RequireComponent(typeof(Rigidbody))]
public class ActorTank : Actor 
{
	[SerializeField] GameObject					m_baseModelPrefab;
	[SerializeField] GameObject					m_topModelPrefab;
	[SerializeField] Transform					m_baseModelRoot;
	[SerializeField] Transform					m_topModelRoot;
	[SerializeField] Transform					m_topPivot;
	[SerializeField] private ActorProjectile	m_weaponProjectilePrefab;
	[SerializeField] Transform					m_weaponFirePoint;
	[SerializeField] float						m_weaponFireSpeed = 10f;
	[SerializeField] float						m_weaponLifespan = 3f;
	[SerializeField] float						m_velocityBlend = 10f;
	[SerializeField] float						m_speed = 5f;
	[SerializeField] float						m_rotateBlend = 5f;
	[SerializeField] int						m_startingHealth = 3;
	[SerializeField] Effect						m_takeDamageEffectPrefab;
	[SerializeField] Effect						m_destroyedEffectPrefab;

	public Vector3 moveInput { get; protected set; }
	public Vector3 aimInput	 { get; protected set; }
	public int health		 { get; protected set; }

	public event System.Action<ActorTank> OnWeaponFired;

	Rigidbody   m_rigidbody;
	GameObject  m_baseModel;
	GameObject  m_topModel;

	protected virtual void Awake()
	{
		m_rigidbody = GetComponent<Rigidbody>(); 
		Assert.IsNotNull(m_rigidbody);       
		m_rigidbody.useGravity = false;
		m_rigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;

		Assert.IsNotNull(m_baseModelPrefab);       
		Assert.IsNotNull(m_topModelPrefab);       
		Assert.IsNotNull(m_baseModelRoot);       
		Assert.IsNotNull(m_topModelRoot);       
		Assert.IsNotNull(m_weaponFirePoint);       
		Assert.IsNotNull(m_weaponProjectilePrefab);       

		m_baseModel = Instantiate(m_baseModelPrefab, m_baseModelRoot);
		m_baseModel.transform.localPosition = Vector3.zero;
		m_baseModel.transform.localRotation = Quaternion.identity;
		m_baseModel.transform.localScale = Vector3.one;

		m_topModel = Instantiate(m_topModelPrefab, m_topModelRoot);
		m_topModel.transform.localPosition = Vector3.zero;
		m_topModel.transform.localRotation = Quaternion.identity;
		m_topModel.transform.localScale = Vector3.one;
	}

	protected virtual void Start()
	{
		health = m_startingHealth;
	}

	protected virtual void FixedUpdate()
	{
		var desiredVelocity = Vector3.ClampMagnitude(moveInput, 1f)*m_speed;
		desiredVelocity.y = 0f;

		if(desiredVelocity.sqrMagnitude > 0.01f)
		{
			// reverse our desired direction if there's less of a turn to have the treads running in reverse
			// (this stops tanks doing 180 degree turns to reverse)
			var lookDirection = (aimInput.sqrMagnitude > 0f && Vector3.Angle(transform.forward, desiredVelocity) < 90)
				? desiredVelocity
				: -desiredVelocity;

			transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDirection), m_rotateBlend*Time.deltaTime);
		}

		if (aimInput.sqrMagnitude > 0.01f)
		{            
			m_topPivot.rotation = Quaternion.Slerp(m_topPivot.rotation, Quaternion.LookRotation(aimInput), m_rotateBlend*Time.deltaTime);
		}

		var newVelocity = Vector3.Lerp(m_rigidbody.velocity, desiredVelocity, m_velocityBlend*Time.fixedDeltaTime);
		newVelocity.y = m_rigidbody.velocity.y;

		m_rigidbody.velocity = newVelocity;
	}

	protected virtual void OnDrawGizmos()
	{
		GizmoUtilities.DrawPrefab(m_baseModelPrefab, m_baseModelRoot, Vector3.zero, Quaternion.identity, Vector3.one);
		GizmoUtilities.DrawPrefab(m_topModelPrefab, m_topModelRoot, Vector3.zero, Quaternion.identity, Vector3.one);
	}

	public void FireWeapon()
	{
		var projectileInstance = Instantiate(m_weaponProjectilePrefab, m_weaponFirePoint.position, m_weaponFirePoint.rotation);
		projectileInstance.Setup(this, m_weaponFireSpeed, m_weaponLifespan);
		if (OnWeaponFired != null)
		{
			OnWeaponFired(this);
		}
	}

	public void ApplyDamage()
	{
		health -= 1;
		if (health <= 0f)
		{
			Effect.Spawn(m_destroyedEffectPrefab, transform.position, transform.rotation);
			OnDestroyed();
			Destroy(gameObject);
		}
		else
		{
			Effect.Spawn(m_takeDamageEffectPrefab, transform.position, transform.rotation, transform);
		}
	}

	protected virtual void OnDestroyed()
	{
	}
}

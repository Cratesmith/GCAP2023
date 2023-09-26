using UnityEngine;
using UnityEngine.Assertions;

[RequireComponent(typeof(Collider), typeof(SphereCollider))]
[RequireComponent(typeof(Rigidbody))]
public class ActorProjectile : Actor
{
	[SerializeField] GameObject m_modelPrefab;
	[SerializeField] Effect		m_effectPrefab;
	[SerializeField] Effect		m_hitEffectPrefab;
	[SerializeField] Effect		m_fireEffectPrefab;

	Rigidbody			m_rigidbody;
	private GameObject	m_modelInstance;
	private Actor		m_owner;
	private Effect		m_projectileEffect;

	void Awake()
	{
		m_rigidbody = GetComponent<Rigidbody>(); 
		Assert.IsNotNull(m_rigidbody);       
		m_rigidbody.useGravity = false;
		m_rigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
		m_rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
		m_rigidbody.mass = float.MinValue; // effectively zero mass so ContinuousDynamic collision detection is used but doesn't knock things back

		Assert.IsNotNull(m_modelPrefab);
		m_modelInstance = Instantiate(m_modelPrefab, transform);
		m_modelInstance.transform.localPosition = Vector3.zero;
		m_modelInstance.transform.localRotation = Quaternion.identity;
		m_modelInstance.transform.localScale = Vector3.one;
	}

	void Start()
	{
		Effect.Spawn(m_fireEffectPrefab, transform.position, transform.rotation);
		m_projectileEffect = Effect.Spawn(m_effectPrefab, transform.position, transform.rotation, transform);
	}

	void OnDrawGizmos()
	{
		GizmoUtilities.DrawPrefab(m_modelPrefab, transform, Vector3.zero, Quaternion.identity, Vector3.one);
	}

	public void Setup(Actor owner, float speed, float lifeTime)
	{
		m_rigidbody.velocity = transform.forward * speed;
		m_owner = owner;
		Destroy(gameObject, lifeTime);

		foreach (var colliderA in GetComponentsInChildren<Collider>())
		foreach (var colliderB in m_owner.GetComponentsInChildren<Collider>())
		{
			Physics.IgnoreCollision(colliderA, colliderB, true);
		}
	}

	void OnCollisionEnter(Collision col)
	{
		Destroy(gameObject);

		Effect.Spawn(m_hitEffectPrefab, transform.position, transform.rotation);

		var otherTank = col.gameObject.GetComponentInParent<ActorTank>();
		if (otherTank)
		{
			otherTank.ApplyDamage();
		}
	}
}

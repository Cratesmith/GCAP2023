using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SceneControllerHoardGame : Actor
{
	[SerializeField] private Text m_scoreText;
	[SerializeField] private Text m_timeText;
	[SerializeField] private Text m_healthText;
	[SerializeField] private ActorPlayerTank m_playerPrefab;
	[SerializeField] private Actor m_enemyPrefab;
	[SerializeField] private AnimationCurve m_spawnRateVsTime = AnimationCurve.Linear(0, 10, 30, 1);
	[SerializeField] private float m_minSpawnRadius = 10f;
	[SerializeField] private float m_maxSpawnRadius = 25f;
	
	private int m_score;
	private float m_startTime;

	void Awake()
	{
		Assert.IsNotNull(m_scoreText);
		Assert.IsNotNull(m_timeText);
		Assert.IsNotNull(m_healthText);
		Assert.IsNotNull(m_playerPrefab);
		Assert.IsNotNull(m_enemyPrefab);
	}

	void Start()
	{
		StartCoroutine(GameCoroutine());
	}

	private IEnumerator GameCoroutine()
	{		
		while (true)
		{
			var enemies = new List<Actor>();

			m_startTime = Time.time;
			if (!ActorPlayerTank.current)
			{
				Instantiate(m_playerPrefab, Vector3.zero, Quaternion.identity);
			}

			m_score = 0;
			ActorEnemyTank.OnEnemyTankDestroyed += OnEnemyTankDestroyed;

			while (ActorPlayerTank.current != null)
			{
				var angle = Random.Range(0, 360);
				var rotation = Quaternion.AngleAxis(angle, Vector3.up);

				var distance = Random.Range(m_minSpawnRadius, m_maxSpawnRadius);
				var position = rotation * Vector3.forward * distance;

				enemies.Add(Instantiate(m_enemyPrefab, position, Quaternion.Inverse(rotation)));

				var timeElapsed = Time.time - m_startTime;
				yield return new WaitForSeconds(m_spawnRateVsTime.Evaluate(timeElapsed));
			}

			ActorEnemyTank.OnEnemyTankDestroyed -= OnEnemyTankDestroyed;
			foreach (var enemy in enemies)
			{
				if (enemy != null)
				{
					Destroy(enemy.gameObject);
				}
			}
		}		

	}

	void Update()
	{
		m_scoreText.text = "Score " + m_score;
		if (ActorPlayerTank.current != null)
		{
		
			m_healthText.enabled = true;
			m_healthText.text = "Health " + ActorPlayerTank.current.health;
			m_timeText.text = "Time " + Mathf.Floor(Time.time - m_startTime);
		}
	}

	private void OnEnemyTankDestroyed(ActorEnemyTank obj)
	{
		m_score += 10;
	}

	void OnDrawGizmos()
	{
	#if UNITY_EDITOR
		Handles.color = Color.blue;
		Handles.DrawWireArc(transform.position, Vector3.up, Vector3.forward, 360f, m_minSpawnRadius);
		Handles.color = Color.cyan;
		Handles.DrawWireArc(transform.position, Vector3.up, Vector3.forward, 360f, m_maxSpawnRadius);
	#endif
	}
}

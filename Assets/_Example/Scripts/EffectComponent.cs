using UnityEngine;

public abstract class EffectComponent : ActorComponent<Effect> 
{
	// is this sub effect still going? 
	public abstract bool isPlaying { get; }

	// stop this effect
	public abstract void Stop();

	// quick lookup for parent
	public Transform parent {get { return actor.parent; } }

	void OnEnable()
	{
		actor.AddEffectComponent(this);
	}

	void OnDisable()
	{
		actor.RemoveEffectComponent(this);
	}
}

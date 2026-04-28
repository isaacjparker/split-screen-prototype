using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public partial class ThreatTable : Node
{
	private readonly Dictionary<ActorCore, float> _threats = new Dictionary<ActorCore, float>();

	private readonly List<ActorCore> _keysBuffer = new List<ActorCore>();

	public void AddThreat(ActorCore source, float amount)
	{
		if (source == null) return;

		if (_threats.ContainsKey(source))
		{
			_threats[source] += amount;
		}
		else
		{
			_threats[source] = amount;
		}
	}

	public float GetThreat(ActorCore source)
	{
		if (source != null && _threats.TryGetValue(source, out float value))
		{
			return value;
		}

		return 0f;
	}

	public float GetHighestThreat()
	{
		if (_threats.Count == 0) return 0f;

		return _threats.Values.Max();
	}

	// This may get computationally heavy later if there are a lot of
	// Agents in scene at one time: List creation every frame +
	// foreach loop every frame + valid instance check every frame.
	public void Decay(float delta, float decayRate)
	{
		if (_threats.Count == 0) return;

		_keysBuffer.Clear();
		_keysBuffer.AddRange(_threats.Keys);

		foreach (ActorCore key in _keysBuffer)
		{
			if (!Node.IsInstanceValid(key))
			{
				_threats.Remove(key);
				continue;
			}

			_threats[key] -= decayRate * delta;

			if (_threats[key] <= 0f)
			{
				_threats.Remove(key);
			}
				
		}
	}

	public void Clear()
	{
		_threats.Clear();
	}
}

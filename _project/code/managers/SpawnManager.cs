using Godot;
using System;
using System.Collections.Generic;

public partial class SpawnManager : Node
{
	[Export] public PackedScene EnemyTemplate;
	[Export] public Area3D SpawnArea;
	[Export] public float SpawnFrequency = 3f;
	[Export] public int MaxEnemies = 5;

	private List<ActorCore> CurrentEnemies;
	private float _spawnTimer;

	public event Action<ActorCore> OnEnemySpawned;
	public void RaiseEnemySpawned(ActorCore core) => OnEnemySpawned?.Invoke(core);


	public override void _Ready()
	{
		CurrentEnemies = new List<ActorCore>();
		_spawnTimer = SpawnFrequency;
	}

	public override void _Process(double delta)
	{
		CleanupSpawnList();

		if (CurrentEnemies.Count >= MaxEnemies) return;

		_spawnTimer -= (float) delta;

		if (_spawnTimer <= 0f)
		{
			_spawnTimer = SpawnFrequency;
			Vector3 spawnPos = GetRandomBoundaryPoint();
			SpawnEnemy(spawnPos);
		}
	}

	private void SpawnEnemy(Vector3 position)
	{
		if (EnemyTemplate == null) return;

		ActorCore spawnedEnemy = EnemyTemplate.Instantiate() as ActorCore;
		
		if (spawnedEnemy == null)
		{
			GD.PrintErr("SpawnManager: EnemyTemplate root node is not an ActorCore.");
			return;
		}

		// Position actor before adding to tree to avoid physics issues.
		spawnedEnemy.Position = position;
		AddChild(spawnedEnemy);
		

		CurrentEnemies.Add(spawnedEnemy);
		spawnedEnemy.OnDeath += OnEnemyDeath;

		RaiseEnemySpawned(spawnedEnemy);
	}

	private void OnEnemyDeath(ActorCore core)
	{
		if (!CurrentEnemies.Contains(core)) return;

		core.OnDeath -= OnEnemyDeath;
		CurrentEnemies.Remove(core);

		// Despawn after delay
		Tween tween = CreateTween();
		tween.TweenInterval(20.0f);			// TODO: Magic number - replace
		tween.TweenCallback(Callable.From(() =>
		{
			if (IsInstanceValid(core))
				core.QueueFree();
		}));
	}

	private Vector3 GetRandomBoundaryPoint()
	{
		if (SpawnArea == null)
		{
			GD.Print("SpawnManager: SpawnArea is null. Spawning at Vector3.Zero.");
			return Vector3.Zero;
		}

		CollisionShape3D collisionShape = SpawnArea.GetNode<CollisionShape3D>("CollisionShape3D");
		if (collisionShape?.Shape is not BoxShape3D box) return Vector3.Zero;

		Vector3 halfExtents = box.Size / 2f;
		Vector3 center = collisionShape.GlobalPosition;

		float edgeX = halfExtents.X * 2f;
		float edgeZ = halfExtents.Z * 2f;
		float perimeter = 2f * (edgeX + edgeZ);
		float point = (float)GD.RandRange(0, perimeter);

		float x, z;

		if (point < edgeX)
		{
			// Top edge (+Z)
			x = Mathf.Lerp(-halfExtents.X, halfExtents.X, point / edgeX);
			z = halfExtents.Z;
		}
		else if (point < edgeX + edgeZ)
		{
			// Right edge (+X)
			x = halfExtents.X;
			z = Mathf.Lerp(halfExtents.Z, -halfExtents.Z, (point - edgeX) / edgeZ);
		}
		else if (point < 2f * edgeX + edgeZ)
		{
			// Bottom edge (-Z)
			x = Mathf.Lerp(halfExtents.X, -halfExtents.X, (point - edgeX - edgeZ) / edgeX);
			z = -halfExtents.Z;
		}
		else
		{
			// Left edge (-X)
			x = -halfExtents.X;
			z = Mathf.Lerp(-halfExtents.Z, halfExtents.Z, (point - 2f * edgeX - edgeZ) / edgeZ);
		}

		return center + new Vector3(x, 0f, z);
	}

	/// <summary>
	/// Removes list entries where the node has been freed externally
	/// </summary>
	private void CleanupSpawnList()
	{
		CurrentEnemies.RemoveAll(e => !IsInstanceValid(e));
	}
}

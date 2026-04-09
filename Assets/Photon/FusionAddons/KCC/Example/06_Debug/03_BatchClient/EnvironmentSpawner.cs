namespace Example.BatchClient
{
	using System;
	using UnityEngine;

	public sealed class EnvironmentSpawner : MonoBehaviour
	{
		public float      MapSize         = 1024.0f;
		public float      ColliderSize    = 32.0f;
		public float      OverlapSize     = 0.0f;
		public float      VerticalOffset  = 0.0f;
		public GameObject GroundPrefab;
		public Obstacle[] Obstacles;

		private void Awake()
		{
			ApplicationUtility.GetCommandLineArgument("-seed", out int seed);
			System.Random random = new System.Random(seed);

			Transform parent = transform;
			parent.position = new Vector3(0.0f, -0.5f, 0.0f);

			int   colliders = 0;
			int   obstacles = 0;
			float mapExtent = MapSize * 0.5f;
			float offset    = ColliderSize - OverlapSize;

			for (float z = -mapExtent + ColliderSize * 0.5f; z < mapExtent; z += offset)
			{
				for (float x = -mapExtent + ColliderSize * 0.5f; x < mapExtent; x += offset)
				{
					Vector3 spawnPosition = new Vector3(x, -0.5f + GetRandom(random, -VerticalOffset, VerticalOffset), z);
					GameObject groundGameObject = GameObject.Instantiate(GroundPrefab, spawnPosition, Quaternion.identity, parent);
					groundGameObject.transform.localScale = new Vector3(ColliderSize, 1.0f, ColliderSize);

					++colliders;
				}
			}

			for (int j = 0; j < Obstacles.Length; ++j)
			{
				Obstacle obstacle = Obstacles[j];
				if (obstacle.IsEnabled == true)
				{
					for (int i = 0; i < obstacle.Count; ++i)
					{
						Vector3 spawnPosition = Quaternion.Euler(0.0f, GetRandom(random, 0.0, 360.0), 0.0f) * Vector3.forward * GetSqrtRandom(random, 0.0, mapExtent);
						GameObject obstacleGameObject = GameObject.Instantiate(obstacle.Prefab, spawnPosition, Quaternion.identity, parent);

						Vector3 scale;
						scale.x = GetRandom(random, obstacle.Size.x, obstacle.Size.y);
						scale.y = obstacle.IsUniform == true ? scale.x : GetRandom(random, obstacle.Size.x, obstacle.Size.y);
						scale.z = obstacle.IsUniform == true ? scale.x : GetRandom(random, obstacle.Size.x, obstacle.Size.y);

						obstacleGameObject.transform.localScale = scale;

						++obstacles;
					}
				}
			}

			Debug.LogWarning($"Generated {colliders} ground colliders and {obstacles} obstacles.");
		}

		private static float GetRandom(System.Random random, double min, double max)
		{
			double range = max - min;
			return (float)(min + random.NextDouble() * range);
		}

		private static float GetSqrtRandom(System.Random random, double min, double max)
		{
			double range = max - min;
			double randomNumber = random.NextDouble();
			return randomNumber > 0.000000000001 ? (float)(min + Math.Sqrt(randomNumber) * range) : 0.0f;
		}

		[Serializable]
		public sealed class Obstacle
		{
			public bool       IsEnabled;
			public GameObject Prefab;
			public int        Count;
			public Vector2    Size = Vector2.one;
			public bool       IsUniform;
		}
	}
}

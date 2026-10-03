using System.Collections.Generic;
using UnityEngine;

public class MapGeneration : MonoBehaviour
{
    // Configs
    [SerializeField] private List<GameObject> _roadPrefabs;
    [SerializeField] private List<GameObject> _bombPrefabs;
    [SerializeField] private List<GameObject> _buildingPrefabs;
    [SerializeField] private List<GameObject> _carPrefabs;

    [SerializeField] private float _buildingWidth = 10f;
    [SerializeField] private float _roadWidth = 8f;
    [SerializeField] private float _roadLength = 20f;

    [SerializeField] private float _bombChance = 0.2f;

    [SerializeField] private int _initialSegments = 10;

    [SerializeField] private float _spawnDistance = 150f; // how far ahead it will spawn in buildings
    [SerializeField] private float _destroyDistance = 20f; // required distance to destroy behind player

    public float GenerationIncline = 15f;

    public Transform PlayerTransform;

    // Data
    private List<GameObject> _activeBuilings;
    private List<GameObject> _activeRoads;
    private List<GameObject> _activeBombs;

    private Vector3 _nextRoadSpawnPoint = Vector3.zero;
    private float _leftBuildingZOffset = 0f;
    private float _rightBuildingZOffset = 0f;

    private void Start()
    {
        _activeBuilings = new List<GameObject>();
        _activeRoads = new List<GameObject>();
        _activeBombs = new List<GameObject>();

        // Generate initial segments if player transform is available
        if (PlayerTransform == null) return;

        for (int i = 0; i < _initialSegments; i++)
        {
            SpawnNextSegment();
        }
    }

    private void Update()
    {
        if (PlayerTransform == null) return;

        // Keep generating road & buildings ahead up to _spawnDistance
        if (Vector3.Distance(PlayerTransform.position, _nextRoadSpawnPoint) < _spawnDistance)
        {
            SpawnNextSegment();
        }

        CleanupBehindPlayer();
    }

    private void SpawnNextSegment()
    {
        if (_roadPrefabs == null || _roadPrefabs.Count == 0) return;

        Quaternion slopeRotation = Quaternion.Euler(GenerationIncline, 0f, 0f);

        // 1. Spawn Road Segment
        GameObject selectedRoad = _roadPrefabs[Random.Range(0, _roadPrefabs.Count)];
        GameObject roadInstance = Instantiate(selectedRoad, _nextRoadSpawnPoint, slopeRotation, transform);
        _activeRoads.Add(roadInstance);

        // 2. Spawn Side Buildings (Left & Right)
        SpawnBuildingsAlongSide(isLeft: true, slopeRotation);
        SpawnBuildingsAlongSide(isLeft: false, slopeRotation);

        // 3. Spawn Bombs / Obstacles on Road
        TrySpawnBomb(_nextRoadSpawnPoint, slopeRotation);

        // Advance next spawn point along the incline vector
        float rad = GenerationIncline * Mathf.Deg2Rad;
        Vector3 step = new Vector3(
            0f,
            -Mathf.Sin(rad) * _roadLength,
            Mathf.Cos(rad) * _roadLength
        );

        _nextRoadSpawnPoint += step;
    }

    private void SpawnBuildingsAlongSide(bool isLeft, Quaternion slopeRotation)
    {
        if (_buildingPrefabs == null || _buildingPrefabs.Count == 0) return;

        Vector3 forwardDir = slopeRotation * Vector3.forward;
        Vector3 rightDir = slopeRotation * Vector3.right;

        // Distance from road center to building edge
        float sideDistance = (_roadWidth / 2f) + (_buildingWidth / 2f);
        Vector3 sideLineOrigin = isLeft
            ? _nextRoadSpawnPoint - (rightDir * sideDistance)
            : _nextRoadSpawnPoint + (rightDir * sideDistance);

        ref float currentOffset = ref isLeft ? ref _leftBuildingZOffset : ref _rightBuildingZOffset;
        float targetOffset = currentOffset + _roadLength;

        while (currentOffset < targetOffset)
        {
            GameObject selectedBuilding = _buildingPrefabs[Random.Range(0, _buildingPrefabs.Count)];

            // Position along slope line
            Vector3 spawnPos = sideLineOrigin + (forwardDir * (currentOffset - (_activeRoads.Count - 1) * _roadLength + (_buildingWidth / 2f)));

            // Facing rotation toward road
            float yRotation = isLeft ? 90f : -90f;
            Quaternion buildingRotation = Quaternion.Euler(0f, yRotation, 0f);

            GameObject buildingInstance = Instantiate(selectedBuilding, spawnPos, buildingRotation, transform);
            _activeBuilings.Add(buildingInstance);

            currentOffset += _buildingWidth;
        }
    }

    private void TrySpawnBomb(Vector3 roadPos, Quaternion slopeRotation)
    {
        if (_bombPrefabs == null || _bombPrefabs.Count == 0 || Random.value > _bombChance) return;

        // 1. Pick a random X offset anywhere within the road width
        // Subtract a small buffer (e.g. 1f) so bombs don't spawn clipping off the road edge
        float halfWidthBuffer = (_roadWidth / 2f) - 1f;
        float randomX = Random.Range(-halfWidthBuffer, halfWidthBuffer);

        // 2. Pick a random Z offset along the length of the road tile so bombs aren't all in a straight horizontal line
        float randomZ = Random.Range(-_roadLength / 2f, _roadLength / 2f);

        Vector3 rightDir = slopeRotation * Vector3.right;
        Vector3 forwardDir = slopeRotation * Vector3.forward;

        // Calculate position relative to slope orientation
        Vector3 bombPos = roadPos
            + (rightDir * randomX)
            + (forwardDir * randomZ)
            + (slopeRotation * Vector3.up * 0.5f);

        // 3. Fix: Pick from _bombPrefabs instead of _roadPrefabs
        GameObject selectedBomb = _bombPrefabs[Random.Range(0, _bombPrefabs.Count)];
        GameObject bombInstance = Instantiate(selectedBomb, bombPos, slopeRotation, transform);
        _activeBombs.Add(bombInstance);
    }

    private void CleanupBehindPlayer()
    {
        float destroyZ = PlayerTransform.position.z - _destroyDistance;

        // Clean Roads
        for (int i = _activeRoads.Count - 1; i >= 0; i--)
        {
            if (_activeRoads[i] != null && _activeRoads[i].transform.position.z < destroyZ)
            {
                Destroy(_activeRoads[i]);
                _activeRoads.RemoveAt(i);
            }
        }

        // Clean Buildings
        for (int i = _activeBuilings.Count - 1; i >= 0; i--)
        {
            if (_activeBuilings[i] != null && _activeBuilings[i].transform.position.z < destroyZ)
            {
                Destroy(_activeBuilings[i]);
                _activeBuilings.RemoveAt(i);
            }
        }

        // Clean Bombs
        for (int i = _activeBombs.Count - 1; i >= 0; i--)
        {
            if (_activeBombs[i] != null && _activeBombs[i].transform.position.z < destroyZ)
            {
                Destroy(_activeBombs[i]);
                _activeBombs.RemoveAt(i);
            }
        }
    }
}

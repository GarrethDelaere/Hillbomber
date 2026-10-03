using System.Collections.Generic;
using UnityEngine;

public class MapGeneration : MonoBehaviour
{
    // Configs
    [SerializeField] private List<GameObject> _roadPrefabs;
    [SerializeField] private List<GameObject> _rampPrefabs;
    [SerializeField] private List<GameObject> _bombPrefabs;
    [SerializeField] private List<GameObject> _buildingPrefabs;
    [SerializeField] private List<GameObject> _carPrefabs;

    [SerializeField] private float _buildingWidth = 10f;
    [SerializeField] private float _roadWidth = 8f;
    [SerializeField] private float _roadLength = 20f;
    [SerializeField] private int _lanesCount = 3;

    [Range(0f, 1f)][SerializeField] private float _rampChance = 0.15f;
    [Range(0f, 1f)][SerializeField] private float _bombChance = 0.2f;
    [Range(0f, 1f)][SerializeField] private float _carChance = 0.1f;

    [SerializeField] private int _initialSegments = 10;
    [SerializeField] private float _spawnDistance = 150f;
    [SerializeField] private float _destroyDistance = 30f;

    public float GenerationIncline = 15f;

    public Transform PlayerTransform;

    // Data
    private readonly List<GameObject> _activeBuildings = new List<GameObject>();
    private readonly List<GameObject> _activeRoads = new List<GameObject>();
    private readonly List<GameObject> _activeRamps = new List<GameObject>();
    private readonly List<GameObject> _activeBombs = new List<GameObject>();
    private readonly List<GameObject> _activeCars = new List<GameObject>();

    private Vector3 _nextRoadSpawnPoint = Vector3.zero;
    private void Start()
    {
        if (PlayerTransform == null) return;

        for (int i = 0; i < _initialSegments; i++)
        {
            SpawnNextSegment();
        }
    }

    private void Update()
    {
        if (PlayerTransform == null) return;

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

        // 1. Spawn Road
        GameObject selectedRoad = _roadPrefabs[Random.Range(0, _roadPrefabs.Count)];
        Quaternion roadRotation = slopeRotation * selectedRoad.transform.rotation;

        GameObject roadInstance = Instantiate(selectedRoad, _nextRoadSpawnPoint, roadRotation, transform);
        _activeRoads.Add(roadInstance);

        // 2. Spawn Side Buildings
        SpawnBuildingsAlongSide(isLeft: true, slopeRotation);
        SpawnBuildingsAlongSide(isLeft: false, slopeRotation);

        // 3. Try Spawning Ramp vs Obstacles
        if (!TrySpawnRamp(_nextRoadSpawnPoint, slopeRotation))
        {
            TrySpawnBomb(_nextRoadSpawnPoint, slopeRotation);
            TrySpawnCar(_nextRoadSpawnPoint, slopeRotation);
        }

        // 4. Advance Next Spawn Point Along Incline Vector
        Vector3 step = slopeRotation * Vector3.forward * _roadLength;
        _nextRoadSpawnPoint += step;
    }

    private bool TrySpawnRamp(Vector3 roadPos, Quaternion slopeRotation)
    {
        if (_rampPrefabs == null || _rampPrefabs.Count == 0 || Random.value > _rampChance) return false;

        float lanePos = GetRandomLaneOffset();
        Vector3 rightDir = slopeRotation * Vector3.right;
        Vector3 forwardDir = slopeRotation * Vector3.forward;

        Vector3 rampPos = roadPos + (rightDir * lanePos) + (forwardDir * (_roadLength * 0.25f));

        GameObject selectedRamp = _rampPrefabs[Random.Range(0, _rampPrefabs.Count)];
        Quaternion rampRotation = slopeRotation * selectedRamp.transform.rotation;

        GameObject rampInstance = Instantiate(selectedRamp, rampPos, rampRotation, transform);
        _activeRamps.Add(rampInstance);

        return true;
    }

    private void TrySpawnBomb(Vector3 roadPos, Quaternion slopeRotation)
    {
        if (_bombPrefabs == null || _bombPrefabs.Count == 0 || Random.value > _bombChance) return;

        float lanePos = GetRandomLaneOffset();
        float randomOffsetZ = Random.Range(-_roadLength * 0.35f, _roadLength * 0.35f);

        Vector3 rightDir = slopeRotation * Vector3.right;
        Vector3 forwardDir = slopeRotation * Vector3.forward;

        Vector3 bombPos = roadPos + (rightDir * lanePos) + (forwardDir * randomOffsetZ);

        GameObject selectedBomb = _bombPrefabs[Random.Range(0, _bombPrefabs.Count)];
        GameObject bombInstance = Instantiate(selectedBomb, bombPos, selectedBomb.transform.rotation * slopeRotation, transform);
        _activeBombs.Add(bombInstance);
    }

    private void TrySpawnCar(Vector3 roadPos, Quaternion slopeRotation)
    {
        if (_carPrefabs == null || _carPrefabs.Count == 0 || Random.value > _carChance) return;

        float lanePos = GetRandomLaneOffset();
        float randomOffsetZ = Random.Range(-_roadLength * 0.35f, _roadLength * 0.35f);

        Vector3 rightDir = slopeRotation * Vector3.right;
        Vector3 forwardDir = slopeRotation * Vector3.forward;

        Vector3 carPos = roadPos + (rightDir * lanePos) + (forwardDir * randomOffsetZ);

        bool driveForward = Random.value > 0.5f;
        Quaternion carRotation = slopeRotation * Quaternion.Euler(0f, driveForward ? 0f : 180f, 0f);

        GameObject selectedCar = _carPrefabs[Random.Range(0, _carPrefabs.Count)];
        GameObject carInstance = Instantiate(selectedCar, carPos, carRotation, transform);
        _activeCars.Add(carInstance);
    }

    private void SpawnBuildingsAlongSide(bool isLeft, Quaternion slopeRotation)
    {
        if (_buildingPrefabs == null || _buildingPrefabs.Count == 0) return;

        Vector3 forwardDir = slopeRotation * Vector3.forward;
        Vector3 rightDir = slopeRotation * Vector3.right;

        float sideDistance = (_roadWidth / 2f) + (_buildingWidth / 2f);
        Vector3 segmentSideOrigin = _nextRoadSpawnPoint + (isLeft ? -rightDir : rightDir) * sideDistance;

        float segmentProgress = 0f;

        while (segmentProgress < _roadLength)
        {
            GameObject selectedBuilding = _buildingPrefabs[Random.Range(0, _buildingPrefabs.Count)];

            Vector3 spawnPos = segmentSideOrigin + forwardDir * (segmentProgress + _buildingWidth / 2f);

            float yRotation = isLeft ? 90f : -90f;
            Quaternion buildingRotation = Quaternion.Euler(0f, yRotation, 0f);

            float rad = GenerationIncline * Mathf.Deg2Rad;
            float halfDepth = _buildingWidth / 2f;
            float heightOffset = Mathf.Sin(rad) * halfDepth;

            spawnPos.y -= heightOffset;

            GameObject buildingInstance = Instantiate(selectedBuilding, spawnPos, buildingRotation, transform);
            _activeBuildings.Add(buildingInstance);

            segmentProgress += _buildingWidth;
        }
    }

    private float GetRandomLaneOffset()
    {
        if (_lanesCount <= 1) return 0f;

        float usableWidth = _roadWidth - 2f; // Safety buffer from road edges
        float laneSpacing = usableWidth / (_lanesCount - 1);
        int randomLane = Random.Range(0, _lanesCount);

        return -(usableWidth / 2f) + (randomLane * laneSpacing);
    }

    private void CleanupBehindPlayer()
    {
        Quaternion slopeRotation = Quaternion.Euler(GenerationIncline, 0f, 0f);
        Vector3 forwardVector = slopeRotation * Vector3.forward;

        CleanupList(_activeRoads, forwardVector);
        CleanupList(_activeBuildings, forwardVector);
        CleanupList(_activeRamps, forwardVector);
        CleanupList(_activeBombs, forwardVector);
        CleanupList(_activeCars, forwardVector);
    }

    private void CleanupList(List<GameObject> list, Vector3 forwardVector)
    {
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (list[i] == null)
            {
                list.RemoveAt(i);
                continue;
            }

            // Directional distance along path vector relative to player
            Vector3 toObject = list[i].transform.position - PlayerTransform.position;
            float dotDistance = Vector3.Dot(toObject, forwardVector);

            if (dotDistance < -_destroyDistance)
            {
                Destroy(list[i]);
                list.RemoveAt(i);
            }
        }
    }
}

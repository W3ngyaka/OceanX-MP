using System.Collections.Generic;
using UnityEngine;

// 3-D uniform grid that maps world positions to bucket lists of objects.
// Reduces neighbor search from O(n²) to roughly O(n × bucket_density).
public class SpatialPartition3D<T>
{
    private readonly Dictionary<Vector3Int, List<T>> _grid = new Dictionary<Vector3Int, List<T>>();
    private readonly Dictionary<T, Vector3Int>       _cellMap = new Dictionary<T, Vector3Int>();
    private readonly float _cellSize;
    private readonly int   _neighborOffset;

    public Dictionary<Vector3Int, List<T>> Grid => _grid;

    public SpatialPartition3D(float cellSize, int neighborRange)
    {
        _cellSize       = cellSize;
        _neighborOffset = neighborRange;
    }

    public void Add(Vector3 position, T obj)
    {
        Vector3Int cell = CellOf(position);
        AddToCell(cell, obj);
    }

    public void Remove(Vector3 position, T obj)
    {
        Vector3Int cell = CellOf(position);
        RemoveFromCell(cell, obj);
    }

    public void UpdateObjectCell(Vector3 newPosition, T obj)
    {
        Vector3Int newCell = CellOf(newPosition);
        if (_cellMap.TryGetValue(obj, out Vector3Int oldCell) && oldCell != newCell)
        {
            RemoveFromCell(oldCell, obj);
            AddToCell(newCell, obj);
        }
    }

    public List<T> GetNearby(Vector3 position)
    {
        List<T>    result = new List<T>();
        Vector3Int center = CellOf(position);

        for (int x = -_neighborOffset; x <= _neighborOffset; x++)
        for (int y = -_neighborOffset; y <= _neighborOffset; y++)
        for (int z = -_neighborOffset; z <= _neighborOffset; z++)
        {
            Vector3Int neighbor = center + new Vector3Int(x, y, z);
            if (_grid.TryGetValue(neighbor, out List<T> bucket))
                result.AddRange(bucket);
        }

        return result;
    }

    public void Clear()
    {
        _grid.Clear();
        _cellMap.Clear();
    }

    private void AddToCell(Vector3Int cell, T obj)
    {
        if (!_grid.TryGetValue(cell, out List<T> bucket))
        {
            bucket = new List<T>();
            _grid[cell] = bucket;
        }
        bucket.Add(obj);
        _cellMap[obj] = cell;
    }

    private void RemoveFromCell(Vector3Int cell, T obj)
    {
        if (_grid.TryGetValue(cell, out List<T> bucket))
        {
            bucket.Remove(obj);
            if (bucket.Count == 0)
                _grid.Remove(cell);
        }
        _cellMap.Remove(obj);
    }

    private Vector3Int CellOf(Vector3 position)
    {
        return new Vector3Int(
            Mathf.FloorToInt(position.x / _cellSize),
            Mathf.FloorToInt(position.y / _cellSize),
            Mathf.FloorToInt(position.z / _cellSize));
    }
}


using System.Collections.Generic;
using UnityEngine;

namespace OceanX.BoidsCPU
{
    /// <summary>
    /// Class that implements a grid of cells that can hold objects based on their position in the world.
    /// This is an optimization structure that reduces total number of calculations in the simulation by
    /// limiting them to only neighboring cells.
    /// </summary>
    public class SpatialPartition3D<T>
    {
        private Dictionary<Vector3Int, List<T>> _spatialPartitionGrid = new Dictionary<Vector3Int, List<T>>();
        // Dictionary containing the current cell key of each object.
        private Dictionary<T, Vector3Int> _objectCellMap = new Dictionary<T, Vector3Int>();
        private float _cellSize;
        private int _neighboringCellsOffset;

        public Dictionary<Vector3Int, List<T>> Grid { get => _spatialPartitionGrid; }

        /// <summary>
        /// Constructor for the spatial partition grid that initializes the properties for the whole grid.
        /// </summary>
        /// <param name="cellSize">Size of each cell in meters. Larger cell size will result in more 
        /// objects being contained in each cell, while the smaller cell size will result in more
        /// cells being created and managed.</param>
        /// <param name="neighborRange">The range of neighboring cells to consider when retrieving nearby objects.</param>
        public SpatialPartition3D(float cellSize, int neighborRange)
        {
            _cellSize = cellSize;
            _neighboringCellsOffset = neighborRange;
        }

        /// <summary>
        /// Adds an object to the spatial partition at the given position.
        /// </summary>
        /// <param name="position">The world position of the object.</param>
        /// <param name="objectToAdd">The object to add to the partition.</param>
        public void Add(Vector3 position, T objectToAdd)
        {
            Vector3Int cellIndex = GetCellIndex(position);
            AddByCellIndex(cellIndex, objectToAdd);
        }

        /// <summary>
        /// Removes an object from the spatial partition at the given position.
        /// </summary>
        /// <param name="position">The world position of the object.</param>
        /// <param name="objectToRemove">The object to remove from the partition.</param>
        public void Remove(Vector3 position, T objectToRemove)
        {
            Vector3Int cellIndex = GetCellIndex(position);
            RemoveByCellIndex(cellIndex, objectToRemove);
        }        

        /// <summary>
        /// Updates the cell of an object if its position change is resulting in new cell index.
        /// </summary>
        /// <param name="newPosition">The new world position of the object.</param>
        /// <param name="objectToUpdate">The object to update.</param>
        public void UpdateObjectCell(Vector3 newPosition, T objectToUpdate)
        {
            Vector3Int newCellIndex = GetCellIndex(newPosition);
            if (_objectCellMap.TryGetValue(objectToUpdate, out Vector3Int oldCellIndex) && oldCellIndex != newCellIndex)
            {
                RemoveByCellIndex(oldCellIndex, objectToUpdate);
                AddByCellIndex(newCellIndex, objectToUpdate);
            }
        }

        /// <summary>
        /// Clears all objects from the spatial partition.
        /// </summary>
        public void Clear()
        {
            _spatialPartitionGrid.Clear();
            _objectCellMap.Clear();
        }

        /// <summary>
        /// Retrieves all objects within the neighboring cells of a given position.
        /// </summary>
        /// <param name="position">The world position to search around.</param>
        /// <returns>A list of objects found in the same or neighboring cells.</returns>
        public List<T> GetNearby(Vector3 position)
        {
            List<T> nearbyObjects = new List<T>();
            Vector3Int cellIndex = GetCellIndex(position);

            for (int x = -_neighboringCellsOffset; x <= _neighboringCellsOffset; x++)
            {
                for (int y = -_neighboringCellsOffset; y <= _neighboringCellsOffset; y++)
                {
                    for (int z = -_neighboringCellsOffset; z <= _neighboringCellsOffset; z++)
                    {
                        Vector3Int neighborIndex = cellIndex + new Vector3Int(x, y, z);
                        if (_spatialPartitionGrid.ContainsKey(neighborIndex))
                        {
                            nearbyObjects.AddRange(_spatialPartitionGrid[neighborIndex]);
                        }
                    }
                }
            }

            return nearbyObjects;
        }

        private void AddByCellIndex(Vector3Int cellIndex, T objectToAdd)
        {
            if (!_spatialPartitionGrid.ContainsKey(cellIndex))
            {
                _spatialPartitionGrid[cellIndex] = new List<T>();
            }
            _spatialPartitionGrid[cellIndex].Add(objectToAdd);
            _objectCellMap[objectToAdd] = cellIndex;
        }

        private void RemoveByCellIndex(Vector3Int cellIndex, T objectToRemove)
        {
            if (_spatialPartitionGrid.ContainsKey(cellIndex))
            {
                _spatialPartitionGrid[cellIndex].Remove(objectToRemove);
                if (_spatialPartitionGrid[cellIndex].Count == 0)
                {
                    _spatialPartitionGrid.Remove(cellIndex);
                }
            }
            _objectCellMap.Remove(objectToRemove);
        }

        private Vector3Int GetCellIndex(Vector3 position)
        {
            return new Vector3Int(
                Mathf.FloorToInt(position.x / _cellSize),
                Mathf.FloorToInt(position.y / _cellSize),
                Mathf.FloorToInt(position.z / _cellSize));
        }
    }
}
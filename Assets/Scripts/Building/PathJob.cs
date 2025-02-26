using Unity.Collections.LowLevel.Unsafe;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using System;
using Unity.Burst;

[BurstCompile(FloatPrecision.Low, FloatMode.Fast, OptimizeFor = OptimizeFor.Performance)]
public struct PathJob : IJob
{
    public readonly struct IndexDistance : IComparable<IndexDistance>
    {
        public readonly int Index;
        public readonly int Distance;

        public IndexDistance(int index, int distance)
        {
            Index = index;
            Distance = distance;
        }

        public int CompareTo(IndexDistance other) => Distance.CompareTo(other.Distance);
    }

    public NativePriorityQueue<IndexDistance> FrontierQueue;
    public NativeArray<byte> Directions;
    public NativeArray<int> Distances;

    [ReadOnly]
    public NativeArray<bool>.ReadOnly NotWalkableIndexes;
    
    [ReadOnly]
    public NativeArray<bool>.ReadOnly TargetIndexes;
    
    [ReadOnly]
    public NativeArray<byte>.ReadOnly MovementCosts;

    [ReadOnly]
    public NativeArray<int2>.ReadOnly NeighbourDirections;
    
    [ReadOnly]
    public int GridWidth;

    [ReadOnly]
    public int ArrayLength;
    
    [BurstCompile]
    public void Execute()
    {
        for (int i = 0; i < ArrayLength; i++)
        {
            if (TargetIndexes[i])
            {
                Distances[i] = 0;
                FrontierQueue.Enqueue(new IndexDistance(i, 0));
            }
            else
            {
                Distances[i] = int.MaxValue;
            }
        }

        NativeArray<int> neighbours = new NativeArray<int>(8, Allocator.Temp);
        while (FrontierQueue.Length > 0)
        {
            IndexDistance cell = FrontierQueue.Dequeue();
            GetNeighbours(cell.Index, neighbours);
            for (int i = 0; i < 8; i++)
            {
                int index = neighbours[i];
                if (index == -1) continue;
                if (NotWalkableIndexes[index]) continue;

                int manhattanDist = i % 2 == 0 ? 10 : 14;
                int dist = cell.Distance + MovementCosts[index] * manhattanDist;
                if (dist >= Distances[index]) continue;
                
                Distances[index] = dist;
                Directions[index] = GetDirection(-NeighbourDirections[i]);
                FrontierQueue.Enqueue(new IndexDistance(index, dist));
            }
        }

        neighbours.Dispose();
    }

    private void GetNeighbours(int index, NativeArray<int> array)
    {
        int x = index % GridWidth;
        int y = index / GridWidth;

        for (int i = 0; i < 8; i++)
        {
            int nx = x + NeighbourDirections[i].x;
            int ny = y + NeighbourDirections[i].y;

            if (nx >= 0 && nx < GridWidth && ny >= 0 && ny < ArrayLength / GridWidth)
            {
                array[i] = ny * GridWidth + nx;
            }
            else
            {
                array[i] = -1;
            }
        }
    }

    private static byte GetDirection(int2 direction)
    {
        // Directly map the 8 possible int2 values to corresponding byte values
        return (direction.x, direction.y) switch
        {
            (1, 0) => 0,       // Right
            (1, 1) => 32,      // Up-Right
            (0, 1) => 64,      // Up
            (-1, 1) => 96,     // Up-Left
            (-1, 0) => 128,    // Left
            (-1, -1) => 160,   // Down-Left
            (0, -1) => 192,    // Down
            (1, -1) => 224,    // Down-Right
            _ => 0             // Default case (should never happen with valid inputs)
        };
    }
}


/// <summary>
/// Priority Queue implementation with item data stored in native containers. 
/// </summary>
/// <typeparam name="T"></typeparam>
[NativeContainer]
[DebuggerDisplay("Length = {Length}")]
[DebuggerTypeProxy(typeof(NativePriorityQueueDebugView<>))]
public unsafe struct NativePriorityQueue<T> : IDisposable
        where T : struct, IComparable<T>
{
    #region Public properties

    /// <summary>
    /// Returns the number of values stored in the queue
    /// </summary>
    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get { return m_ListData != null ? m_ListData->length : 0; }
    }

    /// <summary>
    /// Returns true if the queue is empty
    /// </summary>
    public bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get { return m_ListData == null || m_ListData->length == 0; }
    }

    #endregion

    #region Private fields

    private const int DEFAULT_SIZE = 32;
    private const int GROWTH_FACTOR = 2;

    private Allocator m_AllocatorLabel;
    private NativeArray<T> m_Buffer;

    [NativeDisableUnsafePtrRestriction]
    private UnsafeListData* m_ListData;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
    internal AtomicSafetyHandle m_Safety;
    [NativeSetClassTypeToNullOnSchedule] internal DisposeSentinel m_DisposeSentinel;
#endif

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the BinaryHeap class with a the indicated capacity
    /// </summary>
    public NativePriorityQueue(int capacity = DEFAULT_SIZE, Allocator allocator = Allocator.TempJob)
    {
        if (capacity < 1)
        {
            throw new ArgumentException("Capacity must be greater than zero");
        }

        m_AllocatorLabel = allocator;

        m_ListData = (UnsafeListData*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<UnsafeListData>(), UnsafeUtility.AlignOf<UnsafeListData>(), allocator);
        m_Buffer = new NativeArray<T>(capacity, allocator, NativeArrayOptions.UninitializedMemory);

        m_ListData->capacity = capacity;
        m_ListData->length = 0;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 0, allocator);
#endif

    }

    #endregion

    #region Public methods

    /// <summary>
    /// Disposes of any native memory held by this instance
    /// </summary>
    public void Dispose()
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif

        if (!m_Buffer.IsCreated)
        {
            throw new InvalidOperationException($"This collection has already been disposed");
        }

        UnsafeUtility.Free(m_ListData, m_AllocatorLabel);
        m_Buffer.Dispose();

        m_ListData = null;
        m_AllocatorLabel = Allocator.Invalid;
    }

    /// <summary>
    /// Returns the first item in the heap without removing it from the heap
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Peek()
    {
        if (m_ListData->length == 0)
        {
            throw new InvalidOperationException("Cannot peek at first item when the heap is empty.");
        }

        return m_Buffer[0];
    }

    /// <summary>
    /// Adds a key and value to the heap.
    /// </summary>
    /// <param name="item">The item to add to the heap.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Enqueue(T item)
    {
        if (m_ListData->length == m_ListData->capacity)
        {
            EnsureCapacity(m_ListData->length + 1);
        }

        m_Buffer[m_ListData->length] = item;

        heapifyUp(m_ListData->length, item);

        m_ListData->length++;
    }

    /// <summary>
    /// Removes and returns the first item in the heap.
    /// </summary>
    /// <returns>The first value in the heap.</returns>
    public T Dequeue()
    {
        if (m_ListData->length == 0)
        {
            throw new InvalidOperationException("Cannot remove item from an empty heap");
        }

        // Stores the key of root node to be returned
        var v = m_Buffer[0];

        // Decrease heap size by 1
        m_ListData->length -= 1;

        // Copy the last node to the root node and clear the last node
        m_Buffer[0] = m_Buffer[m_ListData->length];
        m_Buffer[m_ListData->length] = default(T);

        // Restore the heap property of the tree
        heapifyDown(0, m_Buffer[0]);

        return v;
    }

    /// <summary>
    /// Ensures that there is large enough internal capacity to store the indicated number of
    /// items without having to re-allocate the internal buffer.
    /// </summary>
    /// <param name="count"></param>
    public void EnsureCapacity(int count)
    {
        var originalLength = m_ListData->capacity;
        while (count > m_ListData->capacity)
        {
            m_ListData->capacity *= GROWTH_FACTOR;
        }

        // Create a new array to hold the item data and copy the existing data into it. 
        var dataSize = UnsafeUtility.SizeOf<T>() * originalLength;
        var newArray = new NativeArray<T>(m_ListData->capacity, m_AllocatorLabel);
        UnsafeUtility.MemCpy(m_Buffer.GetUnsafePtr(), newArray.GetUnsafePtr(), dataSize);

        // Dispose of the existing array 
        m_Buffer.Dispose();

        // The new array is now this instance's items array 
        m_Buffer = newArray;
    }

    /// <summary>
    /// Returns the raw (not necessarily sorted) contents of the priority queue as a managed array.
    /// </summary>
    /// <returns></returns>
    public T[] ToArray()
    {
        var length = m_ListData->length;

        T[] result = new T[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = m_Buffer[i];
        }

        return result;
    }

    #endregion

    #region Private utility methods

    private int heapifyUp(int index, T item)
    {
        var parent = (index - 1) >> 1;

        while (parent > -1 && item.CompareTo(m_Buffer[parent]) <= 0)
        {
            // Swap nodes
            m_Buffer[index] = m_Buffer[parent];

            index = parent;
            parent = (index - 1) >> 1;
        }

        m_Buffer[index] = item;

        return index;
    }

    private int heapifyDown(int parent, T item)
    {
        var index = 0;

        while (true)
        {
            int ch1 = (parent << 1) + 1;
            if (ch1 >= m_ListData->length)
                break;

            int ch2 = (parent << 1) + 2;
            if (ch2 >= m_ListData->length)
            {
                index = ch1;
            }
            else
            {
                index = m_Buffer[ch1].CompareTo(m_Buffer[ch2]) <= 0 ? ch1 : ch2;
            }

            if (item.CompareTo(m_Buffer[index]) < 0)
                break;

            m_Buffer[parent] = m_Buffer[index]; // Swap nodes
            parent = index;
        }

        m_Buffer[parent] = item;

        return parent;
    }

    #endregion

    #region Debugging support

    public override string ToString()
    {
        return string.Format("Length={0}", m_ListData != null ? m_ListData->length : -1);
    }

    #endregion

}

#region Related types 

public struct UnsafeListData
{
    public int length;
    public int capacity;
}

internal sealed class NativePriorityQueueDebugView<T>
    where T : struct, IComparable<T>
{
    private NativePriorityQueue<T> list;

    /// <summary>
    /// Create the view for a given list
    /// </summary>
    /// 
    /// <param name="list">
    /// List to view
    /// </param>
    public NativePriorityQueueDebugView(NativePriorityQueue<T> list)
    {
        this.list = list;
    }

    /// <summary>
    /// Get a managed array version of the list's elements to be viewed in the
    /// debugger.
    /// </summary>
    public T[] Items
    {
        get
        {
            return list.ToArray();
        }
    }
}

#endregion
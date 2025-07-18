using System;

namespace Utility
{
    public sealed class FiloHandle<T>
    {
        internal DeletableQueue<T>.Node Node;
    }

    public sealed class DeletableQueue<T>
    {
        public sealed class Node
        {
            internal Node Prev, Next;
            internal readonly T Item;
            internal readonly FiloHandle<T> Handle;

            internal Node(T item)
            {
                Item = item;
                Handle = new FiloHandle<T> { Node = this };
            }
        }

        private Node head, tail;

        public FiloHandle<T> Enqueue(T item)
        {
            var n = new Node(item);

            if (tail == null)
                head = tail = n;
            else
            {
                n.Prev = tail;
                tail.Next = n;
                tail = n;
            }

            Count++;
            return n.Handle;
        }

        public T Dequeue()
        {
            if (head == null)
                throw new InvalidOperationException("Queue empty");

            var n = head;
            RemoveNode(n);
            return n.Item;
        }

        public bool Delete(FiloHandle<T> h)
        {
            var n = h.Node;
            if (n == null) return false;

            RemoveNode(n);
            return true;
        }

        public int Count { get; private set; }

        private void RemoveNode(Node n)
        {
            if (n.Prev != null) n.Prev.Next = n.Next;
            if (n.Next != null) n.Next.Prev = n.Prev;
            if (head == n) head = n.Next;
            if (tail == n) tail = n.Prev;

            n.Handle.Node = null;
            Count--;
        }

        public bool TryDequeue(out T value)
        {
            value = default;
            if (Count <= 0)
            {
                return false;
            }
            
            value = Dequeue();
            return true;
        }
    }
}
using System.Collections.Generic;

namespace Enderlook.StateMachine
{
    internal struct ListSlot<T>
    {
        private List<T> list;

        public ListSlot(List<T> list) => this.list = list;

        public int Reserve()
        {
            int count = list.Count;
            list.Add(default);
            return count;
        }

        public (int from, int to) Reserve(int amount)
        {
            int from = list.Count;
            if (list.Capacity < list.Count + amount)
                list.Capacity = list.Count + amount;
            for (int i = 0; i < amount; i++)
                list.Add(default);
            return (from, list.Count);
        }

        public void Store(T value, int slot) => list[slot] = value;

        public List<T> Extract() => list;
    }
}
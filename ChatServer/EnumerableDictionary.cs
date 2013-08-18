using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PandaServer.Classes
{
    public class EnumerableDictionary<TKey, TValue> : Dictionary<TKey, TValue>
    {
        public new TValue this[TKey key]
        {
            get
            {
                if (base.ContainsKey(key))
                    return base[key];
                return default(TValue);
            }
            set
            {
                base[key] = value;
            }
        }
        public new IEnumerator<TValue> GetEnumerator()
        {
            return new Enumerator<TValue>(this);
        }
        private struct Enumerator<T> : IEnumerator<T>
        {
            internal Enumerator(EnumerableDictionary<TKey, T> dict)
            {
                this.m_list = dict.Select(pair => pair.Value).ToList();
                this.position = -1;
            }

            private List<T> m_list;
            int position;

            public T Current
            {
                get
                {
                    return this.m_list[position];
                }
            }
            public void Dispose()
            {
                this.m_list = null;
                this.Reset();
            }
            public bool MoveNext()
            {
                return (++position) < m_list.Count;
            }
            public void Reset()
            {
                position = -1;
            }

            object IEnumerator.Current
            {
                get { return this.Current; }
            }
        }
    }
}
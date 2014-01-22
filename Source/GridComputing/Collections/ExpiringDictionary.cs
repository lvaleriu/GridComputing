using System;
using System.Collections;
using System.Collections.Generic;
using System.Timers;

namespace GridComputing.Collections
{
    /// <summary>
    /// A dictionary collection that automatically
    /// removes items from its own contents once
    /// a specified duration has passed.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    public class ExpiringDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        #region Fields

        private readonly List<KeyValuePair<TKey, DateTime>> _expiries = new List<KeyValuePair<TKey, DateTime>>();
        private readonly Dictionary<TKey, TValue> _innerDictionary = new Dictionary<TKey, TValue>();
        private readonly object _syncLock = new object();
        private readonly Timer _timer = new Timer();
        private TimeSpan _itemLiveTime;

        #endregion


        private void OnKeyRemoved(TKey key)
        {
            if (KeyRemoved != null) KeyRemoved(key);
        }

        private void FireKeyRemoving(TKey key, ref bool cancel)
        {
            if (KeyRemoving != null)
                KeyRemoving(key, ref cancel);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpiringDictionary&lt;TKey, TValue&gt;"/> class.
        /// </summary>
        /// <param name="itemLiveTime">The item live time. <seealso cref="ItemLiveTime"/></param>
        /// <param name="interval">The interval. How frequently to scan
        /// for items to eject.</param>
        public ExpiringDictionary(TimeSpan itemLiveTime, TimeSpan interval)
        {
            _itemLiveTime = itemLiveTime;
            _timer.Interval = interval.TotalMilliseconds;
            _timer.Elapsed += RemoveExpiredItems;
            _timer.Start();
        }

        /// <summary>
        /// Gets the sync lock, used for asynchronous access
        /// to the inner collection.
        /// </summary>
        /// <value>The sync lock.</value>
        public object SyncLock
        {
            get { return _syncLock; }
        }

        /// <summary>
        /// Gets or sets the time that an item should remain
        /// in the collection before it is automatically removed.
        /// </summary>
        /// <value>The item live time.</value>
        public TimeSpan ItemLiveTime
        {
            get { return _itemLiveTime; }
            set { _itemLiveTime = value; }
        }

        #region IDictionary<TKey,TValue> Members

        public bool ContainsKey(TKey key)
        {
            return _innerDictionary.ContainsKey(key);
        }

        public void Add(TKey key, TValue value)
        {
            lock (SyncLock)
            {
                _innerDictionary.Add(key, value);
                _expiries.Add(new KeyValuePair<TKey, DateTime>(key, DateTime.UtcNow));
            }
        }

        public bool Remove(TKey key)
        {
            lock (SyncLock)
            {
                bool result = _innerDictionary.Remove(key);
                _expiries.RemoveAll(k => EqualityComparer<TKey>.Default.Equals(k.Key, key));
                return result;
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _innerDictionary.TryGetValue(key, out value);
        }

        public TValue this[TKey key]
        {
            get { return _innerDictionary[key]; }
            set
            {
                lock (SyncLock)
                {
                    _innerDictionary[key] = value;

                    _expiries.RemoveAll(k => EqualityComparer<TKey>.Default.Equals(k.Key, key));

                    if (!EqualityComparer<TValue>.Default.Equals(value))
                    {
                        _expiries.Add(new KeyValuePair<TKey, DateTime>(key, DateTime.UtcNow));
                    }
                }
            }
        }

        public ICollection<TKey> Keys
        {
            get { return _innerDictionary.Keys; }
        }

        public ICollection<TValue> Values
        {
            get { return _innerDictionary.Values; }
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            lock (SyncLock)
            {
                _innerDictionary.Add(item.Key, item.Value);
                if (!EqualityComparer<KeyValuePair<TKey, TValue>>.Default.Equals(item))
                {
                    _expiries.Add(new KeyValuePair<TKey, DateTime>(item.Key, DateTime.UtcNow));
                }
            }
        }

        public void Clear()
        {
            lock (SyncLock)
            {
                _innerDictionary.Clear();
                _expiries.Clear();
            }
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            if (EqualityComparer<KeyValuePair<TKey, TValue>>.Default.Equals(item))
            {
                return false;
            }
            return _innerDictionary.ContainsKey(item.Key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }

            if (arrayIndex < 0 || arrayIndex > array.Length)
            {
                throw new ArgumentOutOfRangeException("arrayIndex", "Must be greater than zero.");
            }
            if (array.Length - arrayIndex < Count)
            {
                throw new ArgumentException("array.Length must be greater than arrayIndex.");
            }
            lock (SyncLock)
            {
                foreach (var pair in _innerDictionary)
                {
                    array[arrayIndex++] = new KeyValuePair<TKey, TValue>(pair.Key, pair.Value);
                }
            }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (EqualityComparer<KeyValuePair<TKey, TValue>>.Default.Equals(item))
            {
                return false;
            }
            lock (SyncLock)
            {
                bool result = _innerDictionary.Remove(item.Key);
                return result;
            }
        }

        public int Count
        {
            get { return _innerDictionary.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return _innerDictionary.GetEnumerator();
        }

        public IEnumerator GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<TKey, TValue>>) this).GetEnumerator();
        }

        #endregion

        private void RemoveExpiredItems(object sender, ElapsedEventArgs e)
        {
            lock (SyncLock)
            {
                var itemsToBeRemoved = new List<TKey>();
                DateTime expiryTime = DateTime.UtcNow - _itemLiveTime;

                foreach (var pair in _expiries)
                {
                    if (pair.Value < expiryTime)
                    {
                        itemsToBeRemoved.Add(pair.Key);
                    }
                    else
                    {
                        break;
                    }
                }
                foreach (TKey key in itemsToBeRemoved)
                {
                    bool cancel = false;
                    FireKeyRemoving(key, ref cancel);

                    if (!cancel)
                    {
                        Remove(key);
                        OnKeyRemoved(key);
                    }
                    else
                    {
                        Touch(key);
                    }
                }
            }
        }

        /// <summary>
        /// Updates the expiry time on an item in the collection;
        /// thus renewing it so that the time it will remain in the
        /// collection will be increased.
        /// </summary>
        /// <param name="key">The key.</param>
        public void Touch(TKey key)
        {
            lock (SyncLock)
            {
                this[key] = this[key]; /* Forces DateTime update. */
            }
        }

        public event Action<TKey> KeyRemoved;
        public event KeyRemovingDelegate KeyRemoving;

        public delegate void KeyRemovingDelegate(TKey key, ref bool cancel);
    }
}
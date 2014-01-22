using System;
using System.Collections;
using System.Collections.Generic;
using System.Timers;

namespace GridComputing.Collections
{
    /// <summary>
    /// A queue collection that automatically
    /// removes items from its own contents once
    /// a specified duration has passed.
    /// </summary>
    /// <typeparam name="T">The type of the key.</typeparam>
    public class ExpiringQueue<T> : IEnumerable<T>, ICollection, IEnumerable
    {
        private readonly List<KeyValuePair<T, DateTime>> _expiries = new List<KeyValuePair<T, DateTime>>();
        private readonly List<T> _queue = new List<T>();
        private readonly object _sync = new object();
        private readonly Timer _timer = new Timer();
        private TimeSpan _itemLiveTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpiringQueue&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="itemLiveTime">The item live time. 
        /// <seealso cref="ItemLiveTime"/></param>
        /// <param name="interval">The interval to periodically check 
        /// removal candidates.</param>
        public ExpiringQueue(TimeSpan itemLiveTime, TimeSpan interval)
        {
            _itemLiveTime = itemLiveTime;
            _timer.Interval = interval.TotalMilliseconds;
            _timer.Elapsed += RemoveExpiredItems;
            _timer.Start();
        }

        /// <summary>
        /// Gets or sets the time that and item should
        /// remain in the collection before 
        /// being automatically removed.
        /// </summary>
        /// <value>The item live time.</value>
        public TimeSpan ItemLiveTime
        {
            get { return _itemLiveTime; }
            set { _itemLiveTime = value; }
        }

        #region ICollection Members

        public void CopyTo(Array array, int arrayIndex)
        {
            lock (SyncRoot)
            {
                _queue.CopyTo((T[]) array, arrayIndex);
            }
        }

        public int Count
        {
            get
            {
                lock (SyncRoot)
                {
                    return _queue.Count;
                }
            }
        }

        public object SyncRoot
        {
            get { return _sync; }
        }

        public bool IsSynchronized
        {
            get { return true; }
        }

        #endregion

        #region IEnumerable<T> Members

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            lock (SyncRoot)
            {
                return _queue.GetEnumerator();
            }
        }

        public IEnumerator GetEnumerator()
        {
            lock (SyncRoot)
            {
                return ((IEnumerable<T>) this).GetEnumerator();
            }
        }

        #endregion

        private void RemoveExpiredItems(object sender, ElapsedEventArgs e)
        {
            lock (SyncRoot)
            {
                var itemsToBeRemoved = new List<T>();
                DateTime expiryTime = DateTime.Now - _itemLiveTime;

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
                foreach (T key in itemsToBeRemoved)
                {
                    _queue.Remove(key);
                }
            }
        }

        public void Enqueue(T item)
        {
            lock (SyncRoot)
            {
                _queue.Insert(0, item);
            }
        }

        public T Dequeue()
        {
            lock (SyncRoot)
            {
                if (_queue.Count < 1)
                {
                    return default(T);
                }
                T item = _queue[_queue.Count - 1];
                _queue.Remove(item);
                return item;
            }
        }

        public T Peek()
        {
            lock (SyncRoot)
            {
                if (_queue.Count < 1)
                {
                    return default(T);
                }
                T item = _queue[_queue.Count - 1];
                return item;
            }
        }

        /// <summary>
        /// Updates the expiry time on an item in the collection;
        /// thus renewing it so that the time it will remain in the
        /// collection will be increased.
        /// </summary>
        /// <param name="item">The item to touch.</param>
        public void Touch(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            lock (SyncRoot)
            {
                if (_queue.Contains(item))
                {
                    _expiries.RemoveAll(k => EqualityComparer<T>.Default.Equals(k.Key, item));
                    _expiries.Add(new KeyValuePair<T, DateTime>(item, DateTime.Now));
                }
                else
                {
                    Enqueue(item);
                }
            }
        }
    }
}
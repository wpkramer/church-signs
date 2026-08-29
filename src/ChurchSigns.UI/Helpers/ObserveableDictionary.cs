using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace ChurchSigns.UI.Helpers
{
    /// <summary>
    /// A dictionary that notifies UI when items are added, removed, or updated.
    /// </summary>
    public class ObservableDictionary<TKey, TValue> :
        IDictionary<TKey, TValue>,
        INotifyCollectionChanged,
        INotifyPropertyChanged
    {
        private readonly Dictionary<TKey, TValue> _dictionary = new();

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public TValue this[TKey key]
        {
            get => _dictionary[key];
            set
            {
                if (_dictionary.ContainsKey(key))
                {
                    var oldValue = _dictionary[key];
                    if (!EqualityComparer<TValue>.Default.Equals(oldValue, value))
                    {
                        _dictionary[key] = value;
                        OnCollectionChanged(
                            new NotifyCollectionChangedEventArgs(
                                NotifyCollectionChangedAction.Replace,
                                new KeyValuePair<TKey, TValue>(key, value),
                                new KeyValuePair<TKey, TValue>(key, oldValue)
                            )
                        );
                        OnPropertyChanged(nameof(Count));
                    }
                }
                else
                {
                    Add(key, value);
                }
            }
        }

        public ICollection<TKey> Keys => _dictionary.Keys;
        public ICollection<TValue> Values => _dictionary.Values;
        public int Count => _dictionary.Count;
        public bool IsReadOnly => false;

        public void Add(TKey key, TValue value)
        {
            _dictionary.Add(key, value);
            OnCollectionChanged(
                new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Add,
                    new KeyValuePair<TKey, TValue>(key, value)
                )
            );
            OnPropertyChanged(nameof(Count));
        }

        public bool Remove(TKey key)
        {
            if (_dictionary.TryGetValue(key, out var value) && _dictionary.Remove(key))
            {
                OnCollectionChanged(
                    new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Remove,
                        new KeyValuePair<TKey, TValue>(key, value)
                    )
                );
                OnPropertyChanged(nameof(Count));
                return true;
            }
            return false;
        }

        public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);
        public bool TryGetValue(TKey key, out TValue value) => _dictionary.TryGetValue(key, out value);
        public void Clear()
        {
            _dictionary.Clear();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            OnPropertyChanged(nameof(Count));
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dictionary.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _dictionary.GetEnumerator();

        public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
        public bool Contains(KeyValuePair<TKey, TValue> item) => _dictionary.ContainsKey(item.Key);
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) =>
            ((IDictionary<TKey, TValue>)_dictionary).CopyTo(array, arrayIndex);
        public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);

        protected void OnCollectionChanged(NotifyCollectionChangedEventArgs e) =>
            CollectionChanged?.Invoke(this, e);

        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}


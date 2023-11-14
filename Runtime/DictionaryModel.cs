using System;
using System.Collections.Generic;
using System.IO;
using TinySerializer;

namespace PersistentX
{
    public class DictionaryModel<TK, TV> : ModelBase where TK : IEquatable<TK>
    {

        public event Action<TK> ItemRemoveEvent; 

        public event Action<TK, TV> ItemChangeEvent; 

        private enum ModifyCmd : byte
        {
            Init, Change, Remove
        }
        
        private readonly Dictionary<TK, TV> _dictionary = 
            new Dictionary<TK, TV>();

        public IReadOnlyDictionary<TK, TV> ReadOnlyDictionary => _dictionary;

        public TV this[TK key]
        {
            get => _dictionary.TryGetValue(key, out var ret) ? ret : default;
            set
            {
                _dictionary[key] = value;
                var stream = StartModify();
                stream.WriteByte((byte) ModifyCmd.Change);
                SerializationUtility.Serialize(stream, key);
                SerializationUtility.Serialize(stream, value);
                ItemChangeEvent?.Invoke(key, value);
            }
        }

        public bool ContainsKey(TK key) => _dictionary.ContainsKey(key);

        public void Remove(TK key)
        {
            _dictionary.Remove(key);
            var stream = StartModify();
            stream.WriteByte((byte) ModifyCmd.Remove);
            SerializationUtility.Serialize(stream, key);
            ItemRemoveEvent?.Invoke(key);
        }

        internal override void ApplyModify(Stream stream)
        {
            var cmd = (ModifyCmd) stream.ReadByte();
            switch (cmd)
            {
                case ModifyCmd.Change:
                {
                    var key = SerializationUtility.Deserialize<TK>(stream);
                    var value = SerializationUtility.Deserialize<TV>(stream);
                    _dictionary[key] = value;
                    break;
                }
                case ModifyCmd.Remove:
                {
                    var key = SerializationUtility.Deserialize<TK>(stream);
                    _dictionary.Remove(key);
                    break;
                }
                case ModifyCmd.Init:
                {
                    var count = SerializationUtility.Deserialize<int>(stream);
                    while (count-- > 0)
                    {
                        var key = SerializationUtility.Deserialize<TK>(stream);
                        var value = SerializationUtility.Deserialize<TV>(stream);
                        _dictionary[key] = value;
                    }
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        internal override void FullWrite(Stream stream)
        {
            stream.WriteByte((byte) ModifyCmd.Init);
            SerializationUtility.Serialize(stream, _dictionary.Count);
            foreach (var pair in _dictionary)
            {
                SerializationUtility.Serialize(stream, pair.Key);
                SerializationUtility.Serialize(stream, pair.Value);
            }
        }
    }
}
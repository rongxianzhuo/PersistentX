using System;
using System.Collections.Generic;
using System.IO;
using TinySerializer;

namespace PersistentX
{
    public class HashSetModel<T> : ModelBase where T : IEquatable<T>
    {

        public event Action<T> ItemRemoveEvent; 

        public event Action<T> ItemAddEvent; 

        private enum ModifyCmd : byte
        {
            Init, Add, Remove
        }
        
        private readonly HashSet<T> _hashSet = 
            new HashSet<T>();

        public IReadOnlyCollection<T> ReadOnlySet => _hashSet;

        public void Remove(T key)
        {
            if (!_hashSet.Contains(key)) return;
            _hashSet.Remove(key);
            var stream = StartModify();
            stream.WriteByte((byte) ModifyCmd.Remove);
            SerializationUtility.Serialize(stream, key);
            ItemRemoveEvent?.Invoke(key);
        }

        public void Add(T key)
        {
            if (_hashSet.Contains(key)) return;
            _hashSet.Add(key);
            var stream = StartModify();
            stream.WriteByte((byte) ModifyCmd.Add);
            SerializationUtility.Serialize(stream, key);
            ItemAddEvent?.Invoke(key);
        }

        internal override void ApplyModify(Stream stream)
        {
            var cmd = (ModifyCmd) stream.ReadByte();
            switch (cmd)
            {
                case ModifyCmd.Add:
                {
                    var key = SerializationUtility.Deserialize<T>(stream);
                    _hashSet.Add(key);
                    break;
                }
                case ModifyCmd.Remove:
                {
                    var key = SerializationUtility.Deserialize<T>(stream);
                    _hashSet.Remove(key);
                    break;
                }
                case ModifyCmd.Init:
                {
                    var count = SerializationUtility.Deserialize<int>(stream);
                    while (count-- > 0)
                    {
                        var key = SerializationUtility.Deserialize<T>(stream);
                        _hashSet.Add(key);
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
            SerializationUtility.Serialize(stream, _hashSet.Count);
            foreach (var key in _hashSet)
            {
                SerializationUtility.Serialize(stream, key);
            }
        }
    }
}
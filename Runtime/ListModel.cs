using System;
using System.Collections.Generic;
using System.IO;
using TinySerializer;

namespace PersistentX
{
    public class ListModel<T> : ModelBase
    {
        
        private enum ModifyCmd : byte
        {
            Init, Change, Clear, Add
        }

        public event Action ItemAddEvent;

        private readonly List<T> _list = new List<T>();

        public int Count => _list.Count;

        public IReadOnlyList<T> ReadOnlyList => _list;

        public T this[int index]
        {
            get => _list[index];
            set
            {
                _list[index] = value;
                var stream = StartModify();
                stream.WriteByte((byte) ModifyCmd.Change);
                SerializationUtility.Serialize(stream, index);
                SerializationUtility.Serialize(stream, value);
            }
        }

        public void Clear()
        {
            _list.Clear();
            var stream = StartModify();
            stream.WriteByte((byte) ModifyCmd.Clear);
        }

        public void Add(T t)
        {
            _list.Add(t);
            var stream = StartModify();
            stream.WriteByte((byte) ModifyCmd.Add);
            SerializationUtility.Serialize(stream, t);
            ItemAddEvent?.Invoke();
        }

        internal override void ApplyModify(Stream stream)
        {
            var cmd = (ModifyCmd) stream.ReadByte();
            switch (cmd)
            {
                case ModifyCmd.Init:
                {
                    var count = SerializationUtility.Deserialize<int>(stream);
                    _list.Clear();
                    for (var i = 0; i < count; i++)
                    {
                        _list.Add(SerializationUtility.Deserialize<T>(stream));
                    }
                    break;
                }
                case ModifyCmd.Change:
                {
                    var index = SerializationUtility.Deserialize<int>(stream);
                    _list[index] = SerializationUtility.Deserialize<T>(stream);
                    break;
                }
                case ModifyCmd.Clear:
                {
                    _list.Clear();
                    break;
                }
                case ModifyCmd.Add:
                {
                    _list.Add(SerializationUtility.Deserialize<T>(stream));
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        internal override void FullWrite(Stream stream)
        {
            stream.WriteByte((byte) ModifyCmd.Init);
            SerializationUtility.Serialize(stream, _list.Count);
            foreach (var t in _list)
            {
                SerializationUtility.Serialize(stream, t);
            }
        }
    }
}
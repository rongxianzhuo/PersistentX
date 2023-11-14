using System;
using System.Collections.Generic;
using System.IO;
using TinySerializer;
using UnityEngine;

namespace PersistentX
{
    public class ArrayModel<T> : ModelBase
    {
        
        private enum ModifyCmd : byte
        {
            Init, Change, Clear, ClearByArray
        }

        public event Action<int, T> ItemChangeEvent; 
        
        private T[] _internalArray;

        public int Length => _internalArray.Length;

        public IReadOnlyList<T> ReadOnlyList => _internalArray;

        public T this[int index]
        {
            get => _internalArray[index];
            set
            {
                _internalArray[index] = value;
                var stream = StartModify();
                stream.WriteByte((byte) ModifyCmd.Change);
                SerializationUtility.Serialize(stream, index);
                SerializationUtility.Serialize(stream, value);
                ItemChangeEvent?.Invoke(index, value);
            }
        }

        public ArrayModel(T[] defaultData)
        {
            _internalArray = defaultData;
        }

        public void Clear(T t)
        {
            for (var i = 0; i < _internalArray.Length; i++)
            {
                _internalArray[i] = t;
            }

            var stream = StartModify();
            stream.WriteByte((byte) ModifyCmd.Clear);
            SerializationUtility.Serialize(stream, t);
            if (ItemChangeEvent == null) return;
            {
                for (var i = 0; i < _internalArray.Length; i++)
                {
                    ItemChangeEvent(i, _internalArray[i]);
                }
            }
        }

        public void Clear(T[] array)
        {
            for (var i = 0; i < array.Length; i++)
            {
                _internalArray[i] = array[i];
            }

            var stream = StartModify();
            stream.WriteByte((byte) ModifyCmd.ClearByArray);
            foreach (var t in array)
            {
                SerializationUtility.Serialize(stream, t);
            }
            if (ItemChangeEvent == null) return;
            {
                for (var i = 0; i < _internalArray.Length; i++)
                {
                    ItemChangeEvent(i, _internalArray[i]);
                }
            }
        }

        internal override void ApplyModify(Stream stream)
        {
            var cmd = (ModifyCmd) stream.ReadByte();
            switch (cmd)
            {
                case ModifyCmd.Init:
                {
                    var length = SerializationUtility.Deserialize<int>(stream);
                    if (_internalArray == null) _internalArray = new T[length];
                    else if (_internalArray.Length != length) Debug.LogWarning("Saving array length not match.");
                    for (var i = 0; i < length && i < _internalArray.Length; i++)
                    {
                        _internalArray[i] = SerializationUtility.Deserialize<T>(stream);
                    }
                    break;
                }
                case ModifyCmd.Change:
                {
                    var index = SerializationUtility.Deserialize<int>(stream);
                    _internalArray[index] = SerializationUtility.Deserialize<T>(stream);
                    break;
                }
                case ModifyCmd.Clear:
                {
                    var clearValue = SerializationUtility.Deserialize<T>(stream);
                    for (var i = 0; i < _internalArray.Length; i++)
                    {
                        _internalArray[i] = clearValue;
                    }
                    break;
                }
                case ModifyCmd.ClearByArray:
                {
                    for (var i = 0; i < _internalArray.Length; i++)
                    {
                        var clearValue = SerializationUtility.Deserialize<T>(stream);
                        _internalArray[i] = clearValue;
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
            SerializationUtility.Serialize(stream, _internalArray.Length);
            foreach (var t in _internalArray)
            {
                SerializationUtility.Serialize(stream, t);
            }
        }
        
    }
}
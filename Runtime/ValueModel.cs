using System;
using System.IO;
using TinySerializer;

namespace PersistentX
{
    public class ValueModel<T> : ModelBase where T : IEquatable<T>
    {

        public event Action<T> ValueChangeEvent;

        private T _value;

        public T Value
        {
            get => _value;
            set
            {
                if (_value.Equals(value)) return;
                _value = value;
                SerializationUtility.Serialize(Owner.StartModify(this), _value);
                ValueChangeEvent?.Invoke(value);
            }
        }

        public ValueModel(T defaultValue)
        {
            _value = defaultValue;
        }

        internal override void ApplyModify(Stream stream)
        {
            _value = SerializationUtility.Deserialize<T>(stream);
        }

        internal override void FullWrite(Stream stream)
        {
            SerializationUtility.Serialize(stream, _value);
        }
    }
}
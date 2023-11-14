using System;
using System.Collections.Generic;
using System.IO;
using TinySerializer;

namespace PersistentX
{
    public class Database
    {

        private readonly ModelBase[] _models;

        private FileStream _recordingStream;

        public bool IsLoaded { get; private set; }

        public Database()
        {
            var fields = GetType().GetFields();
            Array.Sort(fields, (i1, i2) => string.Compare(i1.Name, i2.Name, StringComparison.Ordinal));
            var modelList = new List<ModelBase>();
            foreach (var field in fields)
            {
                if (field.GetValue(this) is not ModelBase model) continue;
                model.ModelId = modelList.Count;
                model.Owner = this;
                modelList.Add(model);
            }
            _models = modelList.ToArray();
        }

        internal Stream StartModify(ModelBase model)
        {
            SerializationUtility.Serialize(_recordingStream, model.ModelId);
            return _recordingStream;
        }

        public void Load(string savePath)
        {
            if (IsLoaded) return;
            IsLoaded = true;

            var parentDirectory = Directory.GetParent(savePath);
            if (parentDirectory != null && !parentDirectory.Exists)
            {
                Directory.CreateDirectory(parentDirectory.FullName);
            }

            if (File.Exists(savePath))
            {
                var stream = File.OpenRead(savePath);
                while (stream.Position < stream.Length)
                {
                    var modelId = SerializationUtility.Deserialize<int>(stream);
                    _models[modelId].ApplyModify(stream);
                }
                stream.Close();
                stream.Dispose();
                File.Delete(savePath);
            }

            _recordingStream = File.Open(savePath, FileMode.Append, FileAccess.Write);
            foreach (var model in _models)
            {
                SerializationUtility.Serialize(_recordingStream, model.ModelId);
                model.FullWrite(_recordingStream);
            }
            OnLoad();
        }

        public void Release()
        {
            if (_recordingStream == null) return;
            _recordingStream.Flush();
            _recordingStream.Close();
            _recordingStream.Dispose();
            _recordingStream = null;
            OnRelease();
        }

        protected virtual void OnLoad()
        {
            
        }

        protected virtual void OnRelease()
        {
            
        }
        
    }
}
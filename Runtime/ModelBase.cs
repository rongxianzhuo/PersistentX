using System.IO;

namespace PersistentX
{
    public abstract class ModelBase
    {

        internal int ModelId;

        internal Database Owner;

        protected Stream StartModify()
        {
            return Owner.StartModify(this);
        }

        internal abstract void ApplyModify(Stream stream);

        internal abstract void FullWrite(Stream stream);

    }
}
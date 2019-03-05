using System;

namespace Neo.IO.Caching
{
    public abstract class MetaDataCache<T>
        where T : class, ICloneable<T>, ISerializable, new()
    {
        protected T Item;
        protected TrackState State;
        protected readonly Func<T> factory;

        protected abstract void AddInternal(T item);
        protected abstract T TryGetInternal();
        protected abstract void UpdateInternal(T item);

        protected MetaDataCache(Func<T> factory)
        {
            this.factory = factory;
        }

        public abstract void Commit(UInt64 height);

        protected void Commit()
        {
            switch (State)
            {
                case TrackState.Added:
                    AddInternal(Item);
                    break;
                case TrackState.Changed:
                    UpdateInternal(Item);
                    break;
            }
        }

        public MetaDataCache<T> CreateSnapshot()
        {
            return new CloneMetaCache<T>(this);
        }

        public T Get()
        {
            if (Item == null)
            {
                Item = TryGetInternal();
            }
            if (Item == null)
            {
                Item = factory?.Invoke() ?? new T();
                State = TrackState.Added;
            }
            return Item;
        }

        public T GetAndChange()
        {
            T item = Get();
            if (State == TrackState.None)
                State = TrackState.Changed;
            return item;
        }
    }
}

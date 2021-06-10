using System;
using System.Windows.Media;

namespace CompMs.Graphics.Base
{
    public interface IBrushMapper {
        Brush Map(object key);
    }

    public interface IBrushMapper<in T> : IBrushMapper {
        Brush Map(T key);
    }

    public abstract class BrushMapper<T> : IBrushMapper<T>
    {
        public abstract Brush Map(T key);

        public virtual Brush Map(object key) {
            if (key is T Tkey) {
                return Map(Tkey);
            }
            throw new NotSupportedException($"Type {key.GetType()} is not suported.");
        }
    }
}

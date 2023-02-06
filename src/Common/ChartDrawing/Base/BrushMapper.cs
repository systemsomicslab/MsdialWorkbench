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

    public static class BrushMapperExtensions {
        public static IBrushMapper<U> Contramap<T, U>(this IBrushMapper<T> mapper, Func<U, T> consumer) {
            return new ContravariantBrushMapper<U, T>(mapper, consumer);
        }

        class ContravariantBrushMapper<T, U> : IBrushMapper<T>
        {
            private readonly IBrushMapper<U> _mapper;
            private readonly Func<T, U> _consumer;

            public ContravariantBrushMapper(IBrushMapper<U> mapper, Func<T, U> consumer) {
                _mapper = mapper;
                _consumer = consumer;
            }

            public Brush Map(T key) => _mapper.Map(_consumer(key));

            public Brush Map(object key) => _mapper.Map(_consumer((T)key));
        }
    }
}

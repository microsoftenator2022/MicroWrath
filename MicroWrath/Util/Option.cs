using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MicroWrath.Util
{
    public abstract class Option<T> : IEquatable<T>, IEquatable<Option<T>>
    {
        private Option() { }

        public T? Value { get; init; }

        public abstract bool IsSome { get; }
        public abstract bool IsNone { get; }

        public abstract bool Equals(Option<T> other);
        public abstract bool Equals(T other);

        public static bool operator ==(Option<T> a, T other) => a.Equals(other);
        public static bool operator !=(Option<T> a, T other) => !a.Equals(other);

        public override bool Equals(object obj) => obj.Equals(Value);
        public override int GetHashCode() => EqualityComparer<T?>.Default.GetHashCode(Value);

        public static NoneType None = new();

        public class NoneType : Option<T>
        {
            internal NoneType() { }

            public override bool IsSome => false;
            public override bool IsNone => true;

            public bool Equals(NoneType _) => true;

            public override bool Equals(Option<T> other) => other is NoneType;
            public override bool Equals(T other) => other is null;

            public override string ToString() => $"None";

            public static explicit operator T?(NoneType _) => default;
        }

        public class Some : Option<T>
        {
            public Some(T value)
            {
                if (value is null) throw new ArgumentException("'Some' value cannot be null", nameof(value));

                base.Value = value;
            }

            public override bool IsSome => true;
            public override bool IsNone => false;

            public new T Value => base.Value!;

            public override bool Equals(Option<T> other) =>
                other is Some some &&
                this.Value!.Equals(some.Value);

            public override bool Equals(T other) => other is not null && other.Equals(Value);

            public override string ToString() => $"Some {Value}";

            public static implicit operator T(Some some) => some.Value;
        }

        public static explicit operator Option<T>(T? option) => option is null ? new Option<T>.NoneType() : new Some(option);
        public static explicit operator T?(Option<T> option) => option.Value;
    }

    public static class Option
    {
        public static Option<T>.Some Some<T>(T value) => new(value);
        public static Option<T>.NoneType None<T>() => Option<T>.None;

        public static Option<T> OfObj<T>(T? value) => (Option<T>)value;

        public static Option<T> ToOption<T>(this T? obj) => (Option<T>)obj;
        public static Option<T> ToOption<T>(this Option<T> option) => option;

        public static Option<U> Map<T, U>(this Option<T> option, Func<T, U> mapper) =>
            option is Option<T>.Some value ?
                Some(mapper(value)) :
                None<U>();

        public static Option<U> Bind<T, U>(this Option<T> option, Func<T, Option<U>> binder) =>
            option is Option<T>.Some value ?
                binder(value) :
                None<U>();

        public static T Cast<T>(this Option<T>.Some option) => option;
        public static T? Cast<T>(this Option<T>.NoneType _) => default;

        public static IEnumerable<U> Choose<T, U>(this IEnumerable<T> source, Func<T, Option<U>> chooser) =>
            source.Select(chooser).OfType<Option<U>.Some>().Select(some => some.Value);

        public static IEnumerable<T> ToEnumerable<T>(this Option<T> option)
        {
            if (option is Option<T>.Some some) yield return some;
        }

        public static Option<T> TryHead<T>(this IEnumerable<T> source) => source.FirstOrDefault().ToOption();
        public static Option<T> TryFind<T>(this IEnumerable<T> source, Func<T, bool> predicate) => source.FirstOrDefault(predicate).ToOption();

        public static T DefaultValue<T>(this Option<T> option, T defaultValue) => (T?)option ?? defaultValue;
    }
}

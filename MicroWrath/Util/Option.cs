using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MicroWrath.Util
{
    /// <summary>
    /// Option type. Roughly mimics F#'s Option type
    /// </summary>
    public abstract class Option<T> : IEquatable<T>, IEquatable<Option<T>>
    {
        private Option() { }

        /// <summary>
        /// Value. If this is <see cref="NoneType">None</see>, returns <see langword="default"/>
        /// </summary>
        public T? Value { get; init; }

#pragma warning disable CS1591
        public abstract bool IsSome { get; }
        public abstract bool IsNone { get; }

        public abstract bool Equals(Option<T> other);
        public abstract bool Equals(T other);

        public static bool operator ==(Option<T> a, T other) => a.Equals(other);
        public static bool operator !=(Option<T> a, T other) => !a.Equals(other);

        public override bool Equals(object obj) => obj.Equals(Value);
        public override int GetHashCode() => EqualityComparer<T?>.Default.GetHashCode(Value);
#pragma warning restore CS1591

        /// <summary>
        /// Canonical <see cref="NoneType">None</see> instance.
        /// </summary>
        public static readonly NoneType None = new();

        /// <summary>
        /// Represent an absent or undefined value.
        /// </summary>
        public class NoneType : Option<T>
        {
            internal NoneType() { }

#pragma warning disable CS1591
            public override bool IsSome => false;
            public override bool IsNone => true;

            public bool Equals(NoneType _) => true;

            public override bool Equals(Option<T> other) => other is NoneType;
            public override bool Equals(T other) => other is null;

            public override string ToString() => $"None";
#pragma warning restore CS1591

            /// <summary>
            /// Cast <see cref="Option{T}.NoneType"/> to <typeparamref name="T"/>?. Always returns <see langword="default"/>.
            /// </summary>
            public static explicit operator T?(NoneType _) => default;
        }

        /// <summary>
        /// Represents a defined value.
        /// </summary>
        public class Some : Option<T>
        {
            /// <exception cref="ArgumentException">If <paramref name="value"/> is <see langword="default"/>.</exception>
            public Some(T value)
            {
                if (value is null) throw new ArgumentException("'Some' value cannot be null", nameof(value));

                base.Value = value;
            }

#pragma warning disable CS1591
            public override bool IsSome => true;
            public override bool IsNone => false;

            public new T Value => base.Value!;

            public override bool Equals(Option<T> other) =>
                other is Some some &&
                this.Value!.Equals(some.Value);

            public override bool Equals(T other) => other is not null && other.Equals(Value);

            public override string ToString() => $"Some {Value}";
#pragma warning restore CS1591

            /// <summary>
            /// Implicit cast to <typeparamref name="T"/>.
            /// </summary>
            public static implicit operator T(Some some) => some.Value;
        }

        /// <summary>
        /// Cast to <typeparamref name="T"/> to <see cref="Option{T}"/>. If <paramref name="option"/> is <see langword="default"/>, this returns <see cref="Option{T}.None"/>,
        /// otherwise returns <see cref="Option{T}.Some"/>
        /// </summary>
        public static explicit operator Option<T>(T? option) => option is null ? new Option<T>.NoneType() : new Some(option);

        /// <summary>
        /// Cast <see cref="Option{T}"/> to <typeparamref name="T"/>?. If <paramref name="option"/> is <see cref="Option{T}.None"/> returns <see langword="default"/>.
        /// </summary>
        public static explicit operator T?(Option<T> option) => option.Value;
    }

    /// <summary>
    /// Utility and extension methods for <see cref="Option{T}"/>
    /// </summary>
    public static class Option
    {
        /// <summary>
        /// Creates an <see cref="Option{T}"/>
        /// </summary>
        public static Option<T>.Some Some<T>(T value) => new(value);

        /// <returns><see cref="Option{T}.None"/></returns>
        public static Option<T>.NoneType None<T>() => Option<T>.None;

#pragma warning disable CS1591
        public static bool IsSome<T>(this Option<T> option) => option.IsSome;
        public static bool IsNone<T>(this Option<T> option) => option.IsNone;
#pragma warning restore CS1591

        /// <summary>
        /// Creates an <see cref="Option{T}"/>
        /// </summary>
        /// <returns><see cref="Option{T}.None"/> is <paramref name="value"/> is <see langword="default"/>. Otherwise, <see cref="Option{T}.Some"/>.</returns>
        public static Option<T> OfObj<T>(T? value) => (Option<T>)value;

        /// <inheritdoc cref="Option.OfObj{T}"/>
        public static Option<T> ToOption<T>(this T? obj) => (Option<T>)obj;

#pragma warning disable CS1591
        [Obsolete]
        public static Option<T> ToOption<T>(this Option<T> option) => option;
#pragma warning restore CS1591

#pragma warning disable CS1591
        public static Option<U> Map<T, U>(this Option<T> option, Func<T, U> mapper) =>
            option is Option<T>.Some value ?
                Some(mapper(value)) :
                None<U>();

        public static Option<U> Bind<T, U>(this Option<T> option, Func<T, Option<U>> binder) =>
            option is Option<T>.Some value ?
                binder(value) :
                None<U>();
#pragma warning restore CS1591

        /// <summary>
        /// Retrieve value from <see cref="Option{T}.Some"/>
        /// </summary>
        /// <returns><typeparamref name="T"/> value</returns>
        public static T Cast<T>(this Option<T>.Some option) => option;

        /// <returns>Always returns <see langword="default"/></returns>
        public static T? Cast<T>(this Option<T>.NoneType _) => default;

        /// <summary>
        /// Applies a selector function to <paramref name="source"/>, returning only the elements where it returns <see cref="Option.Some{U}"/>.
        /// </summary>
        public static IEnumerable<U> Choose<T, U>(this IEnumerable<T> source, Func<T, Option<U>> chooser) =>
            source.Select(chooser).OfType<Option<U>.Some>().Select(some => some.Value);

        /// <returns>If <paramref name="option"/> is <see cref="Option{T}.Some"/>, returns a single element sequence. Otherwise returns an empty sequence</returns>
        public static IEnumerable<T> ToEnumerable<T>(this Option<T> option)
        {
            if (option is Option<T>.Some some) yield return some;
        }

        /// <returns>First element in <paramref name="source"/> or <see cref="Option{T}.None"/>.</returns>
        public static Option<T> TryHead<T>(this IEnumerable<T> source) => source.FirstOrDefault().ToOption();

        /// <summary>
        /// Applies a predicate to <paramref name="source"/>, returning the first element where it returns <see langword="true"/>.
        /// If no matching elements are found, returns <see cref="Option{T}.None"/>
        /// </summary>
        public static Option<T> TryFind<T>(this IEnumerable<T> source, Func<T, bool> predicate) => source.FirstOrDefault(predicate).ToOption();

        /// <returns>If <paramref name="option"/> is <see cref="Option{T}.Some"/>, returns its value. Otherwise returns <paramref name="defaultValue"/>.</returns>
        public static T DefaultValue<T>(this Option<T> option, T defaultValue) => (T?)option ?? defaultValue;
    }
}

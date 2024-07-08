using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;

namespace MicroWrath.Util
{
    /// <summary>
    /// Struct representation of an optional value.
    /// </summary>
    public readonly struct Option<T> : IEquatable<Option<T>>, IEquatable<T?>, IEnumerable<T> where T : notnull
    {
        /// <summary>
        /// <see langword="default"/> if <see cref="None"/>
        /// </summary>
        public readonly T MaybeValue;

        /// <summary>
        /// Throws <see cref="NullReferenceException"/> if <see cref="None"/>
        /// </summary>
        public T Value => MaybeValue ?? throw new NullReferenceException();

        /// <summary>
        /// True if <see cref="Some(T)"/>
        /// </summary>
        public readonly bool IsSome;

        /// <summary>
        /// True if <see cref="None"/>
        /// </summary>
        public bool IsNone => !IsSome;

        Option(T value)
        {
            MaybeValue = value;
            IsSome = true;
        }

        /// <summary>
        /// <see cref="None"/>
        /// </summary>
        public Option()
        {
            MaybeValue = default!;
        }

        /// <summary>
        /// Creates a new <see cref="Some(T)"/> value.
        /// </summary>
        public static Option<T> Some(T value) => new(value);

        /// <summary>
        /// Represents "no value".
        /// </summary>
        public static readonly Option<T> None = new();


        /// <inheritdoc cref="Object.ToString" />
        public override string ToString() => this.IsSome ? $"Some {this.Value}" : "None";

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">Attempted to compare to a null value.</exception>
        public bool Equals(Option<T> other)
        {
            if (this.IsNone)
                return other.IsNone;

            return this.MaybeValue?.Equals(other.MaybeValue) ??
                throw new InvalidOperationException(
                    $"null is an invalid value for {typeof(Option<T>)}.{nameof(Some)}");
        }

        /// <inheritdoc />
        public static bool operator ==(Option<T> a, Option<T> b) => a.Equals(b);
        /// <inheritdoc />
        public static bool operator !=(Option<T> a, Option<T> b) => !a.Equals(b);

        /// <inheritdoc />
        public bool Equals(T? other) => this switch
        {
            var some when some.IsSome => some.Value.Equals(other),
            _ => other is null,
        };

        /// <inheritdoc />
        public static bool operator ==(Option<T> a, T? b) => a.Equals(b);
        /// <inheritdoc />
        public static bool operator !=(Option<T> a, T? b) => !a.Equals(b);
        /// <inheritdoc />
        public static bool operator ==(T? a, Option<T> b) => b.Equals(a);
        /// <inheritdoc />
        public static bool operator !=(T? a, Option<T> b) => !b.Equals(a);

        /// <inheritdoc />
        public override bool Equals(object obj) =>
            base.Equals(obj) ||
            (obj is Option<T> option && this.Equals(option)) ||
            (this.MaybeValue is null && obj is null) ||
            (this.MaybeValue?.Equals(obj) ?? false);

        /// <inheritdoc cref="Object.GetHashCode"/>
        public override int GetHashCode() => HashCode.Combine(MaybeValue);

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator()
        {
            if (IsSome)
                yield return MaybeValue!;
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }

    /// <summary>
    /// Extension methods for <see cref="Option{T}" />
    /// </summary>
    public static class Option
    {
        /// <inheritdoc cref="Option{T}.Some(T)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<T> Some<T>(T value) where T : notnull => Option<T>.Some(value);
        /// <inheritdoc cref="Option{T}.None" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<T> None<T>() where T : notnull => Option<T>.None;

        /// <summary>
        /// Creates an <see cref="Option{T}"/> from a nullable value of type <typeparamref name="T"/>
        /// </summary>
        public static Option<T> OfObj<T>(T? obj) where T : notnull =>
            obj switch
            {
                not null => Some(obj),
                _ => None<T>()
            };

        /// <inheritdoc cref="Option.OfObj{T}(T)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<T> ToOption<T>(this T? obj) where T : notnull => OfObj(obj);

        /// <summary>
        /// Converts an optional value to a value of type <typeparamref name="T"/> or null.
        /// </summary>
        /// <returns>Value of type <typeparamref name="T"/> or null.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T? ToObj<T>(Option<T> option) where T : notnull => option.Value;

        /// <inheritdoc cref="Option{T}.IsSome"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSome<T>(Option<T> option) where T : notnull => option.IsSome;

        /// <inheritdoc cref="Option{T}.IsNone"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNone<T>(Option<T> option) where T : notnull => option.IsNone;


#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<A> Return<A>(A value) where A : notnull => Some(value);

        public static Func<Option<A>, Option<B>> Bind<A, B>(Func<A, Option<B>> binder)
            where A : notnull
            where B : notnull =>
            option => option switch
            {
                var some when some.IsSome => binder(some.Value),
                _ => Option<B>.None
            };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<B> Bind<A, B>(this Option<A> option, Func<A, Option<B>> binder)
            where A : notnull
            where B : notnull =>
            Bind(binder)(option);

        public static Func<Option<A>, Option<B>> Lift<A, B>(Func<A, B> f)
            where A : notnull
            where B : notnull =>
            option => option switch
            {
                var some when some.IsSome => ToOption(f(some.Value)),
                _ => Option<B>.None
            };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<B> Map<A, B>(this Option<A> option, Func<A, B> f)
            where A : notnull
            where B : notnull =>
            Lift(f)(option);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Func<Option<A>, Option<B>> Apply<A, B>(Option<Func<A, B>> lifted)
            where A : notnull
            where B : notnull =>
            option => lifted.Bind(f => option.Map(f));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<B> Apply<A, B>(this Option<Func<A, B>> lifted, Option<A> option)
            where A : notnull
            where B : notnull =>
            Apply(lifted)(option);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<C> Apply2<A, B, C>(this Option<Func<A, B, C>> lifted, Option<A> optionA, Option<B> optionB)
            where A : notnull
            where B : notnull
            where C : notnull =>
            lifted.Map(Functional.Curry).Apply(optionA).Apply(optionB);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <summary>
        /// Applies function <paramref name="chooser"/> to each element of the sequence and
        /// returns a sequence containing results where this function returns <see cref="Option.Some{T}"/>
        /// </summary>
        /// <param name="source">Source sequence with elements of type <typeparamref name="T"/></param>
        /// <param name="chooser">Function to transform elements of type <typeparamref name="T"/> into type <see cref="Option{U}"/></param>
        /// <returns>Sequence of type <typeparamref name="U"/></returns>
        public static IEnumerable<U> Choose<T, U>(this IEnumerable<T> source, Func<T, Option<U>> chooser) where U : notnull =>
            //source.SelectMany(chooser);
            // :owlcat_suspecting:
            source.SelectMany(x => chooser(x));

        /// <summary>
        /// Returns the first element of the sequence or <see cref="Option{T}.None"/> if the sequence is empty.
        /// </summary>
        /// <returns><see cref="Option{T}.Some"/> of the first element or <see cref="Option{T}.None"/> if the sequence is empty.</returns>
        public static Option<T> TryHead<T>(this IEnumerable<T> source) where T : notnull
        {
            foreach (var x in source)
                return Some(x);

            return None<T>();
        }

        /// <summary>
        /// Returns the first element of the sequence where the <paramref name="predicate"/> function returns true. If no element is found, returns <see cref="Option{T}.None"/>
        /// </summary>
        /// <returns>The matching element or <see cref="Option{T}.None"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<T> TryFind<T>(this IEnumerable<T> source, Func<T, bool> predicate) where T : notnull =>
            source.Where(predicate).TryHead();

        /// <summary>
        /// Returns the value of <paramref name="option"/> or <paramref name="defaultValue"/> if it is <see cref="Option{T}.None"/>
        /// </summary>
        /// <returns>The value of <paramref name="option"/> or <paramref name="defaultValue"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T DefaultValue<T>(this Option<T> option, T defaultValue) where T : notnull =>
            option switch
            {
                var some when some.IsSome => some.Value,
                _ => defaultValue
            };
    }
}

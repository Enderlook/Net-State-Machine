using System;
using System.Runtime.CompilerServices;

namespace Enderlook.StateMachine;

public readonly partial struct Parameter
{
    private readonly struct FakeMemory
    {
        // Note: This type must have the same layout of System.Memory<T> and System.ReadOnlyMemor<T>.
        private readonly object? @object;
        private readonly int index;
        private readonly int length;

        public FakeMemory(object? @object, int index, int length)
        {
            this.@object = @object;
            this.index = index;
            this.length = length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Memory<T> To<T>(in Parameter parameter)
        {
            FakeMemory value = new(parameter.@object, parameter.union.Memory.Index, parameter.union.Memory.Length);
            return Unsafe.As<FakeMemory, Memory<T>>(ref value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Parameter From<T>(in Memory<T> value)
        {
            ref readonly FakeMemory fakeMemory = ref Unsafe.As<Memory<T>, FakeMemory>(ref Unsafe.AsRef(value));
            return new Parameter(fakeMemory.@object, new(fakeMemory.index, fakeMemory.length), MemoryHelper<T, No>.Singlenton);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Parameter From<T>(in ReadOnlyMemory<T> value)
        {
            ref readonly FakeMemory fakeMemory = ref Unsafe.As<ReadOnlyMemory<T>, FakeMemory>(ref Unsafe.AsRef(value));
            return new Parameter(fakeMemory.@object, new(fakeMemory.index, fakeMemory.length), MemoryHelper<T, Yes>.Singlenton);
        }
    }
}
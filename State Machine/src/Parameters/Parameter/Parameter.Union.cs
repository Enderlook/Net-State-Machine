using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Enderlook.StateMachine;

public readonly partial struct Parameter
{
    [StructLayout(LayoutKind.Explicit)]
    private readonly struct Union
    {
        [FieldOffset(0)] public readonly Container Storage;
        [FieldOffset(0)] public readonly (int Offset, int Count) ArraySegment;
        [FieldOffset(0)] public readonly (int Index, int Length) Memory;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Union(int a, int b)
        {
            this = default;
            ArraySegment = (a, b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Union(Container value)
        {
            this = default;
            Storage = value;
        }

        public readonly struct Container
        {
            private readonly long a;
            private readonly long b;
        }
    }
}
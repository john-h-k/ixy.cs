using System;
using System.Runtime.CompilerServices;

namespace IxyCs
{
    public class ThrowHelper
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowInvalidOperationException(string message) 
            => throw new InvalidOperationException(message);
    }
}
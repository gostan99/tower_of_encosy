#if UNITASK || UNITY_6000_0_OR_NEWER

using System.Runtime.CompilerServices;
using Module.Core.Processing;
using UnityEngine;

namespace Module.Core.Extended.Processing
{
    public static class WorldProcessor
    {
        private static Processor s_instance;

        public static Processor Instance => s_instance ??= new();

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            s_instance?.Dispose();
            s_instance = null;
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ProcessHub<TScope> Scope<TScope>(TScope scope)
            => Instance.Scope(scope);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ProcessHub<TScope> Scope<TScope>()
            where TScope : struct
            => Instance.Scope<TScope>();
    }
}

#endif

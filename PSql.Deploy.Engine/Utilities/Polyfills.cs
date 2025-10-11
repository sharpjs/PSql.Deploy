// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

#if !NET7_0_OR_GREATER

using System.ComponentModel;

using static System.AttributeTargets;
using static System.ComponentModel.EditorBrowsableState;

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(Constructor, AllowMultiple = false, Inherited = false)]
    [EditorBrowsable(Never)]
    [ExcludeFromCodeCoverage]
    internal sealed class SetsRequiredMembersAttribute : Attribute {}
}

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(Class | Struct | Field | Property, AllowMultiple = false, Inherited = false)]
    [EditorBrowsable(Never)]
    [ExcludeFromCodeCoverage]
    internal sealed class RequiredMemberAttribute : Attribute {}

    [AttributeUsage(All, AllowMultiple = true, Inherited = false)]
    [EditorBrowsable(Never)]
    [ExcludeFromCodeCoverage]
    internal sealed class CompilerFeatureRequiredAttribute : Attribute
    {
        public CompilerFeatureRequiredAttribute(string featureName)
            => FeatureName = featureName;

        public string FeatureName { get; }
        public bool   IsOptional  { get; init; }
    }
}

#endif

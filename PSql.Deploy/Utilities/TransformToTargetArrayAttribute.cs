// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

/// <summary>
/// 
/// </summary>
public class TransformToTargetArrayAttribute : ArgumentTransformationAttribute
{
    /// <inheritdoc/>
    public override bool TransformNullOptionalParameters => false;

    /// <inheritdoc/>
    public override object Transform(EngineIntrinsics engineIntrinsics, object inputData)
    {
        return Coerce.ToTargetArrayRequired(inputData);
    }
}

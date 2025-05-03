// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

/// <summary>
/// 
/// </summary>
public class TransformToTargetSetAttribute : ArgumentTransformationAttribute
{
    /// <inheritdoc/>
    public override bool TransformNullOptionalParameters => false;

    /// <inheritdoc/>
    public override object Transform(EngineIntrinsics engineIntrinsics, object inputData)
    {
        return Coerce.ToTargetSetArrayRequired(inputData);
    }
}

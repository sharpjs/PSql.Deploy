// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

/// <summary>
/// 
/// </summary>
public class TransformToTargetAttribute : ArgumentTransformationAttribute
{
    /// <inheritdoc/>
    public override object Transform(EngineIntrinsics engineIntrinsics, object inputData)
    {
        return Coerce.ToTargetRequired(inputData);
    }
}

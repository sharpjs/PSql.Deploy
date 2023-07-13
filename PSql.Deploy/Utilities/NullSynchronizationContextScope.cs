// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Utilities;

internal readonly ref struct NullSynchronizationContextScope
{
    private readonly SynchronizationContext? _savedContext;

    public NullSynchronizationContextScope()
    {
        _savedContext = SynchronizationContext.Current;

        if (_savedContext is not null)
            SynchronizationContext.SetSynchronizationContext(null);
    }

    public void Dispose()
    {
        if (_savedContext is not null)
            SynchronizationContext.SetSynchronizationContext(_savedContext);
    }
}

// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

// FUTURE: In PS 7.4 / .NET 8, this can be replaced with a tuple alias.
//         In PS 7.2 / .NET 6, the tuple alias crahes a source generator.
//
// Example:
// using ObjectTypePair = (object Object, Type Type);

internal readonly record struct ObjectTypePair(object Object, Type Type);

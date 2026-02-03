// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Style",
    "IDE0130: Namespace does not match folder structure",
    Justification = nameof(System.Reflection)
    )]
[assembly: SuppressMessage(
    "Design",
    "CA1056: URI-like properties should not be strings",
    Justification = nameof(System.Reflection)
    )]

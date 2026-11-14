
using ZY.Core.Enums;

namespace ZY.Core.Events;

/// <summary>
/// Representa uma alteração no estado de funcionamento do ZY.
/// </summary>
public sealed record AssistantStateChangedEvent(
    AssistantState PreviousState,
    AssistantState CurrentState,
    DateTimeOffset ChangedAt
);

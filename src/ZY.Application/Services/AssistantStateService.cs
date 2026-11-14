
using ZY.Application.Interfaces;
using ZY.Core.Enums;
using ZY.Core.Events;

namespace ZY.Application.Services;

/// <summary>
/// Gerencia o estado atual do assistente ZY.
/// </summary>
public sealed class AssistantStateService : IAssistantStateService
{
    public AssistantState CurrentState { get; private set; } =
        AssistantState.Standby;

    public event Action<AssistantStateChangedEvent>? StateChanged;

    public void SetState(AssistantState newState)
    {
        if (CurrentState == newState)
            return;

        var previousState = CurrentState;

        CurrentState = newState;

        var stateChangedEvent = new AssistantStateChangedEvent(
            previousState,
            newState,
            DateTimeOffset.Now
        );

        StateChanged?.Invoke(stateChangedEvent);
    }
}

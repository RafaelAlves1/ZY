
using ZY.Core.Enums;
using ZY.Core.Events;

namespace ZY.Application.Interfaces;

/// <summary>
/// Define o contrato para gerenciamento do estado do assistente.
/// </summary>
public interface IAssistantStateService
{
    AssistantState CurrentState { get; }

    event Action<AssistantStateChangedEvent>? StateChanged;

    void SetState(AssistantState newState);
}

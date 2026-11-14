
namespace ZY.Core.Enums;

/// <summary>
/// Representa o estado atual de funcionamento do assistente ZY.
/// </summary>
public enum AssistantState
{
    Standby,
    Listening,
    Thinking,
    Speaking,
    Executing,
    Error,
    GameMode
}


using ZY.Application.Services;
using ZY.Core.Enums;
using ZY.Core.Events;
namespace ZY.Tests;

public class AssistantStateServiceTests
{
    [Fact]
    public void CurrentState_ShouldStartAsStandby()
    {
        // Arrange
        var service = new AssistantStateService();

        // Act
        var currentState = service.CurrentState;

        // Assert
        Assert.Equal(AssistantState.Standby, currentState);
    }


    [Fact]
    public void SetState_ShouldUpdateCurrentState()
    {
        // Arrange
        var service = new AssistantStateService();

        // Act
        service.SetState(AssistantState.Listening);

        // Assert
        Assert.Equal(AssistantState.Listening, service.CurrentState);
    }


    [Fact]
    public void SetState_ShouldNotifySubscribers()
    {
        // Arrange
        var service = new AssistantStateService();

        AssistantStateChangedEvent? receivedEvent = null;

        service.StateChanged += stateChanged =>
        {
            receivedEvent = stateChanged;
        };

        // Act
        service.SetState(AssistantState.Thinking);

        // Assert
        Assert.NotNull(receivedEvent);
        Assert.Equal(AssistantState.Standby, receivedEvent.PreviousState);
        Assert.Equal(AssistantState.Thinking, receivedEvent.CurrentState);
        Assert.True(receivedEvent.ChangedAt <= DateTimeOffset.Now);
    }


    [Fact]
    public void SetState_ShouldNotNotifyWhenStateIsUnchanged()
    {
        // Arrange
        var service = new AssistantStateService();
        var notificationCount = 0;

        service.StateChanged += _ =>
        {
            notificationCount++;
        };

        // Act
        service.SetState(AssistantState.Standby);

        // Assert
        Assert.Equal(0, notificationCount);
        Assert.Equal(AssistantState.Standby, service.CurrentState);
    }


}

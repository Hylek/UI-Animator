using System;

public interface ITransitionEffect
{
    public Action<bool> TransitionComplete { get; set; }
    public void StartTransition(UIAnimatorTrigger trigger);
    public void StartTransitionManually(UIAnimatorTrigger trigger);
    public bool IsInManualMode();
}

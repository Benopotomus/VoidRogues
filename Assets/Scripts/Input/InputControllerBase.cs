// Base controller class. Fires off events that can be bound to by various Action Components
// to get the correct input toggles and states.

namespace VoidRogues
{
    using System;
    using UnityEngine;

    [DefaultExecutionOrder(-10)]
    public class InputControllerBase : ContextBehaviour
    {
        public Action<eInputAction, eButtonState, float> onInputChanged;
        public void InvokeOnInputChanged(eInputAction inputAction, eButtonState buttonState, float simulationTime) 
        { onInputChanged?.Invoke(inputAction, buttonState, simulationTime); }

    }
}

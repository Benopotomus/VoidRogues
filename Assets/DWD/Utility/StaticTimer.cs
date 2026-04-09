//© Dicewrench Designs LLC 2020-2023
//All Rights Reserved
//Last Owned by: Allen White (allen@dicewrenchdesigns.com)

using UnityEngine;
using System.Collections.Generic;

namespace DWD.Utility
{
    /// <summary>
    /// Static Timer class stores a Dictionary of WaitForSeconds
    /// Yielders to optimize their use.
    /// </summary>
    public static class StaticTimer
    {
        static Dictionary<float, WaitForSeconds> _timer = new Dictionary<float, WaitForSeconds>(25);
        static WaitForEndOfFrame _waitForEndofFrame = new WaitForEndOfFrame();

        public static WaitForSeconds Seconds(float seconds)
        {
            if (_timer.ContainsKey(seconds) == false)
            {
                _timer.Add(seconds, new WaitForSeconds(seconds));
            }
            return _timer[seconds];
        }

        public static WaitForEndOfFrame WaitForEndOfFrame()
        {
            return _waitForEndofFrame;
        }
    }
}
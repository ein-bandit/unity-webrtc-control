using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityWebRTCCOntrol.Network.Data
{
    /// <summary>
    /// Available types of input data that can be interpreted by default.
    /// </summary>
    public enum InputDataType
    {
        orientation,
        lightsensor,
        proximity,
        motion,
        tap,

        register,
        unregister,
        ready,

        invalid
    }

}
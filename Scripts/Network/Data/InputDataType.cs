using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityWebRtCControl.Network.Data
{
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
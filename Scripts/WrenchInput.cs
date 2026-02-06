using UnityEngine;

namespace RatchetCombat
{
    /// <summary>
    /// Input component for wrench charge attacks.
    /// Attach to characters that use ChargeAttackEffect_v2.
    /// Wire up attackHeld from your input system.
    /// </summary>
    public class WrenchInput : MonoBehaviour
    {
        [HideInInspector] public bool attackHeld;

        private float hyperstrikeHoldTime;

        public float GetHyperstrikeHoldTime()
        {
            return hyperstrikeHoldTime;
        }

        public void SetHyperstrikeHoldTime(float time)
        {
            hyperstrikeHoldTime = time;
        }
    }
}

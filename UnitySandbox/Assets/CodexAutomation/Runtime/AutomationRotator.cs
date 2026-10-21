using UnityEngine;

namespace Sumi.Automation
{
    public sealed class AutomationRotator : MonoBehaviour
    {
        public int UpdateCount { get; private set; }
        void Update()
        {
            transform.Rotate(Vector3.up, 60f * Time.deltaTime, Space.World);
            UpdateCount++;
        }
    }
}

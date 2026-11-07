using UnityEngine;

namespace EchoMountain.Riding
{
    public sealed class LabCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        public void SetTarget(Transform value) => target = value;

        private void LateUpdate()
        {
            if (!target) return;
            Vector3 desired = target.position - target.forward * 8f + Vector3.up * 4f;
            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-4f * Time.unscaledDeltaTime));
            transform.LookAt(target.position + target.forward * 5f + Vector3.up);
        }
    }
}

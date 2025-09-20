using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using _Project.Settings;

namespace _Project.Cameras
{
    [DisallowMultipleComponent]
    public class CameraRig : MonoBehaviour
    {
        [SerializeField] private CinemachineVirtualCamera virtualCamera;
        [SerializeField] private Transform followTarget;
        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private PlayerSettingsSO playerSettings;
        [SerializeField] private string lookActionName = "Look";
        [SerializeField] private Vector2 verticalClamp = new Vector2(-35f, 60f);
        [SerializeField, Min(0f)] private float horizontalSpeed = 300f;
        [SerializeField, Min(0f)] private float verticalSpeed = 250f;

        private CinemachinePOV _pov;
        private InputAction _lookAction;

        private void Awake()
        {
            ResolveCameraDependencies();
            CacheLookAction();
            ApplyFollowTarget();
            ApplyAxisSettings();
        }

        private void OnEnable()
        {
            ResolveCameraDependencies();
            CacheLookAction();
            ApplyFollowTarget();
            ApplyAxisSettings();
        }

        private void OnDisable()
        {
            _lookAction = null;
        }

        private void LateUpdate()
        {
            if (_pov == null || _lookAction == null)
            {
                return;
            }

            Vector2 lookValue = _lookAction.ReadValue<Vector2>();
            float sensitivity = playerSettings != null ? Mathf.Max(0.01f, playerSettings.LookSensitivity) : 1f;

            lookValue *= sensitivity;

            if (playerSettings != null)
            {
                if (playerSettings.InvertHorizontal)
                {
                    lookValue.x = -lookValue.x;
                }

                if (playerSettings.InvertVertical)
                {
                    lookValue.y = -lookValue.y;
                }
            }

            _pov.m_HorizontalAxis.m_InputAxisValue = lookValue.x;
            _pov.m_VerticalAxis.m_InputAxisValue = lookValue.y;
        }

        public void SetFollowTarget(Transform target)
        {
            followTarget = target;
            ApplyFollowTarget();
        }

        public void SetPlayerInput(PlayerInput input)
        {
            playerInput = input;
            CacheLookAction();
        }

        public void SetPlayerSettings(PlayerSettingsSO settings)
        {
            playerSettings = settings;
        }

        private void ResolveCameraDependencies()
        {
            if (virtualCamera == null)
            {
                virtualCamera = GetComponentInChildren<CinemachineVirtualCamera>();
            }

            if (virtualCamera != null)
            {
                _pov = virtualCamera.GetCinemachineComponent<CinemachinePOV>();
            }
        }

        private void CacheLookAction()
        {
            PlayerInput input = playerInput != null ? playerInput : GetComponentInParent<PlayerInput>();
            playerInput = input;

            if (input == null || input.actions == null || string.IsNullOrEmpty(lookActionName))
            {
                _lookAction = null;
                return;
            }

            _lookAction = input.actions.FindAction(lookActionName);

            if (_lookAction != null && !_lookAction.enabled)
            {
                _lookAction.Enable();
            }
        }

        private void ApplyFollowTarget()
        {
            if (virtualCamera == null)
            {
                return;
            }

            if (followTarget == null && playerInput != null)
            {
                followTarget = playerInput.transform;
            }

            if (followTarget == null)
            {
                followTarget = transform;
            }

            virtualCamera.Follow = followTarget;
            if (virtualCamera.LookAt == null)
            {
                virtualCamera.LookAt = followTarget;
            }
        }

        private void ApplyAxisSettings()
        {
            if (_pov == null)
            {
                return;
            }

            _pov.m_VerticalAxis.m_MinValue = verticalClamp.x;
            _pov.m_VerticalAxis.m_MaxValue = verticalClamp.y;
            _pov.m_VerticalAxis.m_Wrap = false;
            _pov.m_HorizontalAxis.m_Wrap = true;
            _pov.m_HorizontalAxis.m_MaxSpeed = horizontalSpeed;
            _pov.m_VerticalAxis.m_MaxSpeed = verticalSpeed;
        }

        private void OnValidate()
        {
            if (verticalClamp.y < verticalClamp.x)
            {
                verticalClamp.y = verticalClamp.x;
            }

            if (!Application.isPlaying)
            {
                ResolveCameraDependencies();
                ApplyAxisSettings();
            }
        }
    }
}

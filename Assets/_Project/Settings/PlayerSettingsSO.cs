using UnityEngine;

namespace _Project.Settings
{
    [CreateAssetMenu(menuName = "_Project/Settings/Player Settings", fileName = "PlayerSettings")]
    public class PlayerSettingsSO : ScriptableObject
    {
        [SerializeField, Range(0.01f, 10f)] private float lookSensitivity = 1f;
        [SerializeField] private bool invertHorizontal;
        [SerializeField] private bool invertVertical;

        public float LookSensitivity => lookSensitivity;
        public bool InvertHorizontal => invertHorizontal;
        public bool InvertVertical => invertVertical;
    }
}

using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>A flagpole's cloth: shows a flag image and sways gently in the breeze.</summary>
    public sealed class FlagView : MonoBehaviour
    {
        private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");

        [Tooltip("Front and back of the cloth.")]
        [SerializeField] private Renderer[] cloth = new Renderer[0];
        [Tooltip("Turns around the pole.")]
        [SerializeField] private Transform swing = null;
        [SerializeField] private float swayDegrees = 8f;
        [SerializeField] private float swaySpeed = 1.7f;

        private MaterialPropertyBlock _block;
        private float _phase;

        public Texture Shown { get; private set; }

        public void Show(Texture texture)
        {
            Shown = texture;
            _block ??= new MaterialPropertyBlock();
            _block.SetTexture(BaseMap, texture);
            for (int i = 0; i < cloth.Length; i++) cloth[i].SetPropertyBlock(_block);
        }

        private void Awake() => _phase = transform.position.x * 1.3f + transform.position.z;

        private void Update()
        {
            float yaw = Mathf.Sin(Time.time * swaySpeed + _phase) * swayDegrees;
            swing.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }
    }
}

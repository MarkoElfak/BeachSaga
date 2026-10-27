using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Pooled visual for one visitor. Reads its VisitorAgent each frame: follows the ground, turns toward
    /// travel direction, swings arms and legs while walking and lies on a towel while relaxing.
    /// </summary>
    public sealed class VisitorView : MonoBehaviour
    {
        private static readonly Quaternion LyingDown = Quaternion.Euler(90f, 0f, 0f);

        [Tooltip("Pivot at the feet; bobbed while walking and rotated flat while relaxing.")]
        [SerializeField] private Transform body = null;
        [SerializeField] private Transform leftLeg = null;
        [SerializeField] private Transform rightLeg = null;
        [SerializeField] private Transform leftArm = null;
        [SerializeField] private Transform rightArm = null;
        [SerializeField] private GameObject towel = null;

        private VisitorAgent _agent;
        private PresentationConfig _config;
        private TerrainShape _ground;
        private float _walkPhase;
        private bool _lying;

        public void Bind(VisitorAgent agent, PresentationConfig config, TerrainShape ground)
        {
            _agent = agent;
            _config = config;
            _ground = ground;
            _walkPhase = 0f;
            SetLying(false);
            transform.SetPositionAndRotation(GroundPosition(), Quaternion.LookRotation(agent.Facing));
        }

        public void Unbind()
        {
            _agent = null;
        }

        private void Update()
        {
            if (_agent == null)
            {
                return;
            }

            bool relaxing = _agent.State == VisitorState.Relaxing && _agent.SpotIndex >= 0;
            SetLying(relaxing);

            Quaternion target = relaxing ? Quaternion.identity : Quaternion.LookRotation(_agent.Facing);
            float maxTurn = _config.visitorTurnDegreesPerSecond * Time.deltaTime;
            transform.SetPositionAndRotation(GroundPosition(), Quaternion.RotateTowards(transform.rotation, target, maxTurn));

            float swing = 0f;
            float height = relaxing ? _config.visitorLyingHeight : 0f;
            if (_agent.IsMoving)
            {
                _walkPhase += Time.deltaTime * _config.visitorBobSpeed;
                height += Mathf.Abs(Mathf.Sin(_walkPhase)) * _config.visitorBobHeight;
                swing = Mathf.Sin(_walkPhase) * _config.visitorLimbSwingDegrees;
            }

            body.localPosition = new Vector3(0f, height, 0f);
            Swing(leftLeg, swing);
            Swing(rightLeg, -swing);
            Swing(leftArm, -swing);
            Swing(rightArm, swing);
        }

        private Vector3 GroundPosition()
        {
            Vector3 p = _agent.Position;
            p.y = _ground != null ? _ground.WalkHeight(p.x, p.z) : 0f; // boardwalk decks too
            return p;
        }

        private static void Swing(Transform limb, float degrees)
        {
            if (limb != null)
            {
                limb.localRotation = Quaternion.Euler(degrees, 0f, 0f);
            }
        }

        private void SetLying(bool lying)
        {
            if (_lying == lying && towel.activeSelf == lying)
            {
                return;
            }

            _lying = lying;
            towel.SetActive(lying);
            body.localRotation = lying ? LyingDown : Quaternion.identity;
        }
    }
}

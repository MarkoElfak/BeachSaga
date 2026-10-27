using BeachTycoon.Core;
using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// The player's character on the beach, with a name tag. Follows CharacterService: walks with swinging arms and
    /// legs, and at work reaches over the counter while their stand serves. Tapping opens the character's card.
    /// </summary>
    public sealed class CharacterView : MonoBehaviour, ITappable
    {
        private const float ServeSwingSpeed = 9f;
        private const float ServeReach = -55f;   // arm forward over the counter
        private const float ServeSwing = 25f;

        [Tooltip("Pivot at the feet; bobbed while walking.")]
        [SerializeField] private Transform body = null;
        [SerializeField] private Transform leftLeg = null;
        [SerializeField] private Transform rightLeg = null;
        [SerializeField] private Transform leftArm = null;
        [SerializeField] private Transform rightArm = null;

        private CharacterService _character;
        private StandService _stands;
        private PresentationConfig _config;
        private TerrainShape _ground;
        private float _phase;

        /// <summary>True while the arm is busy serving (tests).</summary>
        public bool IsServingPose { get; private set; }

        public void Bind(CharacterService character, StandService stands, PresentationConfig config, TerrainShape ground)
        {
            _character = character;
            _stands = stands;
            _config = config;
            _ground = ground;
            name = character.Character.displayName;
            GetComponentInChildren<ISignLabel>(true)?.SetText(character.Character.displayName);
            transform.SetPositionAndRotation(GroundPosition(), Quaternion.LookRotation(character.Agent.Facing));
        }

        public void OnTapped()
        {
            if (_character != null) EventBus.Publish(new CharacterCardRequested());
        }

        private void Update()
        {
            if (_character == null) return;
            VisitorAgent agent = _character.Agent;
            float maxTurn = _config.visitorTurnDegreesPerSecond * Time.deltaTime;
            Quaternion facing = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(agent.Facing), maxTurn);
            transform.SetPositionAndRotation(GroundPosition(), facing);

            float swing = 0f, height = 0f, serve = 0f;
            IsServingPose = !agent.IsMoving && _character.IsWorking && _stands.Get(_character.StandIndex).IsServing;
            if (agent.IsMoving)
            {
                _phase += Time.deltaTime * _config.visitorBobSpeed;
                height = Mathf.Abs(Mathf.Sin(_phase)) * _config.visitorBobHeight;
                swing = Mathf.Sin(_phase) * _config.visitorLimbSwingDegrees;
            }
            else if (IsServingPose)
            {
                _phase += Time.deltaTime * ServeSwingSpeed;
                serve = ServeReach + Mathf.Sin(_phase) * ServeSwing;
            }

            body.localPosition = new Vector3(0f, height, 0f);
            Swing(leftLeg, swing);
            Swing(rightLeg, -swing);
            Swing(leftArm, -swing);
            Swing(rightArm, IsServingPose ? serve : swing);
        }

        private Vector3 GroundPosition()
        {
            Vector3 p = _character.Agent.Position;
            p.y = _ground != null ? _ground.WalkHeight(p.x, p.z) : 0f;
            return p;
        }

        private static void Swing(Transform limb, float degrees)
        {
            if (limb != null) limb.localRotation = Quaternion.Euler(degrees, 0f, 0f);
        }
    }
}

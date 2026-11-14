using BeachTycoon.Core;
using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// A worker or job seeker on the beach, dressed in their own colours. Seekers wear a bobbing badge over the head;
    /// hired workers wear an apron and a name tag, and reach over the counter while their stand serves. Tapping
    /// opens their card.
    /// </summary>
    public sealed class WorkerView : MonoBehaviour, ITappable
    {
        private const float ServeSwingSpeed = 9f;
        private const float ServeReach = -55f;
        private const float ServeSwing = 25f;
        private const float BadgeBob = 0.08f;

        [Tooltip("Pivot at the feet; bobbed while walking.")]
        [SerializeField] private Transform body = null;
        [SerializeField] private Transform leftLeg = null;
        [SerializeField] private Transform rightLeg = null;
        [SerializeField] private Transform leftArm = null;
        [SerializeField] private Transform rightArm = null;
        [SerializeField] private Renderer[] shirtParts = new Renderer[0];
        [SerializeField] private Renderer[] skinParts = new Renderer[0];
        [SerializeField] private Renderer[] hairParts = new Renderer[0];
        [SerializeField] private GameObject longHair = null;
        [SerializeField] private GameObject apron = null;
        [SerializeField] private GameObject nameTag = null;
        [SerializeField] private Transform badge = null;

        private StaffMember _member;
        private StandService _stands;
        private PresentationConfig _config;
        private TerrainShape _ground;
        private float _phase;
        private float _badgeHeight;

        public StaffMember Member => _member;
        public bool ShowsBadge => badge.gameObject.activeSelf;
        public bool IsServingPose { get; private set; }

        public void Bind(StaffMember member, StandService stands, PaletteDefinition palette, PresentationConfig config, TerrainShape ground)
        {
            _member = member;
            _stands = stands;
            _config = config;
            _ground = ground;
            _badgeHeight = badge.localPosition.y;
            WorkerModel model = member.Model;
            name = "Worker " + model.name;
            Dress(shirtParts, palette.GetMaterial(model.shirt));
            Dress(skinParts, palette.GetMaterial(model.skin));
            Dress(hairParts, palette.GetMaterial(model.hair));
            longHair.SetActive(model.longHair);
            nameTag.GetComponent<ISignLabel>()?.SetText(model.name);
            transform.SetPositionAndRotation(GroundPosition(), Quaternion.LookRotation(member.Agent.Facing));
            Update();
        }

        public void OnTapped()
        {
            if (_member != null) EventBus.Publish(new WorkerCardRequested(_member.Id));
        }

        private void Update()
        {
            if (_member == null) return;
            bool seeker = _member.IsSeeker;
            if (badge.gameObject.activeSelf != seeker) badge.gameObject.SetActive(seeker);
            if (apron.activeSelf == seeker) apron.SetActive(!seeker);
            if (nameTag.activeSelf == seeker) nameTag.SetActive(!seeker);

            VisitorAgent agent = _member.Agent;
            float maxTurn = _config.visitorTurnDegreesPerSecond * Time.deltaTime;
            Quaternion facing = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(agent.Facing), maxTurn);
            transform.SetPositionAndRotation(GroundPosition(), facing);

            float swing = 0f, height = 0f, serve = 0f;
            int stand = _member.Model.stand;
            IsServingPose = !agent.IsMoving && _member.IsWorking && stand >= 0 && _stands.Get(stand).IsServing;
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
            if (seeker) badge.localPosition = new Vector3(0f, _badgeHeight + Mathf.Sin(Time.time * 3f) * BadgeBob, 0f);
        }

        private Vector3 GroundPosition()
        {
            Vector3 p = _member.Agent.Position;
            p.y = _ground != null ? _ground.WalkHeight(p.x, p.z) : 0f;
            return p;
        }

        private static void Dress(Renderer[] parts, Material material)
        {
            if (material == null) return;
            for (int i = 0; i < parts.Length; i++) parts[i].sharedMaterial = material;
        }

        private static void Swing(Transform limb, float degrees)
        {
            if (limb != null) limb.localRotation = Quaternion.Euler(degrees, 0f, 0f);
        }
    }
}

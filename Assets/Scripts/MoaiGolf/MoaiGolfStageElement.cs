using UnityEngine;

namespace MoaiGolf
{
    public enum MoaiGolfStageElementKind
    {
        Background,
        Terrain,
        LaunchPedestal,
        TargetPedestal,
        SuccessZone,
        AimMarker,
        GolfClubPivot,
        LaunchMoai,
        TargetMoai,
    }

    [DisallowMultipleComponent]
    public sealed class MoaiGolfStageElement : MonoBehaviour
    {
        [SerializeField] private MoaiGolfStageElementKind kind;
        [SerializeField] private int slotIndex;

        public MoaiGolfStageElementKind Kind => kind;
        public int SlotIndex => slotIndex;

        public void Configure(MoaiGolfStageElementKind elementKind, int index = 0)
        {
            kind = elementKind;
            slotIndex = index;
        }
    }
}

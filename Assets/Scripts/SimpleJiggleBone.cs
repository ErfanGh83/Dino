using UnityEngine;

/// <summary>
/// Verlet-style jiggle for a single bone (e.g. Breast.L / Breast.R). Add one copy per bone.
/// The animated pose remains the target while torso motion makes the simulated tip lag.
/// </summary>
[DefaultExecutionOrder(100)] // run after the Animator (raise if you have IK / look-at scripts)
public class SimpleJiggleBone : MonoBehaviour
{
    [Header("Bones (leave empty to auto-detect)")]
    public Transform bone;   // e.g. Breast.L (defaults to the object this script is on)
    public Transform tip;    // e.g. Breast_Tip.L (defaults to the first child)

    [Header("Body motion")]
    [Tooltip("Animated body part that drives the jiggle. Defaults to the breast bone's parent. If the breast bone is outside this transform's hierarchy, the script makes it follow this transform from its starting offset. Prefer the immediate torso parent for a bone in the same Mixamo rig.")]
    public Transform motionReference;

    [Header("Spring")]
    [Tooltip("How strongly the tip returns to its animated pose. Higher = tighter and faster.")]
    public float stiffness = 400f;
    [Tooltip("How quickly the motion settles. Higher = less bouncy.")]
    public float damping = 12f;
    [Tooltip("How strongly the body's ACCELERATION (starting, stopping, turning, landing) kicks the jiggle.")]
    [Range(0f, 1f)] public float inertia = 0.4f;
    [Tooltip("Maximum rotation away from the animated pose, in degrees.")]
    [Range(0f, 85f)] public float maxAngle = 25f;
    [Tooltip("Overall strength. 0 = no jiggle, 1 = full.")]
    [Range(0f, 1f)] public float blend = 1f;

    [Header("Stability")]
    [Tooltip("Smooths the body velocity (seconds) so stepped or jittery movement doesn't shake the bone. 0.03 - 0.08 is typical.")]
    public float velocitySmoothing = 0.05f;
    [Tooltip("Cap on how hard the body can kick the jiggle (m/s^2). Prevents violent swings on animation snaps and landings.")]
    public float maxBodyAcceleration = 100f;
    [Tooltip("If the torso reference point jumps farther than this in one frame (metres), the jiggle resets.")]
    public float teleportDistance = 0.5f;
    [Tooltip("Longest frame time that is simulated (seconds). Longer frames are clipped.")]
    public float maxFrameTime = 1f / 30f;

    Quaternion animatedLocalRot;
    Quaternion appliedLocalRot;
    bool hasApplied;
    bool initialized;
    bool followsMotionReference;
    Vector3 motionAnchorLocal;
    Quaternion motionAnchorLocalRotation;
    Vector3 prevBodyPosition;
    Vector3 smoothBodyVel; // filtered velocity of the animated body at the breast
    Vector3 offset;      // how far the tip lags behind its animated position (metres)
    Vector3 previousOffset; // previous Verlet position, used to retain velocity

    void Start()
    {
        if (!bone) bone = transform;
        if (!tip && bone.childCount > 0) tip = bone.GetChild(0);
        if (!motionReference) motionReference = bone.parent;

        if (!tip)
        {
            Debug.LogError("SimpleJiggleBone: no tip bone found. Assign one in the Inspector.", this);
            enabled = false;
            return;
        }

        if (motionReference)
        {
            motionAnchorLocal = motionReference.InverseTransformPoint(bone.position);
            motionAnchorLocalRotation = Quaternion.Inverse(motionReference.rotation) * bone.rotation;
            followsMotionReference = bone != motionReference &&
                                     !bone.IsChildOf(motionReference) &&
                                     !motionReference.IsChildOf(bone);
        }
    }

    void OnEnable()
    {
        initialized = false;
    }

    void OnDisable()
    {
        // Put the bone back to its animated pose when the script is turned off.
        if (hasApplied && bone) bone.localRotation = animatedLocalRot;
        if (followsMotionReference && bone && motionReference)
        {
            bone.SetPositionAndRotation(
                motionReference.TransformPoint(motionAnchorLocal),
                motionReference.rotation * motionAnchorLocalRotation);
        }
        hasApplied = false;
        initialized = false;
    }

    /// <summary>Call this after teleporting or respawning the character.</summary>
    public void ResetJiggle()
    {
        initialized = false;
    }

    void LateUpdate()
    {
        float frameDt = Time.deltaTime;
        if (frameDt <= 1e-5f) return;
        float dt = Mathf.Min(frameDt, maxFrameTime); // simulation step only; velocity uses frameDt

        // A normal rig bone already follows its parent through the hierarchy. If
        // this bone belongs to a separate hierarchy, carry its starting pose with
        // the selected body reference before adding jiggle.
        if (followsMotionReference && motionReference)
        {
            bone.SetPositionAndRotation(
                motionReference.TransformPoint(motionAnchorLocal),
                motionReference.rotation * motionAnchorLocalRotation);
        }

        // 1. Recover the clean animated pose of the bone.
        // If the Animator didn't overwrite our last rotation, undo it.
        // If it did (the clip animates this bone), the current rotation is already the animated one.
        if (hasApplied && SameRot(bone.localRotation, appliedLocalRot))
            bone.localRotation = animatedLocalRot;
        else
            animatedLocalRot = bone.localRotation;

        // 2. Read the animated geometry.
        Vector3 origin = bone.position;
        Vector3 target = tip.position;
        Vector3 motionPoint = motionReference
            ? motionReference.TransformPoint(motionAnchorLocal)
            : origin;
        Vector3 restVec = target - origin;
        float length = restVec.magnitude;
        if (length < 1e-6f) return;
        Vector3 restDir = restVec / length;

        // 3. Reset on first frame or teleport.
        if (!initialized || (motionPoint - prevBodyPosition).sqrMagnitude > teleportDistance * teleportDistance)
        {
            prevBodyPosition = motionPoint;
            smoothBodyVel = Vector3.zero;
            offset = Vector3.zero;
            previousOffset = Vector3.zero;
            initialized = true;
        }

        // 4. Track the animated torso at the breast's attachment point. This includes
        // both translation and rotation of the chest/spine, but excludes the breast
        // bone's own rotation, which remains the animation's moving rest pose.
        Vector3 instVel = (motionPoint - prevBodyPosition) / frameDt;
        prevBodyPosition = motionPoint;

        float k = 1f - Mathf.Exp(-frameDt / Mathf.Max(velocitySmoothing, 1e-4f));
        Vector3 newSmooth = Vector3.Lerp(smoothBodyVel, instVel, k);
        Vector3 bodyAcc = (newSmooth - smoothBodyVel) / frameDt;
        smoothBodyVel = newSmooth;
        bodyAcc = Vector3.ClampMagnitude(bodyAcc, maxBodyAcceleration);

        // Only sideways motion can rotate the bone, so drop anything along its length.
        offset = Vector3.ProjectOnPlane(offset, restDir);
        previousOffset = Vector3.ProjectOnPlane(previousOffset, restDir);
        Vector3 drive = -inertia * Vector3.ProjectOnPlane(bodyAcc, restDir);

        // 5. Spring that pulls the offset back to zero.
        float maxOffset = length * Mathf.Tan(maxAngle * Mathf.Deg2Rad);
        float maxStep = Mathf.Min(1f / 120f, 0.25f / Mathf.Sqrt(Mathf.Max(stiffness, 1f))); // stays stable even for very stiff springs
        int steps = Mathf.Clamp(Mathf.CeilToInt(dt / maxStep), 1, 16);
        float h = dt / steps;
        for (int i = 0; i < steps; i++)
        {
            // Verlet integration stores motion in the difference between the
            // current and previous offsets, like the package's chain simulation.
            Vector3 inertialStep = (offset - previousOffset) * Mathf.Exp(-damping * h);
            Vector3 acceleration = drive - stiffness * offset;
            Vector3 nextOffset = offset + inertialStep + acceleration * (h * h);
            previousOffset = offset;
            offset = nextOffset;

            float mag = offset.magnitude;
            if (mag > maxOffset)
            {
                Vector3 n = offset / mag;
                offset = n * maxOffset;
                Vector3 frameVelocity = (offset - previousOffset) / h;
                float outward = Vector3.Dot(frameVelocity, n);
                if (outward > 0f)
                {
                    frameVelocity -= n * outward; // stop pushing past the limit
                    previousOffset = offset - frameVelocity * h;
                }
            }
        }

        if (IsBad(offset) || IsBad(previousOffset))
        {
            offset = Vector3.zero;
            previousOffset = Vector3.zero;
        }

        // 6. Point the bone at the simulated tip.
        Vector3 dir = (restVec + offset).normalized; // offset is perpendicular to restVec, so never zero
        dir = Vector3.Slerp(restDir, dir, blend);
        bone.rotation = Quaternion.FromToRotation(restDir, dir) * bone.rotation;

        appliedLocalRot = bone.localRotation;
        hasApplied = true;
    }

    static bool SameRot(Quaternion a, Quaternion b)
    {
        const float e = 1e-6f;
        return Mathf.Abs(a.x - b.x) < e && Mathf.Abs(a.y - b.y) < e &&
               Mathf.Abs(a.z - b.z) < e && Mathf.Abs(a.w - b.w) < e;
    }

    static bool IsBad(Vector3 v)
    {
        return float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
               float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z);
    }
}

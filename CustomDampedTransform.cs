using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
[System.Serializable]
public struct CustomDampedTransformData : IAnimationJobData, ICustomDampedTransformData
{
    [SerializeField] Transform m_ConstrainedObject;

    [SyncSceneToStream, SerializeField] Transform m_Source;
    [SyncSceneToStream, SerializeField] Transform m_Origin;

    [SyncSceneToStream, SerializeField, Range(0f, 1f)] float m_DampPosition;

    [SyncSceneToStream, SerializeField, Range(0f, 1f)] float m_DampRotation;

    [NotKeyable, SerializeField] bool m_MaintainAim;

    public Transform constrainedObject { get => m_ConstrainedObject; set => m_ConstrainedObject = value; }
    public Transform sourceObject { get => m_Source; set => m_Source = value; }
    public Transform originObject { get => m_Origin; set => m_Origin = value; }
    public float dampPosition { get => m_DampPosition; set => m_DampPosition = Mathf.Clamp01(value); }

    public float dampRotation { get => m_DampRotation; set => m_DampRotation = Mathf.Clamp01(value); }
    public bool maintainAim { get => m_MaintainAim; set => m_MaintainAim = value; }

    string ICustomDampedTransformData.dampPositionFloatProperty => PropertyUtils.ConstructConstraintDataPropertyName(nameof(m_DampPosition));
    string ICustomDampedTransformData.dampRotationFloatProperty => PropertyUtils.ConstructConstraintDataPropertyName(nameof(m_DampRotation));

    bool IAnimationJobData.IsValid() => !(m_ConstrainedObject == null || m_Source == null);

    void IAnimationJobData.SetDefaultValues()
    {
        m_ConstrainedObject = null;
        m_Source = null;
        m_Origin = null;
        m_DampPosition = 0.5f;
        m_DampRotation = 0.5f;
        m_MaintainAim = true;
    }
}

[DisallowMultipleComponent, AddComponentMenu("Animation Rigging/Custom Damped Transform")]
public class CustomDampedTransform : RigConstraint<
    CustomDampedTransformJob,
    CustomDampedTransformData,
    CustomDampedTransformJobBinder<CustomDampedTransformData>
    >
{
#if UNITY_EDITOR
#pragma warning disable 0414
    [NotKeyable, SerializeField, HideInInspector] bool m_SettingsGUIToggle;
#endif
}
      
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

[Unity.Burst.BurstCompile]
public struct CustomDampedTransformJob : IWeightedAnimationJob
{
    const float k_FixedDt = 0.01667f; // 60Hz simulation step
    const float k_DampFactor = 40f;

    public ReadWriteTransformHandle driven;
    public ReadWriteTransformHandle source;
    public ReadOnlyTransformHandle origin;
    public AffineTransform localBindTx;

    public Vector3 aimBindAxis;
    public bool noRotateSource;
    public AffineTransform prevDrivenTx;
    public AffineTransform prevSourceTx;

    public FloatProperty dampPosition;
    public FloatProperty dampRotation;

    public FloatProperty jobWeight { get; set; }

    public void ProcessRootMotion(AnimationStream stream) { }

    public void ProcessAnimation(AnimationStream stream)
    {
        float w = jobWeight.Get(stream);
        float streamDt = Mathf.Abs(stream.deltaTime);

        Vector3 originPos;
        if (origin.IsValid(stream))
            originPos = origin.GetPosition(stream);
        else
            originPos = Vector3.zero;
        
        source.GetGlobalTR(stream, out Vector3 sourcePos, out Quaternion sourceRot);
        driven.GetGlobalTR(stream, out Vector3 drivenPos, out Quaternion drivenRot);

        sourcePos -= originPos;
        drivenPos -= originPos;

        if (w > 0f && streamDt > 0f)
        {
            
            var dampPosW = AnimationRuntimeUtils.Square(1f - dampPosition.Get(stream));
            var dampRotW = AnimationRuntimeUtils.Square(1f - dampRotation.Get(stream));
            bool doAimAjustements = Vector3.Dot(aimBindAxis, aimBindAxis) > 0f;

            var sourceTx = new AffineTransform(sourcePos, sourceRot);
            var targetTx = sourceTx * localBindTx;

            targetTx.translation = Vector3.Lerp(drivenPos, targetTx.translation, w);
            targetTx.rotation = Quaternion.Lerp(drivenRot, targetTx.rotation, w);

            Quaternion rot = Quaternion.identity;

            while (streamDt > 0f)
            {
                float factoredDt = k_DampFactor * Mathf.Min(k_FixedDt, streamDt);

                var dir = (prevDrivenTx.translation - sourcePos).normalized;
                var targetDir = (targetTx.translation - sourceTx.translation).normalized;
                dir = Vector3.Slerp(dir, targetDir, factoredDt*dampPosW);
              
                prevDrivenTx.translation = sourcePos +  dir * localBindTx.translation.magnitude;

                rot = Quaternion.FromToRotation(sourceTx.rotation * aimBindAxis, dir);

                streamDt -= k_FixedDt;
            }
        
            source.SetRotation(stream, (rot * sourceTx.rotation));
            driven.SetPosition(stream, prevDrivenTx.translation + originPos);
        }
        else
        {
            prevDrivenTx.Set(drivenPos, drivenRot);
            AnimationRuntimeUtils.PassThrough(stream, driven);
        }
    }
}

public interface ICustomDampedTransformData
{
    Transform constrainedObject { get; }
    Transform sourceObject { get; }
    Transform originObject { get; }
    bool maintainAim { get; }

    string dampPositionFloatProperty { get; }
    string dampRotationFloatProperty { get; }
}

public class CustomDampedTransformJobBinder<T> : AnimationJobBinder<CustomDampedTransformJob, T>
    where T : struct, IAnimationJobData, ICustomDampedTransformData
{
    public override CustomDampedTransformJob Create(Animator animator, ref T data, Component component)
    {
        var job = new CustomDampedTransformJob();

        job.driven = ReadWriteTransformHandle.Bind(animator, data.constrainedObject);
        job.source = ReadWriteTransformHandle.Bind(animator, data.sourceObject);
        
        job.origin = ReadOnlyTransformHandle.Bind(animator, data.originObject);
        var originPos = data.originObject==null ? Vector3.zero : data.originObject.position;

        var drivenTx = new AffineTransform(data.constrainedObject.position - originPos, data.constrainedObject.rotation);
        var sourceTx = new AffineTransform(data.sourceObject.position - originPos, data.sourceObject.rotation);

        job.localBindTx = sourceTx.InverseMul(drivenTx);
        job.prevDrivenTx = drivenTx;
        job.prevSourceTx = sourceTx;

        job.dampPosition = FloatProperty.Bind(animator, component, data.dampPositionFloatProperty);
        job.dampRotation = FloatProperty.Bind(animator, component, data.dampRotationFloatProperty);

        if (data.maintainAim && AnimationRuntimeUtils.SqrDistance(data.constrainedObject.position, data.sourceObject.position) > 0f)
            job.aimBindAxis = Quaternion.Inverse(data.sourceObject.rotation) * (drivenTx.translation - sourceTx.translation).normalized;
        else
            job.aimBindAxis = Vector3.zero;

        return job;
    }

    public override void Destroy(CustomDampedTransformJob job)
    {
    }
}

    
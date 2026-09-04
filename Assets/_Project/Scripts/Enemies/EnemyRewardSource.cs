using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public sealed class EnemyRewardSource : MonoBehaviour
{
    [FormerlySerializedAs("experienceAmount")]
    [SerializeField, Min(0)] private int runExperienceAmount = 10;

    public int RunExperienceAmount => runExperienceAmount;

#if UNITY_EDITOR
    public void ConfigureForEditor(int configuredRunExperienceAmount)
    {
        runExperienceAmount = Mathf.Max(0, configuredRunExperienceAmount);
    }
#endif
}

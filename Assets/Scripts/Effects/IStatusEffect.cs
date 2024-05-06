using Sirenix.OdinInspector;
using Sirenix.Serialization;

public interface IStatusEffect
{
    public void Perform(ref DamageInstance damageInstance);

    public float ModifierValue { get; set; }
}

#region Invincibility

public class InvincibilityEffect : IStatusEffect
{
    [Title("Percentage")]
    [OdinSerialize]
    public float ModifierValue { get; set; } = 1;

    public void Perform(ref DamageInstance damageInstance)
    {
        damageInstance.Damage *= (1.0f - ModifierValue);
    }
}

#endregion

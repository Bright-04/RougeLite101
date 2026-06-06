/// <summary>
/// Extension point for enemy-specific DDA behavior. The DDA system provides a
/// shared runtime profile and should not depend on concrete enemy types; new
/// enemies participate by mapping that profile to their own parameters.
/// </summary>
public interface IDdaAdaptiveEnemy
{
    void ApplyDdaProfile(DdaDifficultyProfile profile);
}

using UnityEngine;

[System.Serializable]
public class RunStarRatingCalculator
{
    [SerializeField] private float oneStarMinHpRatio = 0.0f;
    [SerializeField] private float twoStarMinHpRatio = 0.4f;
    [SerializeField] private float threeStarMinHpRatio = 0.7f;

    public int CalculateStars(PlayerStats playerStats)
    {
        if (playerStats == null)
        {
            return 1;
        }

        float maxHp = Mathf.Max(1f, playerStats.GetMaxHP());
        float remainingHpRatio = Mathf.Clamp01(playerStats.GetCurrentHP() / maxHp);
        int stars = remainingHpRatio >= oneStarMinHpRatio ? 1 : 0;

        if (remainingHpRatio >= twoStarMinHpRatio)
        {
            stars = 2;
        }

        if (remainingHpRatio >= threeStarMinHpRatio)
        {
            stars = 3;
        }

        return Mathf.Clamp(stars, 1, 3);
    }
}

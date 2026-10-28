using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "Combat/Bow Charge Settings", fileName = "BowSettings")]
public sealed class BowChargeSettings : ScriptableObject
{
    public Sprite idleSprite;
    public Sprite[] drawSprites;
    public GameObject arrowPrefab;

    [Header("활 종류별 차징 시간")]
    [InspectorName("최대 차징 시간 (초)")]
    [Tooltip("이 활이 최대로 차징되기까지 걸리는 시간입니다. 활 종류마다 별도의 Bow Charge Settings 에셋을 연결하여 다르게 지정하세요.")]
    [Min(0.01f)] public float maxChargeSeconds = 1f;

    [Header("차징 비례 피해")]
    [FormerlySerializedAs("minimumDamage")]
    [InspectorName("기본 피해량")]
    [Min(0f)] public float baseDamage = 5f;
    [InspectorName("최대 차징 추가 피해율 (%)")]
    [Tooltip("차징 비율에 따라 0%부터 이 비율까지 선형으로 추가됩니다. 150이면 최대 차징 피해는 기본 피해의 250%입니다.")]
    [Min(0f)] public float maxChargeBonusPercent = 150f;

    [Header("차징 비례 명중 정확도")]
    [InspectorName("미차징 최대 비산 각도 (±도)")]
    [Tooltip("10이면 조준 방향의 -10도~+10도 사이로 무작위 발사합니다. 차징 비율에 따라 선형으로 줄어들며 최대 차징에서는 오차가 0도입니다.")]
    [Min(0f)] public float maxSpreadDegrees = 7.5f;

    [Min(0.1f)] public float arrowSpeed = 30f;
    [Min(0.1f)] public float arrowLifetime = 3f;

    public float ChargeFraction(float seconds)
    {
        return Mathf.Clamp01(seconds / Mathf.Max(0.01f, maxChargeSeconds));
    }

    public float SpreadAt(float seconds)
    {
        return Mathf.Max(0f, maxSpreadDegrees) * (1f - ChargeFraction(seconds));
    }

    public Vector2 ShotDirection(Vector2 aimedDirection, float seconds)
    {
        if (aimedDirection.sqrMagnitude < 0.0001f) aimedDirection = Vector2.right;
        aimedDirection.Normalize();
        float spread = SpreadAt(seconds);
        if (spread <= 0f) return aimedDirection;
        float angle = Random.Range(-spread, spread) * Mathf.Deg2Rad;
        float cosine = Mathf.Cos(angle);
        float sine = Mathf.Sin(angle);
        return new Vector2(aimedDirection.x * cosine - aimedDirection.y * sine,
            aimedDirection.x * sine + aimedDirection.y * cosine);
    }

    public float DamageAt(float seconds)
    {
        float bonusRate = ChargeFraction(seconds) * Mathf.Max(0f, maxChargeBonusPercent) / 100f;
        return Mathf.Max(0f, baseDamage) * (1f + bonusRate);
    }
}

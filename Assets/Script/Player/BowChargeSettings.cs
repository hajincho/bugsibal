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
    [Tooltip("10이면 조준 방향의 -10도~+10도 사이로 무작위 발사합니다. 차징 비율에 따라 최대 차징 오차 설정값까지 선형으로 줄어듭니다.")]
    [Min(0f)] public float maxSpreadDegrees = 7.5f;
    [Tooltip("Remaining random error at full charge (plus/minus degrees).")]
    [Min(0f)] public float fullChargeSpreadDegrees;

    [Header("활 고유 특성")]
    [InspectorName("최대 차징 시 적 관통")]
    public bool pierceEnemiesAtFullCharge;
    [InspectorName("항상 적 관통")]
    public bool alwaysPierceEnemies;
    [HideInInspector] public int fullChargeRicochets; // Legacy serialized value; timed arrows have no bounce limit.
    public bool impactExplosion;
    public bool explodeOnAnyContact;
    public bool ricochetWithoutFullCharge;
    [Min(0f)] public float ricochetDamageBonus = .2f;
    public bool explodeOnRicochetEnemyHit = true;
    public bool usePinkEffects;
    [Range(1, 4)] public int projectileCount = 1;
    [Min(0f)] public float projectileSpacingDegrees;
    [Tooltip("Adjacent arrow spacing at full charge; negative keeps constant spacing.")]
    public float fullChargeProjectileSpacingDegrees = -1f;
    public bool scaleTrailWithCharge = true;
    [Header("활 전용 색상 및 잔상")]
    public bool useGoldenEffects;
    public Material goldSpriteMaterial;
    public Material goldGlowMaterial;
    [InspectorName("최대 차징 잔상 시간 (초)")]
    [Min(0f)] public float maxArrowTrailSeconds;

    public float TrailDurationAt(float seconds)
    {
        return Mathf.Max(0f, maxArrowTrailSeconds) * (scaleTrailWithCharge ? ChargeFraction(seconds) : 1f);
    }

    [InspectorName("절차적 활 당기기 모션")]
    public bool proceduralDrawMotion;

    [Header("지속 피해")]
    [InspectorName("초당 도트 피해")]
    [Min(0f)] public float dotDamagePerSecond;
    [InspectorName("도트 지속 시간 (초)")]
    [Tooltip("동일 효과 재적중 시 중첩 없이 지속시간을 갱신합니다.")]
    [Min(0f)] public float dotDuration = 2f;

    [Header("풀차징 적중 시 끌어당김")]
    [InspectorName("풀차징 끌어당김 사용")]
    public bool pullOnFullCharge;
    [InspectorName("끌어당김 반경")]
    [Min(0f)] public float pullRadius = 2.5f;
    [InspectorName("모이는 지점의 전방 거리")]
    [Tooltip("적중 지점에서 플레이어의 반대 방향으로 이 거리만큼 더 떨어진 한 점에 모읍니다.")]
    [Min(0f)] public float pullForwardOffset = 3f;
    [InspectorName("끌어당김 지속 시간 (초)")]
    [Min(0.01f)] public float pullDuration = 0.3f;
    [InspectorName("끌어당김 속도")]
    [Min(0f)] public float pullSpeed = 12f;

    public bool ShouldPierce(float seconds)
    {
        return alwaysPierceEnemies || (pierceEnemiesAtFullCharge && seconds >= Mathf.Max(0.01f, maxChargeSeconds));
    }

    [Min(0.1f)] public float arrowSpeed = 30f;
    [Min(0.1f)] public float arrowLifetime = 3f;

    public float ChargeFraction(float seconds)
    {
        return Mathf.Clamp01(seconds / Mathf.Max(0.01f, maxChargeSeconds));
    }

    public float SpreadAt(float seconds)
    {
        return Mathf.Lerp(Mathf.Max(0f, maxSpreadDegrees), Mathf.Max(0f, fullChargeSpreadDegrees), ChargeFraction(seconds));
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

    public float ProjectileSpacingAt(float seconds)
    {
        return fullChargeProjectileSpacingDegrees < 0f ? projectileSpacingDegrees :
            Mathf.Lerp(projectileSpacingDegrees, fullChargeProjectileSpacingDegrees, ChargeFraction(seconds));
    }

    public Vector2[] ShotDirections(Vector2 aim, float seconds)
    {
        int count = Mathf.Clamp(projectileCount, 1, 4);
        if(count == 1) return new[] { ShotDirection(aim, seconds) };
        if(aim.sqrMagnitude < .0001f) aim = Vector2.right;
        float center = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
        float spread = SpreadAt(seconds);
        float spacing = Mathf.Max(8f, ProjectileSpacingAt(seconds));
        float previous = float.NegativeInfinity;
        var result = new Vector2[count];
        for(int i=0;i<count;i++)
        {
            float nominal = (i - (count-1)*.5f) * spacing;
            // Independent inaccuracy, constrained so adjacent arrows never converge below 8 degrees.
            float low = Mathf.Max(nominal-spread, previous+8f);
            float offset = Random.Range(low, nominal+spread);
            previous = offset;
            float angle = (center+offset) * Mathf.Deg2Rad;
            result[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }
        return result;
    }

    public float DamageAt(float seconds)
    {
        float bonusRate = ChargeFraction(seconds) * Mathf.Max(0f, maxChargeBonusPercent) / 100f;
        return Mathf.Max(0f, baseDamage) * (1f + bonusRate);
    }
}

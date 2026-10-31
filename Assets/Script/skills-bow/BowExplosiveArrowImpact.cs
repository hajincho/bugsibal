using System.Collections.Generic;
using UnityEngine;

public sealed class BowExplosiveArrowImpact : MonoBehaviour
{
    public GameObject explosionPrefab;
    public float visualScale = 2.25f;
    public float radius = 3.25f;
    public float skillDamageMultiplier = 1f;
    bool exploded;
    public bool HasExploded => exploded;
    readonly HashSet<EnemyData> damagedEnemies = new HashSet<EnemyData>();
    public IEnumerable<EnemyData> DamagedEnemies => damagedEnemies;

    public void Explode(EnemyData primary, Vector3 position, float arrowDamage)
    {
        if (exploded || primary == null) return;
        exploded = true;
        // Reuse only the sword R visual; its collider/script must not apply sword damage.
        var lunarVisual=GetComponent<LunarArrowImpact>();
        if(lunarVisual!=null)lunarVisual.PlayQExplosion(position,radius);
        if (lunarVisual==null && explosionPrefab != null)
        {
            var effect = Instantiate(explosionPrefab, position, Quaternion.identity);
            effect.name = "BowQExplosion";
            effect.transform.localScale = explosionPrefab.transform.localScale * visualScale;
            foreach (var renderer in effect.GetComponentsInChildren<SpriteRenderer>(true))
            {
                var color = renderer.color;
                color.a *= 0.7f;
                renderer.color = color;
            }
            foreach (var collider in effect.GetComponentsInChildren<Collider2D>(true)) collider.enabled = false;
            foreach (var body in effect.GetComponentsInChildren<Rigidbody2D>(true)) body.simulated = false;
            foreach (var script in effect.GetComponentsInChildren<MonoBehaviour>(true)) script.enabled = false;
            Destroy(effect, 0.4f);
        }
        var targets = new HashSet<EnemyData> { primary };
        foreach (var collider in Physics2D.OverlapCircleAll(position, radius))
        {
            var enemy = collider.GetComponentInParent<EnemyData>();
            if (enemy != null && enemy.CompareTag("Enemy")) targets.Add(enemy);
        }
        // The empowered shot deals 2x its launch damage in total, including the primary target.
        foreach (var enemy in targets)
        {
            if (enemy == null || enemy.enemy_current_HP <= 0f) continue;
            damagedEnemies.Add(enemy);
            enemy.TakeDamage(Mathf.Max(0f, arrowDamage * 2f * skillDamageMultiplier), CombatDamageKind.Skill);
            enemy.Hitted();
            var lunarImpact=GetComponent<LunarArrowImpact>();
            if(lunarImpact!=null && enemy.enemy_current_HP<=0f)lunarImpact.OnQualifiedEnemyDefeated(enemy,enemy.transform.position);
        }
    }
}

using System.Collections;
using UnityEngine;

namespace Vampire
{
    public class BoomerangMonster : Monster
    {
        [SerializeField] protected Transform boomerangSpawnPosition;
        protected BoomerangMonsterBlueprint BoomerangBlueprint => (BoomerangMonsterBlueprint)monsterBlueprint;
        protected float timeSinceLastBoomerangAttack;
        protected float timeSinceLastMeleeAttack;
        protected float outOfRangeTime;
        protected int boomerangIndex;

        public override void Setup(int monsterIndex, Vector2 position, MonsterBlueprint monsterBlueprint, float hpBuff = 0)
        {
            base.Setup(monsterIndex, position, monsterBlueprint, hpBuff);
            boomerangIndex = entityManager.AddPoolForBoomerang(BoomerangBlueprint.boomerangPrefab);
            outOfRangeTime = 0;
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            Vector2 toPlayer = (playerCharacter.transform.position - transform.position);
            float distance = toPlayer.magnitude;
            Vector2 dirToPlayer = toPlayer/distance;
            entityManager.Grid.UpdateClient(this);
            timeSinceLastBoomerangAttack += Time.fixedDeltaTime;
            if (distance <= BoomerangBlueprint.range)
            {
                rb.linearVelocity += dirToPlayer * monsterBlueprint.acceleration * Time.fixedDeltaTime / 2;
                if (timeSinceLastBoomerangAttack >= 1.0f/BoomerangBlueprint.boomerangAttackSpeed)
                {
                    ThrowBoomerang(playerCharacter.transform.position);
                    timeSinceLastBoomerangAttack = 0;
                }
            }
            else
            {
                rb.linearVelocity += dirToPlayer * monsterBlueprint.acceleration * Time.fixedDeltaTime;
            }
        }

        protected void ThrowBoomerang(Vector2 targetPosition)
        {
            Boomerang boomerang = entityManager.SpawnBoomerang(boomerangIndex, boomerangSpawnPosition.position, BoomerangBlueprint.boomerangDamage, 0, BoomerangBlueprint.throwRange, BoomerangBlueprint.throwTime, BoomerangBlueprint.targetLayer);
            boomerang.Throw(boomerangSpawnPosition, targetPosition);
        }

        void OnCollisionStay2D(Collision2D col)
        {
            if (((BoomerangBlueprint.targetLayer & (1 << col.collider.gameObject.layer)) != 0) && timeSinceLastMeleeAttack >= 1.0f/monsterBlueprint.atkspeed)
            {
                playerCharacter.TakeDamage(monsterBlueprint.atk);
                timeSinceLastMeleeAttack = 0;
            }
        }
    }
}

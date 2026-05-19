using System.Collections;
using UnityEngine;

namespace Vampire
{
    public class RangedMonster : Monster
    {
        public enum State
        {
            Walking,
            Shooting
        }

        [SerializeField] protected Transform projectileSpawnPosition;
        protected RangedMonsterBlueprint RangedBlueprint => (RangedMonsterBlueprint)monsterBlueprint;
        protected float timeSinceLastAttack;
        protected State state;
        protected float outOfRangeTime;
        protected int projectileIndex;

        public override void Setup(int monsterIndex, Vector2 position, MonsterBlueprint monsterBlueprint, float hpBuff = 0)
        {
            base.Setup(monsterIndex, position, monsterBlueprint, hpBuff);
            projectileIndex = entityManager.AddPoolForProjectile(RangedBlueprint.projectilePrefab);
            outOfRangeTime = 0;
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            if (alive)
            {
                Vector2 toPlayer = (playerCharacter.transform.position - transform.position);
                float distance = toPlayer.magnitude;
                Vector2 dirToPlayer = toPlayer/distance;
                switch (state)
                {
                    case State.Walking:
                        rb.linearVelocity += dirToPlayer * monsterBlueprint.acceleration * Time.fixedDeltaTime;
                        entityManager.Grid.UpdateClient(this);
                        if (distance <= RangedBlueprint.range)
                        {
                            state = State.Shooting;
                            monsterSpriteAnimator.StopAnimating();
                            //rb.bodyType = RigidbodyType2D.Static;
                            //rb.mass = 9999;
                        }
                        break;

                    case State.Shooting:
                        timeSinceLastAttack += Time.fixedDeltaTime;
                        // rb.velocity *= 0.95f;
                        if (timeSinceLastAttack >= 1.0f/monsterBlueprint.atkspeed)
                        {
                            LaunchProjectile(dirToPlayer);
                            timeSinceLastAttack = 0;//Mathf.Repeat(timeSinceLastAttack, 1.0f/monsterBlueprint.atkspeed);
                        }
                        if (distance <= RangedBlueprint.range)
                            outOfRangeTime = 0;
                        else
                            outOfRangeTime += Time.deltaTime;
                        if (outOfRangeTime > RangedBlueprint.timeAllowedOutsideRange)
                        {
                            state = State.Walking;
                            monsterSpriteAnimator.StartAnimating();
                            //rb.bodyType = RigidbodyType2D.Dynamic;
                            //rb.mass = 1;
                        }
                        break;
                }
                // if (!knockedBack && rb.velocity.magnitude > monsterBlueprint.movespeed)
                //      rb.velocity = rb.velocity.normalized * monsterBlueprint.movespeed;
            }
        }

        protected void LaunchProjectile(Vector2 direction)
        {
            Projectile projectile = entityManager.SpawnProjectile(projectileIndex, projectileSpawnPosition.position, monsterBlueprint.atk, 0, RangedBlueprint.projectileSpeed, RangedBlueprint.targetLayer);
            projectile.Launch(direction);
        }
    }
}

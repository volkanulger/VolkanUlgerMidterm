using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class HeroPlayer : MonoBehaviour
{
    public Animator animController;
    public int heroHealth = 100;
    public TextMeshProUGUI healthText;
    public Image healthBar;
    public GameObject hitEffect;
    public SpriteRenderer heroSprite;
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverText;

    public EnemyPlayer enemy;
    public int attack1Damage = 10;
    public int attack2Damage = 20;
    public GameObject attack1Effect;
    public GameObject attack2Effect;

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            Attack1();
        }
        if (Keyboard.current.wKey.wasPressedThisFrame)
        {
            Attack2();
        }
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            GetHurt();
        }
    }

    // Attack 1 Button -> OnClick
    public void Attack1()
    {
        if (enemy.enemyHealth <= 0 || heroHealth <= 0)
        {
            return;
        }
        GetAttack();
        attack1Effect.SetActive(false);
        attack1Effect.SetActive(true);
        enemy.GetHurt(attack1Damage);
    }

    // Attack 2 Button -> OnClick
    public void Attack2()
    {
        if (enemy.enemyHealth <= 0 || heroHealth <= 0)
        {
            return;
        }
        GetAttack();
        attack2Effect.SetActive(false);
        attack2Effect.SetActive(true);
        enemy.GetHurt(attack2Damage);
    }

    public void GameOver()
    {
        animController.SetBool("Death", true);
        heroSprite.color = Color.gray;
        gameOverText.text = "Game Over\nYou Lose";
        gameOverPanel.SetActive(true);
    }
    public void GetHurt()
    {
        if (heroHealth <= 0 || enemy.enemyHealth <= 0)
        {
            return;
        }
        animController.SetTrigger("Hurt");
        heroHealth = heroHealth - 20;
        hitEffect.SetActive(false);
        hitEffect.SetActive(true);
        heroSprite.color = Color.red;
        Invoke("ResetColor", 0.2f);
        healthText.text = "Health: " + heroHealth.ToString();
        healthBar.fillAmount = (float)heroHealth / 100;
        if (heroHealth <= 0)
        {
            GameOver();
        }
    }
    public void ResetColor()
    {
        if (heroHealth > 0)
        {
            heroSprite.color = Color.white;
        }
    }
    public void GetAttack()
    {
        animController.SetTrigger("Attack");
    }
}

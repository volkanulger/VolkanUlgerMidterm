using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyPlayer : MonoBehaviour
{
    public Animator animController;
    public int enemyHealth = 100;
    public TextMeshProUGUI healthText;
    public Image healthBar;
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverText;

    public HeroPlayer hero;
    public float attackDelay = 0.8f;

    public void GameOver()
    {
        animController.SetBool("Death", true);
        gameOverText.text = "Game Over\nYou Win";
        gameOverPanel.SetActive(true);
    }
    public void GetHurt(int damage)
    {
        if (enemyHealth <= 0)
        {
            return;
        }
        animController.SetTrigger("Hurt");
        enemyHealth = enemyHealth - damage;
        if (enemyHealth < 0)
        {
            enemyHealth = 0;
        }
        healthText.text = "Enemy Health: " + enemyHealth.ToString();
        healthBar.fillAmount = (float)enemyHealth / 100;
        if (enemyHealth <= 0)
        {
            GameOver();
        }
        else if (!IsInvoking("GetAttack"))
        {
            // vurulduktan biraz sonra karsilik ver
            Invoke("GetAttack", attackDelay);
        }
    }
    public void GetAttack()
    {
        if (enemyHealth <= 0 || hero.heroHealth <= 0)
        {
            return;
        }
        animController.SetTrigger("Attack");
        // kilic inince P1 hasar alsin
        Invoke("HitHero", 0.4f);
    }
    public void HitHero()
    {
        if (enemyHealth <= 0)
        {
            return;
        }
        hero.GetHurt();
    }
}

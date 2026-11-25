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

    public void GameOver()
    {
        animController.SetBool("Death", true);
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
        else
        {
            Debug.Log("Enemy Health: " + enemyHealth);
        }
    }
}

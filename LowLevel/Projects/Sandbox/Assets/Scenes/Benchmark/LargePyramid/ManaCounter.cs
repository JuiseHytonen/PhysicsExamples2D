using UnityEngine.UIElements;

public class ManaCounter
{
    private const int MAX_MANA = 10;
    private int m_manaValue = 0;
    private Button m_manaButton;

    public ManaCounter(Button manaButton)
    {
        m_manaButton = manaButton;
        m_manaButton.schedule.Execute(OnManaUpdate).Every(700);
    }

    public void ReduceMana(int deduction)
    {
        m_manaValue -= deduction;
        UpdateValue();
    }

    private void OnManaUpdate()
    {
        if (m_manaValue < MAX_MANA)
        {
            m_manaValue++;
            UpdateValue();
        }
    }

    private void UpdateValue()
    {
        m_manaButton.text = m_manaValue.ToString();
    }
}

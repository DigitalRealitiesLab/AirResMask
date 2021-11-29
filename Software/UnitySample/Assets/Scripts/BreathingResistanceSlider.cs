using UnityEngine.UI;
using UnityEngine;

public class BreathingResistanceSlider : MonoBehaviour
{
    public Slider slider;
    public Text text;
    public float decreaseSpeed = 0.2f;

    ESP32ClientBluetooth clientBluetooth;

    // Start is called before the first frame update
    void Start()
    {
        clientBluetooth = FindObjectOfType<ESP32ClientBluetooth>();

        if (slider == null)
        {
            slider = GetComponent<Slider>();
        }

        slider.minValue = 0.0f;
        slider.maxValue = 1.0f;
    }

    // Update is called once per frame
    void Update()
    {
        if (slider.value > slider.minValue)
        {
            slider.value -= Time.deltaTime * decreaseSpeed;
        }
    }

    public void OnSliderValueChanged()
    {
        clientBluetooth.AirResistance = Mathf.RoundToInt(Mathf.Min(1.0f, Mathf.Max(0.0f, slider.value)) * (float)clientBluetooth.airResistanceSteps);
        text.text = clientBluetooth.AirResistance.ToString();
    }
}

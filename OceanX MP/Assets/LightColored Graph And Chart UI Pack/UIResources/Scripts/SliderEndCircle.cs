 

using UnityEngine;
using UnityEngine.UI;

public class SliderEndCircle : MonoBehaviour
{
    // 引用Slider组件
    public Slider slider;
    // 引用要控制旋转的游戏对象
    public Transform targetObject;
    // 新增的布尔变量，用于决定旋转方向
    public bool isClockwise = true;

    void Start()
    {
        // 确保Slider存在
        if (slider != null)
        {
            // 为Slider的onValueChanged事件添加监听器
            slider.onValueChanged.AddListener(OnSliderValueChanged);
        }
    }

    void OnSliderValueChanged(float value)
    {
        // 根据Slider的值计算旋转角度
        float rotationAngle = value * 360f;
        // 根据布尔变量决定旋转方向
        if (!isClockwise)
        {
            rotationAngle = -rotationAngle;
        }
        // 设置目标对象绕Z轴的旋转角度
        targetObject.rotation = Quaternion.Euler(0f, 0f, rotationAngle);
    }

    void OnDestroy()
    {
        // 在脚本销毁时移除监听器，避免内存泄漏
        if (slider != null)
        {
            slider.onValueChanged.RemoveListener(OnSliderValueChanged);
        }
    }
}
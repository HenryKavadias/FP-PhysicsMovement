using UnityEngine;

public class Spring
{
    public float Strength { private get; set; }
    public float Damper { private get; set; }
    public float Target { private get; set; }
    public float Velocity { private get; set; }
    public float Value { get; set; }

    public void Update(float deltaTime)
    {
        var direction = Target - Value >= 0 ? 1f : -1f;
        var force = Mathf.Abs(Target - Value) * Strength;
        Velocity += (force * direction - Velocity * Damper) * deltaTime;
        Value += Velocity * deltaTime;
    }

    public void Reset()
    {
        Velocity = 0;
        Value = 0;
    }
}

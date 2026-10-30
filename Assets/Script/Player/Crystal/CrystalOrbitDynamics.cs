using UnityEngine;

// Momentum-based escape: the central gravitational parameter decays with time.
public static class CrystalOrbitDynamics
{
    public const float MassFadeSeconds = .28f;
    public static float MassFraction(float seconds)
    {
        return Mathf.Exp(-Mathf.Max(0f,seconds)/MassFadeSeconds);
    }
    public static void Step(ref Vector2 position,ref Vector2 velocity,Vector2 center,
        float initialGravity,float elapsed,float deltaTime,Vector2 aimDirection,bool guide,float speedToKeep=0f)
    {
        int steps=Mathf.Max(1,Mathf.CeilToInt(deltaTime/.005f));
        float dt=deltaTime/steps;
        for(int i=0;i<steps;i++)
        {
            float age=Mathf.Max(0f,elapsed+i*dt);
            float mass=MassFraction(age);
            Vector2 radial=position-center;
            float r2=radial.sqrMagnitude;
            Vector2 acceleration=r2>.0001f ? -radial.normalized*initialGravity*mass/(r2+.0225f) : Vector2.zero;
            if(guide && aimDirection.sqrMagnitude>.001f && velocity.sqrMagnitude>.001f && age<1.3f)
            {
                // Only a faint, fading bias toward the launch-time aim. Never reacquire a target.
                float window=(1f-mass)*Mathf.Exp(-age/.6f)*Mathf.SmoothStep(1f,0f,age/1.3f);
                float turn=Mathf.Clamp(Vector2.SignedAngle(velocity,aimDirection)*Mathf.Deg2Rad*1.2f,-.8f,.8f);
                acceleration+=Vector2.Perpendicular(velocity.normalized)*velocity.magnitude*turn*window;
            }
            velocity+=acceleration*dt;
            if(speedToKeep>0f && velocity.sqrMagnitude>.000001f)velocity=velocity.normalized*speedToKeep;
            position+=velocity*dt;
        }
    }
}
